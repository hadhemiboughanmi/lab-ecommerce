# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context

.NET 10 / ASP.NET Core e-commerce microservices, orchestrated by .NET Aspire (AppHost SDK 13.x) in development. Catalog runs on PostgreSQL; the four services also ship as container images. The README and the per-project `src/*/README.md` files are in French.

## Commands

```bash
dotnet build ECommerce.slnx                      # build all projects (src + tests)
dotnet test ECommerce.slnx                       # integration tests (Catalog ones start PostgreSQL with Testcontainers)
dotnet test tests/ECommerce.Catalog.Tests --filter "FullyQualifiedName~CreateProduct"   # a subset
dotnet run --project src/ECommerce.AppHost       # start everything + Aspire dashboard (open the "Login URL" it prints)
dotnet dev-certs https --trust                   # one-time; otherwise the dashboard fails with UntrustedRoot
dotnet ef migrations add <Name> --project src/ECommerce.Catalog.Api   # new migration (design-time factory, no DB needed)
docker compose up -d --build                     # whole stack in containers, http://localhost:8080 (needs .env, see .env.example)
```

- **Docker is required**: Aspire starts PostgreSQL in a container (fixed host port 5433), and Testcontainers needs it for the tests. Other ports change on every run, so read them from the AppHost output or the dashboard.
- Test projects live in `tests/` and are registered in `ECommerce.slnx`.
- If the AppHost is running, it locks `bin/Debug`: build or test with `-c Release` meanwhile.
- A clean build currently reports NuGet vulnerability warnings (NU1902/NU1903) for `MessagePack` (pulled in by AppHost). These are known warnings, not build errors. `Microsoft.OpenApi` is pinned to 2.7.5 in Catalog and Ordering to fix CVE-2026-49451.

## Conventions
- Les services s'appellent par nom (https+http://catalog) : JAMAIS d'URL en dur.
- Branches : feat/, fix/, chore/, test/, ci/ — jamais de travail direct sur main.
- Commits : Conventional Commits, en anglais, à l'impératif ; scopes : catalog, ordering, gateway, web, db, ci (ex. `feat(ordering): validate stock on order creation`).
- Un endpoint = un test d'intégration WebApplicationFactory dans tests/.
- `src/ECommerce.Web/CLAUDE.md` ajoute des règles front (accessibilité, pas de nouvelle dépendance sans accord) : à lire avant de toucher au projet Web.

## Interdits
- Aucune donnée client (nom, e-mail) dans un log, une trace ou une exception.
- Aucun secret dans le code ou appsettings.json : dotnet user-secrets ou variable d'environnement.
- Ne jamais modifier un test pour faire passer un build : corriger le code.
- Ne jamais modifier une migration EF Core déjà committée : en ajouter une nouvelle.

## Architecture

Request flow: **Web (Blazor Server) → Gateway (YARP) → Catalog / Ordering**, with **Ordering → Catalog** for product validation.

- **`src/ECommerce.AppHost/AppHost.cs`** is the topology. It declares the resource names (`catalog`, `ordering`, `gateway`, `web`), the `WithReference` wiring and the `WaitFor` startup ordering. A new service, or a new dependency between services, must be added here.
- **Resource names** are resolved at runtime by Aspire service discovery. A name must match across `AppHost.cs`, the typed-client registrations in each `Program.cs`, and the Gateway's YARP config.
- **Gateway** (`src/ECommerce.Gateway/appsettings.json`): YARP routes are defined in config. `/catalog/{**catch-all}` and `/ordering/{**catch-all}` strip the prefix and forward to the clusters, which are resolved through `AddServiceDiscoveryDestinationResolver()`. Web calls services only through the gateway (`/catalog/api/...`, `/ordering/api/...`), never directly.
- **ServiceDefaults** (`src/ECommerce.ServiceDefaults/Extensions.cs`): every service calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`. Together these set up OpenTelemetry, the `/health` and `/alive` endpoints (mapped in Development only), service discovery, and the standard resilience handler on **all** HttpClients via `ConfigureHttpClientDefaults`. That handler also retries POSTs on 5xx and timeouts.
- **APIs** use Minimal APIs. Endpoints live in `Endpoints/*Endpoints.cs` as `Map*Endpoints` extension methods on a `MapGroup("/api/...")`. OpenAPI is served at `/openapi/v1.json` in Development only.
- **Data**: Catalog uses **PostgreSQL** through `AddNpgsqlDbContext<CatalogDbContext>("catalogdb")`; the AppHost declares `AddPostgres` (port 5433, data volume) and the `catalogdb` database. Schema changes go through EF Core migrations in `src/ECommerce.Catalog.Api/Migrations/` (a design-time factory, `CatalogDbContextFactory`, lets `dotnet ef` run without a connection). `Database.Migrate()` runs at startup **in Development only**. Seed data (3 categories, 4 products with SKUs) comes from `HasData`. Ordering still uses EF Core **InMemory** with `EnsureCreated()`: its orders do not persist across restarts.
- **Catalog API**: `GET /api/categories`; `GET /api/products?categoryId=&search=`; `POST /api/products` requires `Sku` and `CategoryId` (400 unknown category, 409 duplicate SKU, also caught from the unique index).
- **No shared contracts project.** DTOs are duplicated per consumer: Ordering's `CatalogProduct` in `Services/CatalogServiceClient.cs` and Web's `*Dto` records in `Services/*ApiClient.cs`. When you change an API model, update its consumers by hand.
- **Order creation** (`Ordering.Api/Endpoints/OrderingEndpoints.cs`): rejects an empty order or a line with a quantity ≤ 0 (400) before calling Catalog, then makes one synchronous Catalog GET per line. A 404 becomes a 400 response, and name and price are copied into `OrderItem`. Stock is not checked or decremented, and an order that passes is saved directly as `Confirmed` (`Pending` is never persisted). `Order.Total` is a computed property, not stored. Ordering serializes enums as strings (`JsonStringEnumConverter`), and Web reads `Status` as a `string`.
- **Web** is Blazor with interactive server rendering. Pages live in `Components/Pages/` (Products, Orders, Home), and the cart is held as component state in `Products.razor`.
- **Tests** (`tests/`): `WebApplicationFactory` integration tests. Catalog tests run against a real PostgreSQL (`CatalogApiFactory`, Testcontainers); Ordering tests replace the `CatalogServiceClient` HTTP handler with a fake catalog (`OrderingApiFactory`, which also sets `services:catalog:http:0`).
- **Containers**: each service has a `Dockerfile` (build context = repo root, chiseled runtime, non-root user 1654, port 8080, no environment or secret baked in). `docker-compose.yml` replaces Aspire service discovery with `services__<name>__http__0` variables and reads the DB password from an untracked `.env`.
- **CI/CD** (`.github/workflows/`): `ci.yml` (build, tests, idempotent migration script as artifact), `images.yml` (builds the four images, fails on root or fixable CRITICAL vulnerabilities), `claude-review.yml` (headless review commented on each PR), `cd.yml` (staging after a green CI on main; production is manual and gated by the protected `production` environment). `main` is protected: PR and green CI required.
- **Claude Code guardrails** (`.claude/settings.json`): hooks block destructive commands, commits containing secrets, edits to committed migrations, conclusions on red tests, and scan every image built with Trivy.
