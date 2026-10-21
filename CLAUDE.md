# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context

.NET 10 / ASP.NET Core e-commerce microservices, orchestrated by .NET Aspire (AppHost SDK 13.x). No tests, real database, Dockerfile or CI exist yet. The README and the per-project `src/*/README.md` files are in French.

## Commands

```bash
dotnet build ECommerce.slnx                      # build all 6 projects
dotnet run --project src/ECommerce.AppHost       # start everything + Aspire dashboard (open the "Login URL" it prints)
dotnet dev-certs https --trust                   # one-time; otherwise the dashboard fails with UntrustedRoot
```

- Docker is not required. Ports change on every run, so read them from the AppHost output or the dashboard.
- There are no test projects yet. If you add one, register it in `ECommerce.slnx`.
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
- **Data**: each service has its own EF Core **InMemory** database (`UseInMemoryDatabase`) and calls `EnsureCreated()` at startup. Catalog seed products come from `HasData` in `CatalogDbContext`. Nothing persists across restarts.
- **No shared contracts project.** DTOs are duplicated per consumer: Ordering's `CatalogProduct` in `Services/CatalogServiceClient.cs` and Web's `*Dto` records in `Services/*ApiClient.cs`. When you change an API model, update its consumers by hand.
- **Order creation** (`Ordering.Api/Endpoints/OrderingEndpoints.cs`): makes one synchronous Catalog GET per line. A 404 becomes a 400 response, and name and price are copied into `OrderItem`. Stock is not checked or decremented, and an order that passes is saved directly as `Confirmed` (`Pending` is never persisted). `Order.Total` is a computed property, not stored. Ordering serializes enums as strings (`JsonStringEnumConverter`), and Web reads `Status` as a `string`.
- **Web** is Blazor with interactive server rendering. Pages live in `Components/Pages/` (Products, Orders, Home), and the cart is held as component state in `Products.razor`.
