# Plan: Optimize EnkelvoudigInformatieObject Query Performance

## TL;DR
The `GetAllEnkelvoudigInformatieObjectenQueryHandler` (v1._5) has several EF Core query anti-patterns causing severe performance degradation on large datasets. The biggest issues are: (1) UUID filtering via `Id.ToString()` which defeats PK index usage, (2) Trefwoorden filtering with nested `Any()` generating inefficient SQL, and (3) missing covering indexes for the combined cross-table filter + pagination pattern. The fix involves query rewrites, new indexes, and optionally keyset pagination.

## Analysis Summary

**Handler location:** `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`

**Query flow:**
1. Filter by `Owner` (RSIN) on `enkelvoudiginformatieobjecten` table
2. Filter by `Bronorganisatie`, `Identificatie`, `Uuid_In`, `Trefwoorden_In` — the last 3 require access to `LatestEnkelvoudigInformatieObjectVersie` navigation (JOIN to `enkelvoudiginformatieobjectversies`)
3. Optionally JOIN to temp authorization table for non-admin users
4. COUNT all matches (cached 1 min)
5. ORDER BY Id, SKIP/TAKE for pagination
6. Include `LatestEnkelvoudigInformatieObjectVersie` + `BestandsDelen`

**Existing indexes on `enkelvoudiginformatieobjecten`:**
- `(InformatieObjectType)`
- `(Owner)`
- `(LatestEnkelvoudigInformatieObjectVersieId)` UNIQUE
- `(Owner, InformatieObjectType, LatestEnkelvoudigInformatieObjectVersieId)`

**Existing indexes on `enkelvoudiginformatieobjectversies`:**
- `(Bronorganisatie)`
- `(Identificatie)`
- `(Owner, Identificatie, Versie)` UNIQUE
- `(EnkelvoudigInformatieObjectId)`
- Various other composite indexes for storage calculations

---

## Steps

### Phase 1: Query Rewrites (Critical — no schema changes needed)

**Step 1: Fix UUID filter anti-pattern** *(Critical)*
- **File:** `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`
- **Problem:** `filterUuid_In.Contains(e.Id.ToString())` converts every `Guid Id` to string in SQL (`CAST(id AS text)`), defeating the PK index and forcing a sequential scan.
- **Fix:** Parse `filter.Uuid_In` to `List<Guid>` and use `filterUuid_In.Contains(e.Id)`. EF Core translates `Contains(Guid)` to `id = ANY(@p)` which uses the PK index.
- Change `GetEnkelvoudigInformatieObjectFilterPredicate` method:
  - Replace `filter.Uuid_In?.Select(u => u.ToLower()).ToList()` → `filter.Uuid_In?.Select(Guid.Parse).ToList()`
  - Replace `filterUuid_In.Contains(e.Id.ToString())` → `filterUuid_In.Contains(e.Id)`

**Step 2: Optimize Trefwoorden filter** *(High)*
- **File:** Same handler file
- **Problem:** `e.LatestEnkelvoudigInformatieObjectVersie.Trefwoorden.Any(e => filter.Trefwoorden_In.Any(f => f == e))` generates a nested subquery with cross-join semantics in PostgreSQL, which is very expensive.
- **Fix:** Convert to use Npgsql's array containment/overlap support. Use `e.LatestEnkelvoudigInformatieObjectVersie.Trefwoorden.Any(t => filter.Trefwoorden_In.Contains(t))` — Npgsql translates `.Any(x => array.Contains(x))` to the `&&` (overlap) operator, which can use GIN indexes.
- Capture `filter.Trefwoorden_In` as a local `List<string>` variable before the expression to ensure it's parameterized.

### Phase 2: Database Index Additions

**Step 3: Add GIN index on `Trefwoorden` column** *(High — depends on Step 2)*
- **File:** `ZGW_APIs/src/OneGround.ZGW.Documenten.DataModel/DrcDbContext.cs` (OnModelCreating) + new migration
- **Problem:** No index exists on the `Trefwoorden` (PostgreSQL `text[]` array) column. Array overlap queries require a GIN index for performance.
- **Fix:** Add GIN index via EF Core migration:
  ```
  modelBuilder.Entity<EnkelvoudigInformatieObjectVersie>()
      .HasIndex(e => e.Trefwoorden)
      .HasMethod("gin");
  ```
- Generate migration with `dotnet ef migrations add AddGinIndexOnTrefwoorden`

**Step 4: Add covering index for the list query pattern** *(Medium)*
- **File:** Same DbContext file + new migration
- **Problem:** The main query filters by `Owner` on EIO, then JOINs to versie table via `LatestEnkelvoudigInformatieObjectVersieId`, then orders by `Id`. The existing `(Owner)` index doesn't cover the sort. The query planner may choose a sequential scan + sort.
- **Fix:** Add composite index on `EnkelvoudigInformatieObject`:
  ```
  (Owner, Id) INCLUDE (InformatieObjectType, LatestEnkelvoudigInformatieObjectVersieId)
  ```
  This covers: RSIN filter → PK sort → FK lookup for the JOIN and authorization join, all from a single index scan.

**Step 5: Add composite index on versie for filter lookups via FK** *(Medium)*
- **Problem:** When the query JOINs versie via `LatestEnkelvoudigInformatieObjectVersieId` and then filters on `Bronorganisatie` or `Identificatie`, the existing single-column indexes on those columns aren't optimal because the JOIN is by versie `Id` (PK), not by those columns.
- **Fix:** Add a covering index on `EnkelvoudigInformatieObjectVersie`:
  ```
  (Id) INCLUDE (Bronorganisatie, Identificatie, Vertrouwelijkheidaanduiding)
  ```
  This extends the existing `idx_e0_light_covering` concept for the list-query filter pattern. Check if the existing `idx_e0_light_covering` (which includes `Owner, Vertrouwelijkheidaanduiding, EnkelvoudigInformatieObjectId`) can be extended or if a new index is needed.

### Phase 3: Optional Further Optimizations

**Step 6: Consider keyset pagination** *(Low — optional, high-effort)*
- **Problem:** `Skip(size * (page - 1)).Take(size)` with `OrderBy(Id)` degrades as page number grows (PostgreSQL must scan and discard all skipped rows).
- **Fix:** Implement keyset pagination using `WHERE Id > @lastId ORDER BY Id LIMIT @size`. This is O(1) seek regardless of page depth.
- **Trade-off:** Requires API contract change (replacing `page` parameter with a cursor/`lastId` parameter). This may conflict with ZGW API standards. *Only pursue if standard allows it.*

**Step 7: Evaluate the authorization JOIN pattern** *(Low — research)*
- The temp table authorization pattern (`CREATE TEMPORARY TABLE` + `INSERT` + `JOIN`) is functional but adds overhead per request. For users with few authorization entries, this is fine. For high-concurrency scenarios, investigate whether caching authorization results or using CTEs would be more efficient.
- No code change needed now — just measure if this is a bottleneck.

---

## Relevant Files

- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs` — query handler with filter predicate (Steps 1, 2)
- `ZGW_APIs/src/OneGround.ZGW.Documenten.DataModel/DrcDbContext.cs` — index definitions in `OnModelCreating` (Steps 3, 4, 5)
- `ZGW_APIs/src/OneGround.ZGW.Documenten.DataModel/EnkelvoudigInformatieObject.cs` — entity definition
- `ZGW_APIs/src/OneGround.ZGW.Documenten.DataModel/EnkelvoudigInformatieObjectVersie.cs` — versie entity (Trefwoorden column)
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Models/v1/5/GetAllEnkelvoudigInformatieObjectenFilter.cs` — filter model (Uuid_In as string[])

## Verification

1. **Step 1 verification:** Enable EF Core SQL logging (`Microsoft.EntityFrameworkCore.Database.Command` → `Information`), run query with `uuid__in` parameter, confirm SQL uses `id = ANY(@p)` instead of `CAST(id AS text)`
2. **Step 2 verification:** Run query with `trefwoorden` parameter, confirm SQL uses `&&` (overlap) operator instead of nested EXISTS/subquery
3. **Step 3 verification:** Run `EXPLAIN ANALYZE` on the trefwoorden query after GIN index is added, confirm index scan instead of seq scan
4. **Step 4 verification:** Run `EXPLAIN ANALYZE` on the main list query (Owner filter + ORDER BY Id + LIMIT), confirm index-only scan or index scan on the new composite index
5. **Step 5 verification:** Run `EXPLAIN ANALYZE` on queries with Bronorganisatie/Identificatie filters, confirm the covering index is used for the JOIN lookup
6. **Unit tests:** Run existing unit tests: `dotnet test tests/` for Documenten-related test projects
7. **Integration check:** Test with pagination (page 1 and a deep page like page 100) to confirm results are identical before and after changes

## Decisions

- Steps 1-2 are pure query rewrite fixes that don't require schema changes and should be done immediately
- Steps 3-5 require EF Core migrations and database deployment — can be batched into a single migration
- Step 6 (keyset pagination) is excluded from scope unless ZGW standards permit cursor-based pagination
- Step 7 (authorization optimization) is research-only, no implementation

## Further Considerations

1. **Uuid_In parsing safety:** The current code calls `.ToLower()` on UUID strings. Changing to `Guid.Parse` will throw on invalid input. Consider whether validation should happen in the controller/validator layer (recommended) or with `Guid.TryParse` fallback in the predicate builder.
2. **GIN index maintenance cost:** GIN indexes on array columns add write overhead. Given that documents are read-heavy and write-infrequent after initial creation, this trade-off is favorable.
3. **Migration deployment:** The new indexes (Steps 3-5) should be created with `CONCURRENTLY` option if possible to avoid table locks on production. EF Core doesn't support `CREATE INDEX CONCURRENTLY` natively — may need a raw SQL migration.

---

## Phase 4: Post-Index Analysis (Execution Plans After New Indices + VACUUM)

### Results Summary

| Query | Before | After Indices | Improvement |
|-------|--------|--------------|-------------|
| Count | 285s | 53s | ~5.4× |
| GetAll 2a (main page) | 763s | 167s | ~4.6× |
| GetAll 2b (bestandsdelen) | 208s | 66ms | ~3150× |

Query 2b is now excellent. Queries 1 and 2a are still unacceptably slow.

### Root Cause: Why Query 2b Is Fast but 2a Is Slow

Both queries filter the same ~1M rows for owner `852256450`, but PostgreSQL chooses **completely different execution strategies**:

**Query 2b (66ms)** — Uses the new `t3b_IX_eio_owner_id_incl_type_latest` index:
- Parallel Index Only Scan produces rows already sorted by `(owner, id)`
- For each row, checks authorization via PK lookup in 20-row temp table
- `Gather Merge + Limit 100` enables **early termination** — stops after ~650 rows
- Only 116 versie lookups needed (not 1M)

**Query 2a (167s)** — Uses the old `ix_..._owner_informatieobjecttype_la` index:
- Authorization table (20 rows) drives the outer loop
- 20 separate index scans on EIO, each returning ~50K rows = 1,009,880 total
- For EACH of those 1M rows, fetches ALL columns from versie table (PK lookup)
- Materializes all 1M wide rows, sorts entire result set, then takes 100
- **No early termination** — must process all rows before sorting

**Why the planner chooses differently**: Query 2a includes `SELECT e.*, e0.*` (all versie columns = wide rows). The planner avoids the sorted index approach because it estimates random I/O for wide rows is expensive. Query 2b's inner subquery only selects `(e.id, e0.id)` (narrow), so the planner uses the sorted index.

### Root Cause: Count Query (53s)

The count must process ALL ~1M matching rows (no early termination possible). Breakdown:
- Inner nested loop (auth × EIO): 2.9 seconds for 1M rows
- Outer loop (versie lookup per row): 0.049ms × 1,009,880 = **49.5 seconds**

The 1M individual B-tree lookups in the versie covering index dominate execution time.

Additionally, the planner **severely underestimates** cardinality: estimates 7,692 rows vs actual 998,709 (130× off). This is because the temporary authorization table has no statistics. The planner estimates ~1,154 EIO rows per auth type, but actual is ~50,494.

### Step 8: Two-Phase Paginated Query *(Critical — implemented)*

**File:** `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`

**Problem:** The single AsSplitQuery() approach generates Query 2a which fetches all columns in a single wide query. The planner refuses to use the sorted (owner, id) index because of the wide output, so it materializes all 1M rows and sorts them.

**Fix:** Split the paginated query into two phases:
1. **Phase 1 — Find IDs**: `query.OrderBy(e => e.Id).Skip(skip).Take(take).Select(e => e.Id)` — narrow output, planner uses sorted index with early termination (~66ms, same strategy as Query 2b)
2. **Phase 2 — Fetch Data**: `_context.EIO.Where(e => ids.Contains(e.Id)).Include(versie).ThenInclude(bestandsdelen)` — 100 PK lookups (~10ms)

**Expected improvement:** 167s → ~100ms (1600× faster)

**Generated SQL Phase 1:**
```sql
SELECT e.id
FROM enkelvoudiginformatieobjecten e
LEFT JOIN enkelvoudiginformatieobjectversies e0 ON e.latest_id = e0.id
WHERE e.owner = @p0
AND EXISTS (SELECT 1 FROM "TempInformatieObjectAuthorization" t
            WHERE t."InformatieObjectType" = e.informatieobjecttype
            AND e0.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")
ORDER BY e.id LIMIT 100 OFFSET 0
```

**Generated SQL Phase 2:**
```sql
SELECT e.*, e0.*, b.*
FROM enkelvoudiginformatieobjecten e
LEFT JOIN enkelvoudiginformatieobjectversies e0 ON e.latest_id = e0.id
LEFT JOIN bestandsdelen b ON e0.id = b.versie_id
WHERE e.id = ANY(@ids)
ORDER BY e.id
```

### Step 9: Count Query — Further Optimization Options *(Medium — not yet implemented)*

The count query (53s) is fundamentally bottlenecked by 1M versie lookups for the `vertrouwelijkheidaanduiding` authorization check. Options to reduce this:

**Option A: Increase cache TTL** (trivial)
The count is already cached for 1 minute. For a 1M-row dataset, exact counts change slowly. Increase to 5-10 minutes to reduce frequency of the expensive query. Trade-off: less accurate count on pagination UI.

**Option B: Run ANALYZE on tables** (DBA action)
The planner underestimates cardinality by 130× because of stale statistics. Running `ANALYZE enkelvoudiginformatieobjecten;` and `ANALYZE enkelvoudiginformatieobjectversies;` would give the planner accurate statistics, potentially leading it to choose a Hash Join (O(N+M)) instead of Nested Loop (O(N×log M)) for the versie lookup.

```sql
ANALYZE enkelvoudiginformatieobjecten;
ANALYZE enkelvoudiginformatieobjectversies;
```

**Option C: ANALYZE on temp table after insert** (code change)
Add `await _context.Database.ExecuteSqlRawAsync("ANALYZE \"TempInformatieObjectAuthorization\"", cancellationToken)` after inserting authorization rows. This gives the planner accurate statistics about the temp table, improving join order decisions.

**Option D: Denormalize `Vertrouwelijkheidaanduiding`** (schema change — high effort, high reward)
Add `Vertrouwelijkheidaanduiding` as a column on `EnkelvoudigInformatieObject` (mirroring the latest versie's value). This eliminates the versie JOIN entirely for both count and paginated queries. The covering index `(owner, id) INCLUDE (type, vertrouw)` would make count an index-only scan on a single table (~3s instead of 53s). Trade-off: requires keeping the denormalized value in sync on every versie update.

**Option E: Apply the same two-phase pattern to v1.0 and v1.1 handlers** (code change)
The v1.0 and v1.1 handlers have the same performance issue. Additionally, v1.0 doesn't cache the count at all. These should be updated with the same two-phase approach and count caching.

---

## Phase 5: Eliminate Top-Level LEFT JOIN (EXISTS Subquery + Conditional Filter Predicates)

### Problem: Two-Phase Query Still Slow (374s)

The Phase 1 ID-only query still materializes all ~1M matching rows before sorting:

```
Limit  (cost=29363..29363 rows=100)  actual time=374370ms rows=100
  -> Sort (top-N heapsort)           actual time=374370ms rows=2100
    -> Nested Loop                   actual time=7ms..373620ms rows=998837 ← ALL rows materialized
```

**Root cause:** Even with `Select(e => e.Id)`, EF Core still generates a **top-level LEFT JOIN** to `enkelvoudiginformatieobjectversies` because:
1. The authorization predicate uses `e.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding` — a navigation property access that forces a JOIN in the FROM clause.
2. The filter predicate uses `e.LatestEnkelvoudigInformatieObjectVersie.Bronorganisatie` etc. — even when those filters are `null`, EF Core still emits the JOIN.

With a 3-way FROM clause (`eio LEFT JOIN versie INNER JOIN auth`), the planner cannot use the `(owner, id)` covering index for ordered scanning with early termination. It must materialize all joined rows, sort them, then take 100.

### Fix: Three Changes (all implemented)

#### Step 10: Auth predicate — navigation property → correlated EXISTS subquery *(Critical)*

**Files changed:**
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`

**Before** (causes top-level LEFT JOIN):
```csharp
query = query.Where(e =>
    _context.TempInformatieObjectAuthorization.Any(a =>
        a.InformatieObjectType == e.InformatieObjectType
        && (int)e.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding
            <= a.MaximumVertrouwelijkheidAanduiding
    )
);
```

**After** (generates nested EXISTS — no top-level JOIN):
```csharp
query = query.Where(e =>
    _context.EnkelvoudigInformatieObjectVersies.Any(v =>
        v.Id == e.LatestEnkelvoudigInformatieObjectVersieId
        && _context.TempInformatieObjectAuthorization.Any(a =>
            a.InformatieObjectType == e.InformatieObjectType
            && (int)v.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
        )
    )
);
```

**Generated SQL** — outer query has NO joins, just EXISTS:
```sql
SELECT e.id FROM enkelvoudiginformatieobjecten e
WHERE e.owner = @p
  AND EXISTS (
    SELECT 1 FROM enkelvoudiginformatieobjectversies v
    WHERE v.id = e.latest_enkelvoudiginformatieobjectversie_id
      AND EXISTS (
        SELECT 1 FROM "TempInformatieObjectAuthorization" t
        WHERE t."InformatieObjectType" = e.informatieobjecttype
          AND v.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding"))
ORDER BY e.id LIMIT 100 OFFSET 0
```

Now PostgreSQL can scan the `(owner, id)` covering index in order and for each row evaluate the EXISTS cheaply (1 index lookup + 1 tiny table scan). It stops after 100 matches.

#### Step 11: Filter predicate — conditional versie navigation *(Critical)*

**Files changed:** Same 3 handler files.

**Problem:** Even with `null` filters, `(filter.Bronorganisatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Bronorganisatie == filter.Bronorganisatie)` causes EF Core to emit a LEFT JOIN because it sees a navigation property access in the expression tree.

**Fix:** Build the predicate conditionally — only reference the versie navigation when versie-based filters are actually set.

**v1.5 handler:**
```csharp
bool hasVersieFilters = filter.Bronorganisatie != null || filter.Identificatie != null || filterTrefwoorden_In != null;
if (!hasVersieFilters)
{
    return e => filterUuid_In == null || filterUuid_In.Contains(e.Id);
}
// ... full predicate with navigation property only when needed
```

**v1.0 and v1.1 handlers:**
```csharp
if (filter.Bronorganisatie == null && filter.Identificatie == null)
{
    return e => true;
}
// ... full predicate with navigation property only when needed
```

#### Step 12: ANALYZE temp table after insert *(Medium — implemented)*

**File:** `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Services/InformatieObjectAuthorizationTempTableService.cs`

Added `ANALYZE "TempInformatieObjectAuthorization"` after saving rows. This gives PostgreSQL accurate statistics about the temp table (20 rows, not the default estimate), improving join strategy decisions. Benefits all 5 handlers that use this service.

#### Step 13: Two-phase paginated query applied to v1.0 and v1.1 *(Medium — implemented)*

**Files:**
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`
- `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/1/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`

Same two-phase pattern as v1.5: Phase 1 fetches page IDs only, Phase 2 fetches full data for matched IDs.

### Expected Performance After Phase 5

| Query | Before Phase 5 | Expected After |
|-------|---------------|----------------|
| Phase 1 (page IDs) | 374s | <100ms |
| Phase 2 (100 PK lookups) | ~10ms | ~10ms |
| Count (cached) | 53s (first hit) | Improved with ANALYZE, still ~30-50s (1M rows) |

The key improvement: with no top-level JOIN, the `FROM` clause is just `enkelvoudiginformatieobjecten` filtered by `owner`. The planner uses the `(owner, id)` covering index for ordered scanning, evaluates the EXISTS subquery per-row (~0.05ms each), and terminates after 100 matches. Total: ~100 rows × 0.05ms = ~5ms plus index seek overhead.

### Remaining Bottleneck: Count Query

The count query must still process all ~1M rows. The EXISTS subquery approach helps the planner choose better strategies, and ANALYZE on the temp table improves join order, but count is fundamentally O(N) where N = matching rows. Options from Step 9 (cache TTL increase, denormalization) remain valid for further improvement.

---

## Phase 6: Verified Results

### Execution Plan — Page 1 (OFFSET 0, LIMIT 100)

```
Limit  (cost=1.13..500.57 rows=100)  actual time=3.797..489.655 rows=100
  -> Nested Loop                     actual time=3.795..489.612 rows=100
    -> Index Only Scan using t3b_IX_eio_owner_id_incl_type_latest on enkelvoudiginformatieobjecten e
         Index Cond: (owner = '852256450')
         actual time=1.975..10.693 rows=581
         Heap Fetches: 7
    -> Index Only Scan using t3b_idx_e0_light_covering on enkelvoudiginformatieobjectversies e0
         Index Cond: (id = e.latest_enkelvoudiginformatieobjectversie_id)
         Filter: (SubPlan 1)
         actual time=0.823..0.823 rows=0 loops=581
         Heap Fetches: 1
         SubPlan 1
           -> Seq Scan on "InformatieObjectAuthorization_1" t
                actual time=0.004..0.004 rows=0 loops=581
Planning Time: 42.002 ms
Execution Time: 490.184 ms
```

**Key characteristics:**
- **581 rows scanned** to find 100 matches (~17% pass rate) — early termination confirmed
- **Index Only Scan** on `(owner, id)` covering index — no Sort node needed
- EXISTS evaluated as SubPlan per-row via index lookup — 0.8ms average
- Limit directly on Nested Loop — stops immediately after 100 output rows
- 7 heap fetches on EIO table, 1 on versie — covering indexes working

### Execution Plan — Deep Page (OFFSET 80000, LIMIT 100)

```
Limit  (cost=399554..400054 rows=100)  actual time=169912..170083 rows=100
  -> Nested Loop                       actual time=0.041..170071 rows=80100
    -> Index Only Scan on enkelvoudiginformatieobjecten e
         actual time=0.022..2235 rows=501427
    -> Index Only Scan on enkelvoudiginformatieobjectversies e0
         actual time=0.334..0.334 rows=0 loops=501427
         SubPlan 1
           -> Seq Scan on "InformatieObjectAuthorization_1" t
                actual time=0.003..0.003 rows=0 loops=501427
Planning Time: 0.442 ms
Execution Time: 170084.536 ms
```

Deep pages are inherently O(OFFSET) — PostgreSQL must scan and discard 80,000 matching rows before returning the next 100. This requires scanning ~501K EIO rows. Only addressable via keyset pagination (Step 6).

### Final Performance Summary

| Query | Original (Before) | Final (After) | Improvement |
|-------|-------------------|---------------|-------------|
| **Page 1 (IDs + data)** | **763s** | **0.5s** | **1558×** |
| Deep page 801 | 763s | 170s | 4.5× |
| **Count (first hit)** | **285s** | **25s** (cached 5 min) | **11.4×** |
| Bestandsdelen subquery | 208s | 66ms | 3150× |

### All Implemented Changes Summary

| Step | Change | File(s) |
|------|--------|---------|
| 1 | UUID filter: `Id.ToString()` → `Guid.Parse` | v1.5 handler |
| 2 | Trefwoorden: nested `Any()` → `Contains()` overlap | v1.5 handler |
| 3 | GIN index on `Trefwoorden` column | DrcDbContext |
| 4 | Covering index `(Owner, Id) INCLUDE (Type, LatestId)` | DrcDbContext |
| 5 | Covering index on versie `(Id) INCLUDE (...)` | DrcDbContext |
| 8 | Two-phase paginated query | v1.5 handler |
| 10 | Auth predicate: navigation → EXISTS subquery | v1.0, v1.1, v1.5 handlers |
| 11 | Filter predicate: conditional versie navigation | v1.0, v1.1, v1.5 handlers |
| 12 | ANALYZE temp table after insert | InformatieObjectAuthorizationTempTableService |
| 13 | Two-phase query for v1.0 and v1.1 | v1.0, v1.1 handlers |
| 14 | JOIN-based count query for authorization | v1.5 handler |
| 15 | Owner filter on versie side of count JOIN | v1.5 handler |
| 16 | `SET LOCAL` in transaction + `work_mem` 256→80MB | v1.5 handler |

---

## Phase 7: Optimize Count Query — JOIN Instead of EXISTS

### Problem

The count query uses the same EXISTS subquery as the paginated query. While EXISTS is perfect for pagination (early termination per-row), for count it forces **1M individual Nested Loop index lookups** on the versie table (~50s). Count cannot benefit from early termination since it must process all matching rows.

### Fix: Separate Count Query Using INNER JOIN (Step 14 — implemented)

**File:** `ZGW_APIs/src/OneGround.ZGW.Documenten.Web/Handlers/v1/5/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs`

When authorization filtering is active, use a separate JOIN-based count query:

```csharp
_context.EnkelvoudigInformatieObjecten.AsNoTracking()
    .Where(rsinFilter)
    .Where(filter)
    .Join(
        _context.EnkelvoudigInformatieObjectVersies,
        e => e.LatestEnkelvoudigInformatieObjectVersieId,
        v => v.Id,
        (e, v) => new { e.InformatieObjectType, v.Vertrouwelijkheidaanduiding }
    )
    .Where(ev =>
        _context.TempInformatieObjectAuthorization.Any(a =>
            a.InformatieObjectType == ev.InformatieObjectType
            && (int)ev.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
        )
    )
    .CountAsync(cancellationToken);
```

**Expected generated SQL:**
```sql
SELECT count(*)::int
FROM enkelvoudiginformatieobjecten e
INNER JOIN enkelvoudiginformatieobjectversies v
    ON e.latest_enkelvoudiginformatieobjectversie_id = v.id
WHERE e.owner = @p
  AND EXISTS (
    SELECT 1 FROM "TempInformatieObjectAuthorization" t
    WHERE t."InformatieObjectType" = e.informatieobjecttype
      AND v.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")
```

**Why this is faster:** With an INNER JOIN, PostgreSQL can use a **Hash Join** strategy:
1. Scan the versie covering index once → build hash table on `(id → vertrouwelijkheidaanduiding)` ~O(N)
2. Scan the EIO covering index for `owner` → probe hash table per row ~O(M)
3. Total: O(N + M) instead of O(M × log N) for the Nested Loop EXISTS approach

The Hash Join processes the entire versie table in a single streaming pass instead of 1M individual B-tree lookups.

**Why we keep EXISTS for pagination:** The paginated query needs early termination (stop after 100 matches). An INNER JOIN materializes ALL joined rows before the Limit can stop them. EXISTS with per-row evaluation + the sorted index lets the planner terminate after scanning just ~600 rows.

### Key Design Decision: Two Query Shapes

| | Paginated query | Count query |
|---|---|---|
| **Shape** | EXISTS subquery (no top-level JOIN) | INNER JOIN |
| **PostgreSQL strategy** | Index scan + early termination | Hash Join (streaming) |
| **Rows processed** | ~600 (page 1) | ~1M (all matches) |
| **Optimal for** | Finding first N matches fast | Counting all matches fast |

---

## Phase 8: Owner Filter on Versie Side of Count JOIN (Step 15)

### Problem

The Phase 7 Hash Join count query scanned **all 69M versie rows** (the entire table) because the JOIN had no filter condition on the versie side. PostgreSQL built the hash table from 1M EIO rows and then probed by scanning all 69M versie rows — taking 72s.

### Fix: Filter versie by owner (Step 15 — implemented)

Added `.Where(v => v.Owner == _rsin)` to the versie side of the `Join()`:

```csharp
.Join(
    // Filter versie by owner to reduce scan from all 69M rows to ~7.8M for this owner
    _context.EnkelvoudigInformatieObjectVersies.Where(v => v.Owner == _rsin),
    e => e.LatestEnkelvoudigInformatieObjectVersieId,
    v => v.Id,
    (e, v) => new { e.InformatieObjectType, v.Vertrouwelijkheidaanduiding }
)
```

### Execution Plan — Count (25s)

```
Finalize Aggregate  actual time=24992ms rows=1
  -> Gather  Workers Planned: 2, Launched: 2
    -> Partial Aggregate  actual time=24915ms rows=1
      -> Parallel Hash Join  actual time=11673..24893ms rows=333,016
            Hash Cond: (e0.id = e.latest_enkelvoudiginformatieobjectversie_id)
            Join Filter: (e0.vertrouwelijkheidaanduiding <= t0."MaximumVertrouwelijkheidAanduiding")
            Rows Removed by Join Filter: 3,724
            -> Parallel Index Only Scan using tmp_idx_eiov_owner_vha_id
                 on enkelvoudiginformatieobjectversies e0
                 Index Cond: (owner = '852256450')
                 actual time=7.6..12058ms rows=2,611,158 loops=3
                 Heap Fetches: 34,669
            -> Parallel Hash  actual time=11658ms
                 Buckets: 1,048,576  Memory Usage: 71,552kB
                 -> Merge Join  actual time=1.9..11409ms rows=336,740 loops=3
                       Merge Cond: (e.informatieobjecttype = t0."InformatieObjectType")
                       -> Parallel Index Only Scan on ix_..._owner_informatieobjecttype_la
                            on enkelvoudiginformatieobjecten e
                            Index Cond: (owner = '852256450')
                            actual time=1.5..11349ms rows=336,741 loops=3
                            Heap Fetches: 84,028
                       -> Sort on "InformatieObjectAuthorization_1" t0
                            actual time=0.4ms rows=20 loops=3
Planning Time: 18.6 ms
Execution Time: 24,992 ms
```

### Analysis

**Improvement:** 72s → 25s (2.9× from owner filter, 11.4× total from original 285s)

**Bottleneck breakdown** (2 workers + leader, running in parallel):
- **Probe side (versie):** Parallel Index Only Scan of 7.8M rows (2.6M/worker × 3) — 12s/worker. This is all versies for owner `852256450`, of which only ~1M are "latest" (avg ~8 versions per document). The Hash Join discards the non-latest versies.
- **Build side (EIO × auth):** Merge Join of 1M EIO rows with 20 auth rows — 11.4s/worker. 84K heap fetches suggest ~8% of index pages need visibility checks.
- **Sequential phases:** Hash build (11.4s) then probe (12s) = ~23-25s total wall clock

**Why 7.8M instead of 1M versie rows:** The owner filter selects ALL versies for this RSIN, not just the "latest" ones. Each document has ~8 versions on average. The Hash Join matches on `v.id = e.latest_id`, naturally discarding the ~6.8M non-latest versies, but they're still scanned.

### Remaining Optimization Options

**Running `VACUUM ANALYZE` on both tables** would reduce heap fetches (visibility map refresh) — potentially shaving 2-5s from both sides.

**Denormalize `Vertrouwelijkheidaanduiding` onto `EnkelvoudigInformatieObject`** (Option D from Phase 4) would eliminate the versie JOIN entirely. The count becomes a single-table index-only scan: ~3-5s for 1M rows. Trade-off: requires keeping the denormalized value in sync on every versie update.

**With 5-minute count cache**, the 25s query runs at most once per 5 minutes per unique filter combination — acceptable for production.

---

## Phase 9: Production Safety of `SET LOCAL enable_nestloop = off` and `work_mem` (Step 16 — implemented)

### Problem: `SET` vs `SET LOCAL` Session Leaking

The Phase 7 implementation originally used session-scoped `SET` with a manual `RESET` in a `finally` block:

```csharp
// BEFORE (Phase 7 — replaced in Step 16)
try
{
    await _context.Database.ExecuteSqlRawAsync("SET enable_nestloop = off; SET work_mem = '256MB'", cancellationToken);
    var result = await countQuery.CountAsync(cancellationToken);
    return result;
}
finally
{
    await _context.Database.ExecuteSqlRawAsync("RESET enable_nestloop; RESET work_mem", CancellationToken.None);
}
```

**Risk:** If the process crashes or the connection is returned to the pool between `SET` and `RESET` (e.g., cancellation token fires after SET but before RESET), the pooled connection retains `enable_nestloop = off` and `work_mem = '256MB'` for subsequent queries — potentially degrading other queries or consuming excessive memory.

### Fix: Use `SET LOCAL` Inside Explicit Transaction (Step 16 — implemented)

`SET LOCAL` is scoped to the **current transaction only** and reverts automatically when the transaction ends (commit or rollback). No manual `RESET` needed.

| Command | Scope | Reverts when | Risk |
|---|---|---|---|
| `SET` | Session (connection) | `RESET` or connection close | Medium — can leak to pooled connection |
| `SET LOCAL` | Transaction | Transaction ends (commit/rollback) | Low — automatic cleanup |

**Important:** `SET LOCAL` requires an active transaction. Without one, PostgreSQL silently treats it as `SET` (session-scoped). EF Core doesn't open transactions for read queries by default.

**Implemented pattern:**
```csharp
// AFTER (Step 16 — current implementation)
using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
await _context.Database.ExecuteSqlRawAsync(
    "SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB';", cancellationToken);
var result = await countQuery.CountAsync(cancellationToken);
// Settings automatically reverted when transaction is disposed — no RESET needed
```

### Problem: `work_mem = '256MB'` Memory Exhaustion Risk

`work_mem` is allocated **per sort/hash operation per worker**, not per query or connection. With parallel hash joins:

```
Parallel Hash → Memory Usage: 71,552kB (~70MB)
Workers Planned: 2 + 1 leader = 3 processes
```

Each parallel worker allocates its own `work_mem`. Worst-case memory per query:

| Setting | Per worker | × 3 workers | Total per query |
|---|---|---|---|
| `work_mem = '64MB'` | 64MB | × 3 | **192MB** |
| `work_mem = '80MB'` | 80MB | × 3 | **240MB** |
| `work_mem = '128MB'` | 128MB | × 3 | **384MB** |
| `work_mem = '256MB'` | 256MB | × 3 | **768MB** |

#### Concurrent Execution Risk

If N different RSINs (tenants) hit the endpoint simultaneously and all miss the cache:

| Concurrent queries | × 256MB setting | Total additional memory |
|---|---|---|
| 5 | × 768MB/query | **3.8 GB** |
| 10 | × 768MB/query | **7.7 GB** |
| 20 | × 768MB/query | **15.4 GB** |

This is **on top of** `shared_buffers` (typically 25% of RAM) and other connections' work_mem. PostgreSQL does not enforce a global work_mem budget — each backend allocates independently.

#### Mitigations in Place

- **5-minute cache** — only first request per RSIN+filter executes the count query
- **Parallel Hash** naturally caps at actual data size (72MB for this dataset)

### Fix: Reduce `work_mem` to Match Actual Need (Step 16 — implemented)

The hash table requires ~72MB. Reduced `work_mem` from `256MB` to `80MB` (72MB + small margin):

```sql
SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB'
```

Worst case per query dropped from 768MB to 240MB.

#### Tested: Disabling Parallelism (62s — **Rejected**)

```sql
SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB'; SET LOCAL max_parallel_workers_per_gather = 0
```

Disabling parallelism reduces memory to 80MB per query but causes a **severe regression** (25s → 62s). Without parallel workers, PostgreSQL falls back to a single-threaded Merge Join:

```
Aggregate  actual time=61863ms rows=1
  -> Merge Join  actual time=41409..61797ms rows=999,435
    -> Index Only Scan on idx_eiov_owner_id_vha (versie)    ← 7.8M rows, single-threaded: 19.6s
         Heap Fetches: 41,333
    -> Sort  Sort Method: external sort  Disk: 33632kB      ← 33MB spills to disk despite 80MB work_mem
      -> Merge Join (EIO × auth)                             ← 1M rows: 40.7s
           Heap Fetches: 164,236
```

The Parallel Hash Join (25s) splits the 7.8M versie scan across 3 processes and uses in-memory hashing. The single-threaded Merge Join scans sequentially and spills the intermediate sort to disk. **Parallelism is essential for this query pattern.**

### Verification: Nested Loops Without SET Hacks (84.6s — Worse)

Tested with `enable_nestloop = on` (PostgreSQL default) after `ANALYZE` on temp table:

```
Aggregate  actual time=84605ms rows=1
  -> Nested Loop  actual time=6.9..84465ms rows=999,416
    -> Nested Loop  actual time=3.6..52734ms rows=1,010,587        ← 1M loops
      -> Seq Scan on "InformatieObjectAuthorization_1" t0           ← 20 rows drive outer loop
      -> Index Only Scan on ix_..._owner_informatieobjecttype_la    ← 50K rows × 20 loops
           Index Cond: (owner = '852256450', informatieobjecttype = t0."InformatieObjectType")
           Heap Fetches: 160,249                                    ← stale visibility map
    -> Index Only Scan on idx_eiov_owner_id_vha                     ← 1M PK lookups
         actual time=0.031..0.031 rows=1 loops=1,010,587
         Heap Fetches: 6,708
```

**84.6s** vs **25-29s** with hash join. The 1M individual B-tree lookups (31.3s) plus 160K heap fetches on stale visibility map (52.7s) make nested loops 3× slower for this workload. `enable_nestloop = off` is the correct choice for this count query.

### Summary: Production Configuration (Implemented)

| Aspect | Before (Phase 7) | After (Step 16 — implemented) |
|---|---|---|
| **Planner setting** | `SET enable_nestloop = off` | `SET LOCAL enable_nestloop = off` (in explicit transaction) |
| **work_mem** | `256MB` | `80MB` (hash table needs 72MB) |
| **Parallelism** | Default (2 workers) | **Keep default** — disabling causes 2.5× regression (62s vs 25s) |
| **Reset** | Manual `RESET` in `finally` | Automatic (transaction-scoped `SET LOCAL`) |
| **Cache TTL** | 5 minutes | 5 minutes (adequate) |
| **Memory per query** | Up to 768MB (3 × 256MB) | Up to 240MB (3 × 80MB) |

### Permanent Fix: Denormalize `Vertrouwelijkheidaanduiding`

The `SET LOCAL` approach is a safe workaround but remains a planner override. The permanent solution is denormalizing `vertrouwelijkheidaanduiding` onto the `enkelvoudiginformatieobjecten` table:

1. **Add column** `vertrouwelijkheidaanduiding` to `enkelvoudiginformatieobjecten` (mirroring the latest versie's value)
2. **Add covering index** `(owner, informatieobjecttype, vertrouwelijkheidaanduiding)` on `enkelvoudiginformatieobjecten`
3. **Update on versie changes** — set the denormalized value whenever a new version is created or the latest version changes
4. **Backfill** existing data via migration: `UPDATE enkelvoudiginformatieobjecten e SET vertrouwelijkheidaanduiding = (SELECT v.vertrouwelijkheidaanduiding FROM enkelvoudiginformatieobjectversies v WHERE v.id = e.latest_enkelvoudiginformatieobjectversie_id)`

**Result:** Count query becomes single-table index-only scan with no planner overrides:

```sql
SELECT count(*)::int
FROM enkelvoudiginformatieobjecten e
WHERE e.owner = @owner
  AND EXISTS (
    SELECT 1 FROM "TempInformatieObjectAuthorization" t
    WHERE t."InformatieObjectType" = e.informatieobjecttype
      AND e.vertrouwelijkheidaanduiding <= t."MaximumVertrouwelijkheidAanduiding")
```

Expected performance: **< 1 second** with default `work_mem = 4MB`. No `SET LOCAL` hacks needed. No memory risk.
