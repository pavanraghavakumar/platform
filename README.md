# Healthcare Management System

A server-rendered ASP.NET Core MVC web application for a hospital, running locally on one computer with a local SQLite database. It gives four kinds of signed-in people — Admin, Doctor, Receptionist and Patient — each with its own dashboard and its own permitted modules.

Everything is local: no cloud hosting, no deployment, no external database server, no email and no SMS.

## Build and run

Install:

```
dotnet restore
```

Test:

```
dotnet test
```

Run locally:

```
dotnet run --project src/Hms.Web --urls http://127.0.0.1:5000
```

The app applies the EF Core migrations and seeds the demo records when the database is empty before it starts serving. The health endpoint is `/health` and answers `200` with `{"status":"ok"}`.

## Demo logins

| Role | User name | Password |
|---|---|---|
| Admin | admin@hms.local | Admin@123 |
| Doctor | dr.arjun@hms.local | Doctor@123 |
| Receptionist | reception@hms.local | Reception@123 |
| Patient | patient.ravi@hms.local | Patient@123 |

The demo records and logins are created only when the database holds no records, and never when `ASPNETCORE_ENVIRONMENT` is `Production`.

## Project layout

- `src/Hms.Web` — the ASP.NET Core MVC application (controllers, Razor views, services, repositories, EF Core `HmsDbContext`).
- `tests/Hms.Tests` — the xUnit test project.
- `e2e/` — Playwright end-to-end journeys, run only through `run_journeys` and excluded from `dotnet test`.

## End-to-end tests

`@playwright/test` is a dev dependency. Install it with `npm install` and run the journeys in `e2e/` with `npx playwright test`.
