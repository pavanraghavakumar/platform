# Hospital Website Management

ASP.NET Core MVC hospital website foundation targeting .NET 7. The solution keeps public presentation, application use cases, domain rules, and infrastructure adapters in separate layers while retaining SQLite as the system of record.

## Prerequisites

- .NET 7 SDK (7.0.203 or newer)
- Node.js 20 or newer (for Playwright journeys)

## Install

```sh
dotnet restore HospitalWebsiteManagement.sln
npm ci
```

## Run locally

```sh
dotnet run --project src/Hospital.Web/Hospital.Web.csproj --environment Development
```

Set `PORT` to bind a specific loopback port. Without it, development uses a free operating-system-selected port. `PORT` controls only server binding and never changes the hosting environment or account-seeding policy. The health endpoint is `/health`. On first development start, the app creates its workspace-local SQLite database and the four accounts documented in the design; an existing database containing accounts is left unchanged. Production never creates development accounts and creates the first Admin only when `FIRST_ADMIN_EMAIL` and `FIRST_ADMIN_PASSWORD` are configured.

## Test

```sh
dotnet test HospitalWebsiteManagement.sln --no-restore
npm run test:e2e
```

Copy `.env.example` to a local `.env` only if your launch tooling loads dotenv files. ASP.NET Core environment variables use double underscores for nested configuration keys. Never commit real credentials.
