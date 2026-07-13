# Cookbook — common tasks

Step-by-step recipes for the most frequent changes. For the surrounding
architecture (project layering, request flow, conventions) see [`../CLAUDE.md`](../CLAUDE.md).

## Add an endpoint to an existing API version

(e.g. a new action on `OneGround.ZGW.Zaken`)

1. `*.Contracts/v{X}/` — add/extend the request/response DTO for that version.
2. `*.Web/Validators/` — add a FluentValidation validator for the new request DTO.
3. `*.Web/Handlers/v{X}/` — add the `*Command`/`*Query` + its `*Handler` (inherit the API's base handler, e.g. `ZakenBaseHandler<T>`).
4. `*.Web/MappingProfiles/` — add AutoMapper entries if the handler maps between contract and entity.
5. `*.Web/Controllers/v{X}/` — add the controller action, `Send`-ing the command/query.
6. `*.Web/Controllers/Api.cs` — only if this changes what a version supports; otherwise skip.
7. Add/extend a test in `Tests/OneGround.ZGW.<Api>.WebApi.UnitTests/`.

## Bump an API to a new minor version

(e.g. `v1.5` → `v1.6`)

1. Duplicate the previous version's folders under `Contracts/`, `Handlers/`, `Controllers/` into the new version segment — don't edit the old version's files, they must keep serving old clients.
2. `Controllers/Api.cs` — add the new version to `ApiMetaData.SupportedVersions` and update `Api.LatestVersion_*` if this becomes latest.
3. Wire the new controller/handler pair as in "Add an endpoint" above.
4. Update the OpenAPI/Swagger version metadata if the API declares it explicitly (check `Startup`).

## Add an EF Core migration

(after changing a `*.DataModel` entity or `DbContext`)

1. Edit the entity / `DbContext` (e.g. `ZrcDbContext`) in the relevant `*.DataModel` project.
2. From that project's directory: `dotnet ef migrations add <Name> --project . --startup-project ../<Api>.WebApi`.
3. Review the generated file in `Migrations/` — check it doesn't silently drop/alter columns holding DataProtection-encrypted or HMAC-hashed data (see [`DATAPROTECTION.md`](DATAPROTECTION.md)).
4. Do not hand-edit already-applied migrations; add a new one instead.

## Call another API from a handler

(inter-service, never direct DB access)

1. Confirm the target API's `*.ServiceAgent` project exposes the method you need (typed `HttpClient` interface, e.g. `ICatalogiServiceAgent`).
2. Inject that interface into your handler's constructor — it's registered in the calling API's `Startup`.
3. If the method doesn't exist yet, add it to the `*.ServiceAgent` interface + implementation, and to the `*.ClientProxy` project if that's where the DTOs for the call live.
