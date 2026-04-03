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
