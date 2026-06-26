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

### 3. Count query — JOIN against temp table with planner hints

For the count query (which must scan all matching rows), the inline OR predicate is intentionally
**not** used. At scale, PostgreSQL would choose a BitmapOr plan that becomes lossy, causing
millions of heap rechecks (~29 seconds). Instead, the count query:

- Joins directly against `TempInformatieObjectAuthorization` on `InformatieObjectType`.
- Uses `LatestVertrouwelijkheidAanduiding` on the EIO row (no versie join).
- Conditionally applies `SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB'`
  within a transaction when no selectivity filters are set. This forces the planner to choose
  a Hash Join (single streaming pass, O(N+M)) over Nested Loop.
- The `anyFiltersSet` guard prevents the hint from firing when `Identificatie`,
  `Bronorganisatie`, `Uuid_In`, or `Trefwoorden_In` are provided — in those cases the result
  set is small enough that the planner chooses correctly on its own.

### 4. Cached count and anchor values

Both the total count and the authorization count are cached in distributed cache (Redis) with a
5-minute TTL, keyed on `(rsin, filter parameters)`. This avoids repeated expensive `COUNT(*)`
queries during pagination of the same result set.

### 5. Optional cursor-based pagination (v1.5 only, experimental)

A feature flag `Application:EnkelvoudigInformatieObjectenCursorPaging` enables keyset
(seek-method) pagination for v1.5. When active, the query seeks past the previous page's anchor
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
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs` | Inline auth predicate **grouped by VHA level** (`ANY` per level); `(CreationTime DESC, Id ASC)` ordering centralized in an `OrderByPage` helper shared by all four paging paths; two-phase pagination; cached count; cursor paging with composite `PageAnchor(CreationTime, Id)`; simplified count query via `LatestVertrouwelijkheidAanduiding` |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllVerzendingenQueryHandler.cs` | Auth filter and count query updated to use `LatestVertrouwelijkheidAanduiding` (eliminates versie join) |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllGebruiksRechtenQuery.cs` | Auth filter updated to use `LatestVertrouwelijkheidAanduiding` |
| `src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllObjectInformatieObjectenQuery.cs` | Auth filter updated to use `LatestVertrouwelijkheidAanduiding` |

---

## Key invariants

- **NULL safety:** `LatestVertrouwelijkheidAanduiding` is nullable. The `.Value` access in SQL
  produces `UNKNOWN` (not `TRUE`) when the column is `NULL`, so rows without a version are
  excluded — consistent with the previous JOIN-based behavior.
- **`SET LOCAL` requires a transaction:** The planner hints are issued with `SET LOCAL` inside
  `BeginTransactionAsync`. Without an active transaction, PostgreSQL silently promotes `SET LOCAL`
  to a session-scoped `SET`, which would affect subsequent queries on the same pooled connection.
  The transaction is disposed after the count query, automatically reverting the settings.
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
