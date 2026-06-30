# FUND-2496 — DRC Query Optimization

## Problem

The `GET /enkelvoudiginformatieobjecten` endpoint was timing out (214 seconds) when listing
documents for tenants with ~1 million rows and a broad authorization scope (20 `InformatieObjectType`
entries in the authorization table).

### Root cause

The authorization filter was implemented as a **JOIN** between the main
`enkelvoudiginformatieobjecten` table and the session-scoped temp table
`TempInformatieObjectAuthorization`. PostgreSQL's query planner chose the 20-row temp table as
the **outer** side of a Nested Loop. This caused 20 index scans over the 1M-row EIO table,
producing ~1 million intermediate rows. A top-N heap sort then had to process all those rows to
return the 100 requested results.

```
Limit  (actual time=214385 rows=100)
  Sort Key: e.id  (top-N heapsort, 1,023,420 input rows)
  Nested Loop
    Seq Scan on InformatieObjectAuthorization_1  (rows=20)
    Index Scan using t3b_IX_eio_owner_iot_latest_vha (loops=20, rows=51,171 each)
```

The `(owner, id)` index — which would provide early termination after 100 rows — could not be
used because the JOIN broke the row ordering of the base table.

The count query had a related problem: it joined through the `enkelvoudiginformatieobjectversies`
table (up to 69 million rows) to reach the `Vertrouwelijkheidaanduiding` field, even though a
denormalized copy of the latest value (`LatestVertrouwelijkheidAanduiding`) was already available
on the `EnkelvoudigInformatieObject` entity itself.

---

## Solution

### 1. Inline authorization predicate for the paginated query

Instead of joining against the temp table, the authorization pairs are fetched into C# memory
and compiled into an **inline predicate** using `Expression` trees. The pairs are **grouped by
`MaximumVertrouwelijkheidAanduiding`**, so each group becomes a single `ANY` test rather than one
OR term per pair:

```
(InformatieObjectType = ANY(@types_for_vha5) AND (int)LatestVertrouwelijkheidAanduiding.Value <= 5)
OR (InformatieObjectType = ANY(@types_for_vha3) AND (int)LatestVertrouwelijkheidAanduiding.Value <= 3)
OR ...
```

`List<string>.Contains()` translates to `= ANY(ARRAY[...])` in PostgreSQL — a single operator that
stays efficient for large type sets. Because there are only 7 ZGW VHA levels, this collapses N auth
pairs (up to ~5000) into **≤7 OR terms** regardless of how many types are authorized, keeping the
predicate cheap to plan and execute.

Because the temp table is no longer referenced in the paginated query, PostgreSQL can use the
covering index with **early termination** via `LIMIT` — no Sort node needed.

**Expected result:** sub-second response for page 1.

### 2. Two-phase pagination

The paginated query is split into two steps:

- **Phase 1:** `SELECT id, creationtime FROM ... ORDER BY creationtime DESC, id ASC LIMIT N OFFSET M`
  — narrow query, only the IDs (and `CreationTime`, needed to build the cursor anchor — see §5).
  Uses the `(owner, creationtime DESC, id ASC)` covering index with early termination.
- **Phase 2:** `SELECT * FROM ... WHERE id IN (...)` with `Include` navigation properties,
  re-ordered `ORDER BY creationtime DESC, id ASC`. Executes at most N (typically 100) PK lookups.

This prevents EF Core from emitting a `LEFT JOIN` to the versie table in the main paging query,
which would prevent index-only scan usage.

**Result ordering changed** from `id` to `(CreationTime DESC, Id ASC)` — newest documents first.
This is the ordering the covering index is sorted by, so phase 1 reads it directly without a Sort.

All four paging code paths — phase-1 offset, phase-1 cursor seek, the cursor-seek's offset
fallback, and the phase-2 fetch — order through a single private helper
`OrderByPage(IQueryable<EnkelvoudigInformatieObject>)` that applies
`OrderByDescending(CreationTime).ThenBy(Id)`. The keyset seek predicate (§5) is only correct while
every `ORDER BY` matches that exact tuple, so funnelling them through one definition prevents a
future ordering change from silently desyncing the seek from the sort.

### 3. Count query — same inline predicate, `enable_bitmapscan = off`

The count reuses the **same query** as the paged query — it already carries the inline VHA-grouped
authorization predicate — so it is just a `COUNT(*)` over that query. No temp-table `JOIN`, no
separate auth logic. (Earlier this was a dedicated `JOIN` against `TempInformatieObjectAuthorization`
forced into a Hash Join via `SET LOCAL enable_nestloop = off`; that became unnecessary once the
predicate was grouped by VHA level and the `(owner, InformatieObjectType, LatestVertrouwelijkheidAanduiding)`
covering index existed.)

The one risk specific to count: unlike the paged query there is **no `LIMIT`** to terminate early, so
the planner has to process all matching rows. Without a selectivity filter (~1M rows) it may pick a
**BitmapOr** plan that turns lossy at scale and triggers millions of heap rechecks (~29s). To prevent
that, the count:

- Conditionally applies `SET LOCAL enable_bitmapscan = off` within a transaction when no selectivity
  filters are set. This steers the planner to a single **Index-Only Scan + filter + aggregate** over
  the `t3b_IX_eio_owner_iot_latest_vha` covering index (`InformatieObjectType` and
  `LatestVertrouwelijkheidAanduiding` are both in the index, so no heap access) — O(N), no BitmapOr.
- The `anyFiltersSet` guard skips the hint when `Identificatie`, `Bronorganisatie`, `Uuid_In`, or
  `Trefwoorden_In` are provided — those make the result set small enough that the planner chooses
  correctly on its own.

> **Must be validated with `EXPLAIN ANALYZE` on the largest tenant (`852256450`) before merge.** The
> prior ~29s figure was measured before the VHA-grouping and the covering index existed, so it should
> no longer apply — but the count is the one path without an early-exit `LIMIT`, so the planner's
> choice here is the thing to confirm.

### 4. Cached count and anchor values

Both the total count and the authorization count are cached in distributed cache (Redis) with a
5-minute TTL, keyed on `(rsin, filter parameters)`. This avoids repeated expensive `COUNT(*)`
queries during pagination of the same result set.

### 5. Optional cursor-based pagination (v1.5 only)

A feature flag `Application:EnkelvoudigInformatieObjectenCursorPaging` enables keyset
(seek-method) pagination for v1.5. When active (the default), the query seeks past the previous page's anchor
instead of using `OFFSET`, making deep-page navigation O(K) instead of O(N).

Because the result ordering is `(CreationTime DESC, Id ASC)`, the anchor is a **composite**
`PageAnchor(CreationTime, Id)` rather than a single `Guid`. The seek predicate that selects rows
"after" the anchor is:

```
CreationTime < anchor.CreationTime
OR (CreationTime == anchor.CreationTime AND Id > anchor.Id)
```

`CreationTime` alone is not unique, so the `Id` tie-breaker is required for a stable, gap-free seek.
The anchor is cached per `(rsin, page, filter)` tuple with a 5-minute TTL. A sentinel
(`PageAnchor.Sentinel = (DateTime.MinValue, Guid.Empty)`) is stored when no real anchor exists yet,
so the cache factory isn't re-invoked on repeated lookups within the TTL. If a deep page is
requested before page 1, it falls back to the offset method until the sentinel expires —
functionally correct, just less optimal.

The flag is read directly from `IConfiguration` on every request — Consul-backed configuration
reloads without a service restart.

### 6. Same inline predicate applied to `GetAllVerzendingen`

`GetAllVerzendingenQueryHandler` (v1.5) was migrated to the same pattern:

- The paged query's temp-table `EXISTS` is replaced by the inline VHA-grouped predicate, built over
  the `Verzending → InformatieObject` navigation (`InformatieObjectType` and
  `LatestVertrouwelijkheidAanduiding` live on the EIO). The `Verzending → EIO` join itself remains —
  those columns are only available there.
- The count reuses that **same query** (it already carries the inline predicate), so the page and the
  count share one predicate and can no longer drift; the old separate double-`JOIN` count is gone.

The count uses the **same `SET LOCAL enable_bitmapscan = off`** lever as the EIO count (guarded by the
same `anyFiltersSet` check). It shares the EIO count's failure mode — a lossy `BitmapOr` on the
OR-predicate — so the same hint applies. The earlier `enable_nestloop = off` was carried over from the
pre-inline version, where it compensated for the planner's cardinality misestimate on the **temp
authorization table**; that table is no longer in the count query, so the reason is gone, and forcing a
Hash Join could even hurt if `Verzendingen` is small (a nested-loop PK lookup into EIO would be
cheaper). The `Verzending → EIO` join strategy is therefore left to the planner, which now has real
statistics. Like the EIO count, this still needs an `EXPLAIN ANALYZE` confirmation on
production-scale data before merge.

### 7. Keyset seek index-range bound (deep-page fix)

The cursor seek predicate in §5 is correct, but on its own it does **not** make deep pages cheap.
The OR-form

```
CreationTime < anchor.CreationTime
OR (CreationTime == anchor.CreationTime AND Id > anchor.Id)
```

can never become a btree index-range bound: the mixed sort directions (`CreationTime DESC, Id ASC`)
rule out the row-comparison rewrite `(CreationTime, Id) < (anchorCt, anchorId)` that PostgreSQL would
otherwise use as a starting boundary. So PostgreSQL keeps the whole OR-form as a **Filter** and starts
the Index Only Scan at the *top* of the index — every deep page re-scans all rows newer than the
anchor. That is offset-like cost, defeating the point of keyset pagination.

Production `EXPLAIN ANALYZE` (tenant `852256450`, deep page) confirmed this: `Index Cond` was
`(owner = …)` only, with `Rows Removed by Filter: 338314`, `Buffers: shared hit=229429`, ~178 ms.

The fix adds a redundant but **sargable** leading conjunct:

```csharp
query.Where(e =>
    e.CreationTime <= anchor.CreationTime                                  // sargable index-range bound
    && (e.CreationTime < anchor.CreationTime
        || (e.CreationTime == anchor.CreationTime && e.Id > anchor.Id)))   // precise boundary tiebreak
```

`CreationTime <= anchor.CreationTime` is logically implied by both OR branches, so results are
unchanged — but it is a plain `column <= const` that PostgreSQL can use as an **Index Cond** to start
the scan at the anchor. The OR-form then only disambiguates the boundary rows that share the anchor's
`CreationTime`.

Production `EXPLAIN ANALYZE` after the fix (same tenant, deep page):

| Metric | Before | After |
|---|---|---|
| `Index Cond` | `owner` only | `owner AND creationtime <= …` |
| Rows Removed by Filter | 338,314 | 111 |
| Buffers (shared hit) | 229,429 | 203 |
| Heap Fetches | 95,598 | 67 |
| Execution Time | 178.3 ms | 0.9 ms |

The decisive metric is **Buffers** (229,429 → 203): it counts pages touched regardless of cache
state, so the ~1000× drop cannot be cache warmth — it is the scan reading ~1000× fewer index entries.
This fix is independent of the authorization predicate form (inline or parameterized) and improves the
already-shipped code; it only affects the cursor-seek branch (page > 1 with a cached anchor), leaving
the offset and page-1 paths untouched.

### 8. Deterministic authorization predicate ordering

`BuildInlineAuthorizationPredicate` groups auth pairs by VHA with `GroupBy`, which yields groups in
first-occurrence order of the source. The source (`TempInformatieObjectAuthorization` fetched without
`ORDER BY`) has no stable row order, so the generated SQL text — both the order of OR terms and the
element order inside each `ANY('{…}')` array literal — could vary between requests for the same
authorization set. Different SQL text means a different plan-cache / compiled-query-cache key.

The groups are now ordered by VHA level and the types within each group are sorted:

```csharp
.GroupBy(p => p.MaximumVertrouwelijkheidAanduiding)
.OrderBy(g => g.Key)
.Select(g => (MaxVha: g.Key, Types: g.Select(p => p.InformatieObjectType).OrderBy(t => t).ToList()))
```

This makes the generated SQL deterministic for a given authorization set, so PostgreSQL's plan cache
and EF's compiled-query cache hit consistently. Results and plan shape are unchanged.

---

## Index changes

### Removed from `DrcDbContext` / snapshot

| Index | Reason |
|---|---|
| `IX_enkelvoudiginformatieobjecten_informatieobjecttype` | Superseded by composite covering index |
| `IX_enkelvoudiginformatieobjecten_owner` | Superseded by composite covering index |
| `IX_enkelvoudiginformatieobjecten_owner_informatieobjecttype_la~` | Referenced `LatestEnkelvoudigInformatieObjectVersieId` (obsolete) |
| `IX_eio_owner_id_incl_type_latest` | Replaced by `t3b_IX_eio_owner_id_incl_type_latest_vha` |
| `IX_enkelvoudiginformatieobjectversies_owner_vertrouwelijkheid~` | Auth filtering no longer joins through versies |
| `t3b_idx_eiov_owner_vha_id` | Auth filtering no longer joins through versies |
| `IX_enkelvoudiginformatieobjectversies_vertrouwelijkheidaanduid~` | Auth filtering no longer joins through versies |

### Retained / updated

| Index | Columns | INCLUDE | Purpose |
|---|---|---|---|
| `t3b_IX_eio_owner_creationtime_id_incl_type_vha` | `(owner ASC, CreationTime DESC, id ASC)` | `InformatieObjectType`, `LatestVertrouwelijkheidAanduiding` | Phase 1 paging & cursor seek — Index Only Scan with inline auth predicate, sorted by result order |
| `t3b_IX_eio_owner_id_incl_type_latest_vha` | `(owner, id)` | `InformatieObjectType`, `LatestVertrouwelijkheidAanduiding` | Earlier `(owner, id)`-ordered covering index (retained) |
| `t3b_IX_eio_owner_iot_latest_vha` | `(owner, InformatieObjectType, LatestVertrouwelijkheidAanduiding)` | — | Auth count query — Hash Join probe side |

The new `t3b_IX_eio_owner_creationtime_id_incl_type_vha` index is sorted exactly by the
`(owner, CreationTime DESC, Id ASC)` result order, so phase 1 and the cursor seek read it directly
with no Sort node, and evaluate the inline auth conditions from the `INCLUDE` leaf columns without
touching the heap.

The `INCLUDE` columns changed from `LatestEnkelvoudigInformatieObjectVersieId` to
`LatestVertrouwelijkheidAanduiding`. The planner can now evaluate the inline OR conditions
(InformatieObjectType and LatestVertrouwelijkheidAanduiding) purely from the index leaf pages,
without touching the heap.

---

## Migrations

**`20260625120000_optimize_drc_index_cleanup`**

All SQL statements use `CONCURRENTLY IF EXISTS` / `IF NOT EXISTS` to avoid table locks and make
the migration idempotent across environments. The new `t3b_` indices were already created by
earlier raw-SQL migrations; the `IF NOT EXISTS` makes the `CREATE INDEX` statements no-ops on
those environments.

**`20260625130000_optimize_drc_cursor_paging_index`**

Creates the cursor-paging covering index `t3b_IX_eio_owner_creationtime_id_incl_type_vha`
(`owner ASC, creationtime DESC, id ASC` INCLUDE `informatieobjecttype, latest_vertrouwelijkheidaanduiding`).
Uses `CREATE INDEX CONCURRENTLY IF NOT EXISTS` with `suppressTransaction: true` (concurrent index
builds cannot run inside a transaction). `Down` drops it with `DROP INDEX CONCURRENTLY IF EXISTS`.

---

## Files changed

### New files
| File | Description |
|---|---|
| `src/OneGround.ZGW.Documenten.DataModel/Migrations/20260625120000_optimize_drc_index_cleanup.cs` | Migration: drops obsolete indices, ensures new indices exist |
| `src/OneGround.ZGW.Documenten.DataModel/Migrations/20260625120000_optimize_drc_index_cleanup.Designer.cs` | EF Core migration snapshot companion |
| `src/OneGround.ZGW.Documenten.DataModel/Migrations/20260625130000_optimize_drc_cursor_paging_index.cs` | Migration: creates the `(owner, creationtime DESC, id ASC)` cursor-paging covering index `CONCURRENTLY` |
| `src/OneGround.ZGW.Documenten.DataModel/Migrations/20260625130000_optimize_drc_cursor_paging_index.Designer.cs` | EF Core migration snapshot companion |

### Modified files
| File | Change summary |
|---|---|
| `src/OneGround.ZGW.Documenten.DataModel/DrcDbContext.cs` | Removed 4 obsolete indices; updated composite and covering EIO indices to use `LatestVertrouwelijkheidAanduiding` with `t3b_` database names; added the `(owner, creationtime DESC, id ASC)` cursor-paging covering index |
| `src/OneGround.ZGW.Documenten.DataModel/Migrations/DrcDbContextModelSnapshot.cs` | Reflects index changes from `DrcDbContext` (incl. the new cursor-paging index) |
| `src/OneGround.ZGW.DataAccess/BaseDbContextFactory.cs` | **TODO (to be removed):** design-time `EnableSensitiveDataLogging` / `EnableDetailedErrors` / `LogTo(Console)` to capture generated SQL while validating the optimization — must not ship |
| `src/OneGround.ZGW.Documenten.Web/Configuration/ApplicationConfiguration.cs` | Added `EnkelvoudigInformatieObjectenCursorPaging` property |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs` | Inline auth predicate, two-phase pagination, cached count, `anyFiltersSet` hint |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs` | Same as v1.0 plus `BestandsDelen` include in phase 2 |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs` | Inline auth predicate **grouped by VHA level** (`ANY` per level); `(CreationTime DESC, Id ASC)` ordering centralized in an `OrderByPage` helper shared by all four paging paths; two-phase pagination; cached count; cursor paging with composite `PageAnchor(CreationTime, Id)`; count reuses the same inline-predicate query (no temp-table JOIN) with `SET LOCAL enable_bitmapscan = off` to force an Index-Only Scan; cursor-seek index-range bound `CreationTime <= anchor` (§7); deterministic VHA-group / type ordering (§8) |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllVerzendingenQueryHandler.cs` | Same inline VHA-grouped auth predicate over the `Verzending → InformatieObject` navigation (replaces the temp-table `EXISTS` in the paged query); count reuses that same query (no separate double-JOIN) with `SET LOCAL enable_bitmapscan = off`, aligned with the EIO count (the old `enable_nestloop = off` was for the now-removed temp-table join) |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllGebruiksRechtenQuery.cs` | Auth filter updated to use `LatestVertrouwelijkheidAanduiding` |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllObjectInformatieObjectenQuery.cs` | Auth filter updated to use `LatestVertrouwelijkheidAanduiding` |

---

## Production validation (large tenant, PostgreSQL 12)

> **Production runs PostgreSQL 12.** This matters for the authorization predicate: the hashed
> `ScalarArrayOpExpr` optimization — which evaluates `x = ANY(array)` as a hash lookup (~O(1) per row
> regardless of array length) — was introduced in **PostgreSQL 14**. On 12, an `= ANY(array)` / `IN
> (...)` used as a *filter* is scanned **linearly** per row, for both literal and parameter forms.
> Upgrading to ≥14 is the single largest lever for the authorization-filter cost, but is out of scope
> for this work.

Validated against a large tenant authorized for **1450 `InformatieObjectType` entries** with
confidentiality levels, ~2.9 million matching documents.

### Paged query — healthy

The two-phase paged query behaves as designed:

| Metric | Value |
|---|---|
| Plan | `Index Only Scan` + `LIMIT` early termination |
| Index Cond | `(owner = … AND creationtime <= anchor)` — seek bound (§7) active |
| Rows Removed by Filter | 1 |
| Buffers | 100 (62 hit, 38 read) |
| Execution Time | 17.0 ms |
| **Planning Time** | **16.3 ms** |

The only smell is that planning (~16 ms) is as large as execution. This is the cost of parsing the
**1450 inlined string literals** every request; because the paged query is not cached, that cost
recurs on every call. This is the one place where parameterizing the authorization arrays would
measurably help (see below).

### Authorization-array parameterization — investigated, reverted

The inline predicate embeds the type lists as SQL literals (`InformatieObjectType IN ('url', 'url', …)`
/ `= ANY('{…}'::text[])`). For large authorization sets this carries three stacked costs, all caused
by inlining + a dynamically-shaped expression tree:

1. EF Core recompiles the query per distinct authorization set (the `Expression.Constant` list changes
   the tree shape).
2. PostgreSQL re-parses/plans multi-KB SQL with no plan-cache reuse (the ~16 ms planning above).
3. Execution: the `= ANY` membership test — linear per row on PostgreSQL 12.

A prototype parameterized variant was built: each VHA group's type list is wrapped in a small holder
and referenced via member access, so EF funcletizes it into an **array parameter** (`= ANY(@__Types_n)`)
instead of literals. This keeps the SQL text small and stable, fixing costs (1) and (2). It does
**not** change cost (3) on PostgreSQL 12 (linear `= ANY` filter regardless of literal vs parameter).

**Decision: reverted, not shipped.** The deep-page seek bound (§7) solved the actual production
timeout; at the array sizes observed the parameterization benefit is marginal (planning is ~16 ms,
and only for the uncached paged query), and a parameter array makes the planner fall back to a generic
selectivity estimate, which would need `PREPARE`/`EXPLAIN` verification (≥6 executions to reach the
generic plan) per tenant before enabling. The approach is documented here in case a tenant with a very
large authorization set ever makes the planning cost material; re-introduction is a localized change
(a parameterizing variant of `BuildInlineAuthorizationPredicate` behind a config flag).

### Count query — the remaining bottleneck (~14 s)

The count is the slow path on this tenant:

| Metric | Value |
|---|---|
| Plan | `Finalize Aggregate` → `Gather` (2 workers) → `Parallel Index Only Scan` on `(owner, iot, vha)` |
| Auth predicate | fully in `Index Cond` (SAOP), no Filter |
| Rows counted | ~2.9M (`rows=966740 loops=3`) |
| Heap Fetches | 163,780 |
| Buffers | shared hit 2,602,920 + read 143,259 (~21 GB) |
| Planning Time | 29.5 ms |
| **Execution Time** | **13,992 ms** |

`enable_bitmapscan = off` works as intended (Index-Only Scan + aggregate, **no** lossy BitmapOr — the
~29 s failure mode in §3 is avoided). The cost is **not** the array size; an exact `COUNT` over ~2.9M
matching rows simply cannot terminate early.

**The likely dominant cost is the heap fetches, not the counting.** `143,259` disk reads × ~0.1 ms ≈
the 14 s execution, and that read count tracks the `163,780` heap fetches almost 1:1. An "Index Only
Scan" doing 163k heap fetches means the visibility map is not all-visible for those pages, so
PostgreSQL falls back to the heap — random I/O that dominates the wall-clock. The 2.9M index entries
themselves are mostly cache hits and fast.

Recommendations, in order of leverage:

1. **VACUUM / autovacuum (highest leverage, lowest risk).** A
   `VACUUM (VERBOSE, ANALYZE) public.enkelvoudiginformatieobjecten` sets the all-visible bits so the
   scan becomes truly index-only and the heap fetches collapse; tune autovacuum more aggressively for
   this high-write table (lower `autovacuum_vacuum_scale_factor` or a fixed threshold) so it stays
   that way. Confirm by re-running the count `EXPLAIN ANALYZE` and watching `Heap Fetches` / `read=`
   drop. (Maintenance action for the DBA; no data writes.)
2. **Pre-aggregated rollup table (structural fix).** A
   `eio_count_by_type (owner, informatieobjecttype, vha, count)` summary turns the count into
   `SUM(count)` over the ~1450 matching `(iot, vha)` rows — O(authorized types) instead of
   O(documents), sub-millisecond. Cost: incremental maintenance on insert/delete and on `vha`/`iot`
   changes, plus multi-tenant consistency. Worth it only if fresh exact counts on large tenants are a
   recurring requirement.
3. **More parallel workers** (`max_parallel_workers_per_gather`) reduces wall-clock roughly linearly
   but does not address the heap-fetch root cause; apply only after (1).

A new index does **not** help: `(owner, iot, vha)` is already covering for the count; the problem is
visibility (heap fetches) and row volume, neither of which an index addresses. The existing 5-minute
count cache (§4) keeps the 14 s off the hot path but does not fix the cold-cache cost (timeout /
thundering-herd risk on TTL expiry).

---

## Key invariants

- **NULL safety:** `LatestVertrouwelijkheidAanduiding` is nullable. The `.Value` access in SQL
  produces `UNKNOWN` (not `TRUE`) when the column is `NULL`, so rows without a version are
  excluded — consistent with the previous JOIN-based behavior.
- **`SET LOCAL` requires a transaction:** The `enable_bitmapscan = off` hint for the count is issued
  with `SET LOCAL` inside `BeginTransactionAsync`. Without an active transaction, PostgreSQL silently
  promotes `SET LOCAL` to a session-scoped `SET`, which would affect subsequent queries on the same
  pooled connection. The transaction is disposed after the count query, automatically reverting the
  setting.
- **`TempInformatieObjectAuthorization` is session-scoped:** The temp table is created once per
  request and holds at most 25 rows. Fetching it into C# memory for the inline predicate is
  negligible overhead.
- **Cache key stability:** Count and anchor cache keys include `_rsin` and all filter parameters,
  serialized via `ObjectHasher.ComputeSha1Hash`. Adding a new filter field to the model
  automatically changes the hash, invalidating stale cached counts.
- **Composite anchor requires the `Id` tie-breaker:** `CreationTime` is not unique, so the cursor
  seek must compare `(CreationTime, Id)` together. Seeking on `CreationTime` alone would skip or
  duplicate rows that share a timestamp at a page boundary.
- **Result order is now `(CreationTime DESC, Id ASC)`:** This is an observable API behavior change
  (newest first) and the reason the dedicated cursor-paging covering index exists — both phase-1
  paging and the cursor seek must match this order to avoid a Sort node. The order is defined once in
  the `OrderByPage` helper; the keyset seek predicate's correctness depends on every paging path
  using it, so the ordering must not be inlined separately anywhere.
