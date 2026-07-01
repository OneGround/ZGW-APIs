# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

C# / .NET 8 implementation of the VNG Realisatie "APIs voor Zaakgericht Werken" (ZGW) standard — case-management APIs for Dutch government. Each API is an independent microservice; one installation is multi-tenant (serves multiple organisations, partitioned by RSIN). Source lives under `src/`.

## Commands

All commands run from `src/` unless noted.

```bash
# Build the backend (all services + listeners, excludes test projects)
dotnet build ZGW.Backend.slnf

# Build everything (services + tests)
dotnet build ZGW.all.sln

# Run all unit tests
dotnet test ZGW.UnitTests.slnf

# Run one test project
dotnet test Tests/OneGround.ZGW.Zaken.WebApi.UnitTests/ZGW.Zaken.WebApi.UnitTests.csproj

# Run a single test by name
dotnet test ZGW.UnitTests.slnf --filter "FullyQualifiedName~CreateZaak"

# Run a service locally (from its WebApi dir)
dotnet run --project OneGround.ZGW.Zaken.WebApi
```

CI (`.github/workflows/ci-dotnet-build-and-test.yml`) builds `ZGW.Backend.slnf` and tests `ZGW.UnitTests.slnf` in **Release**. Match that before claiming green.

Full local stack (Postgres, RabbitMQ, Keycloak, Ceph, HAProxy) via Docker Compose — see `localdev/README.md`:

```bash
cd localdev && docker compose --env-file ./.env up -d
```

## Formatting

Code is formatted with **CSharpier** (`.csharpierrc.yaml`: printWidth 150, 4 spaces). `CSharpier.MsBuild` runs on build via `Directory.Build.props`, so formatting violations surface at build time. `MSB9008` is treated as an error (`WarningsAsErrors`).

## Service / namespace map

Each API has a Dutch name and a 2–3 letter role code (`ServiceRoleName` in `OneGround.ZGW.Common.Abstractions/Constants/`). Commit messages and code use the codes:

| Code | API | Dutch | Purpose |
|------|-----|-------|---------|
| ZTC | Catalogi | Zaaktypecatalogus | Case-type catalogs and related types |
| ZRC | Zaken | Zaakregistratie | Cases + links to docs/decisions/contacts |
| DRC | Documenten | Documentregistratie | Information objects (documents, media) |
| BRC | Besluiten | Besluitregistratie | Decisions |
| NRC | Notificaties | Notificatieroutering | Subscriptions + change notifications |
| AC | Autorisaties | Autorisatie | App access/authorization |
| RL | Referentielijsten | — | Reference lists (process/result types etc.) served from static data bundled in `Web/Data/`; no DB |

Async work runs in two message listeners (Hangfire + MassTransit/RabbitMQ): `OneGround.ZGW.Documenten.Messaging.Listener` (DRC_LISTENER) and `OneGround.ZGW.Notificaties.Messaging.Listener` (NRC_LISTENER).

> By design, csproj/assembly names drop the `OneGround` prefix for brevity — e.g. dir `OneGround.ZGW.Zaken.Web` holds `ZGW.Zaken.Web.csproj` (assembly `OneGround.ZGW.Zaken.Web`). The `Roxit.ZGW.*` directories under `src/` and `src/Tests/` are stale leftovers (`bin`/`obj` only), not in any solution — work only in `OneGround.ZGW.*`.

## Per-API project layering

Each API is split into projects with a fixed dependency direction:

- **`*.WebApi`** — thin host. `Program.cs` wires host defaults, auth, DataProtection, then delegates to `Startup` in the `*.Web` project. Holds `appsettings*.json`, `Dockerfile`, background services. Has almost no business logic.
- **`*.Web`** — the core. Contains `Controllers/` (versioned), `Handlers/` (MediatR command/query handlers, the actual logic), `BusinessRules/`, `Validators/` (FluentValidation), `MappingProfiles/` (AutoMapper), `Notificaties/`, and the `Startup` class.
- **`*.DataModel`** — EF Core entities + `DbContext` (e.g. `ZrcDbContext`) + `Migrations/`.
- **`*.Contracts`** — request/response DTOs, versioned (`v1`, `v1._2`, `v1._5`).
- **`*.ServiceAgent`** + **`*.ClientProxy`** — typed HTTP clients one service uses to call another (e.g. ZRC handlers call ZTC via `ICatalogiServiceAgent`).

`OneGround.ZGW.Common*` and `OneGround.ZGW.DataAccess` hold shared infrastructure used by all of the above. `OneGround.ZGW.Common.Web` is especially central (auth, authorization, error handling, middleware, versioning, swagger, audit-trail services).

## Request flow & conventions

- **CQRS via MediatR.** Controllers map HTTP → a Command/Query and `Send` it. The corresponding `*CommandHandler` / `*QueryHandler` in `*.Web/Handlers/` does the work and returns a `CommandResult<T>` carrying a `CommandStatus` (Ok / Forbidden / ValidationError / NotFound …) plus `ValidationError[]`. The controller translates that into the HTTP response.
- **API versioning is structural.** Versions are real directory/namespace segments: `Handlers/v1/5/...`, `Contracts/v1._5/...`, controllers under `Controllers/v1/5/`. Supported versions are declared in each API's `Controllers/Api.cs` (`Api.LatestVersion_*` + `ApiMetaData.SupportedVersions`). When adding endpoints, place them under the correct version folder and update `Api.cs`.
- **Multi-tenancy.** Entities carry an `Owner` (RSIN), set from the authenticated context (`_rsin`) in handlers. Authorization is checked per request via `IAuthorizationContext.IsAuthorized(type, vertrouwelijkheid, scope)` before mutating.
- **Handlers inherit a per-API base** (e.g. `ZakenBaseHandler<T>`) which supplies logger, config, auth context, URI service, and notification service.
- **Persistence.** EF Core + PostgreSQL (Npgsql), with NetTopologySuite (geo) and NodaTime. `DbContext`s derive from `BaseDbContext` in `OneGround.ZGW.DataAccess` (audit-trail and encryption hooks live there). After changing a `*.DataModel`, generate an EF migration into that project's `Migrations/`.
- **Encryption at rest.** Sensitive columns use DataProtection; searchable fields use HMAC hashing — see `docs/DATAPROTECTION.md` and `OneGround.ZGW.DataAccess/Encryption/`.
- **Inter-service calls** go through ServiceAgents (typed `HttpClient`s registered in `Startup`), never direct DB access across APIs.

## Key tech

MediatR (CQRS), FluentValidation, AutoMapper, EF Core 8 + Npgsql, MassTransit + RabbitMQ (eventing), Hangfire + Hangfire.PostgreSql (background jobs), Serilog, Duende OAuth2 introspection / JWT (machine-to-machine auth via Keycloak), Asp.Versioning, Swashbuckle, Ceph or filesystem for document blobs. Central package versions in `src/Directory.Packages.props` (`ManagePackageVersionsCentrally`).

## Further docs

- `docs/AUTHENTICATION.md` — OAuth2 access tokens + legacy ZGW tokens
- `docs/AUDITTRAIL.md` — audit-trail mechanics
- `docs/DATAPROTECTION.md` — encryption/HMAC strategy + key generation
- `docs/LOGS.md` — Serilog configuration
- `docs/nrc/CIRCUIT_BREAKER.md` — per-subscription circuit breaker for notification delivery: state machine, Redis keys, automatic blocking
- `localdev/README.md` — full local Docker stack, Keycloak, certs
