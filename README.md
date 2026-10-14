# GameScoringAPI

Minimal game scoring API. Store games, matches, match data points, compute simple stats (winning player, totals).

## Features
- CRUD for Games, Matches, Match Data Points
- Compute match stats and winning player
- Postgres DB with PL/pgSQL triggers auto-updating match/player counts
- Swagger UI for API exploration
- EF Core migrations that create the schema and the trigger functions/triggers

## Tech Stack
- .NET 10 minimal APIs
- Entity Framework Core 10.0.0 (Postgres Npgsql provider)
- Postgres 12+ with PL/pgSQL functions and triggers
- xUnit tests — unit (EF Core InMemory) + integration (Docker PostgreSQL)

## Quick Start

### Prerequisites
- .NET 10 SDK
- Postgres 12+ (local or remote), **or** Docker for the one-line local database below

### Local Dev with Docker

The committed `docker-compose.yml` starts a local PostgreSQL 16 on **localhost:5432**
(distinct from the integration-test database on 5433) with a named volume and a
healthcheck — no manually-installed Postgres required:

```bash
docker-compose up -d
dotnet run --project Api
```

- The container uses **`postgres` / `postgres`** and database **`gamescoringdb`**, matching
  the `DefaultConnection` example in [Setup](#setup).
- Migrations (including the PL/pgSQL trigger definitions) initialize automatically on startup.
- Stop it with `docker-compose down` (data is kept in the `dev-db-volume` volume); use
  `docker-compose down -v` to also delete the data.
- **Connection precedence:** `ConnectionStrings:DatabaseUrl` (or the env var
  `ConnectionStrings__DatabaseUrl`) takes precedence over `DefaultConnection`. If a
  `DatabaseUrl` is set — e.g. in a local, gitignored `Api/appsettings.Development.json` —
  the app connects there instead of the container, so remove/override it to use the local
  database.

### Setup

1. **Configure Database Connection**

   The app reads `ConnectionStrings:DatabaseUrl` first, then `ConnectionStrings:DefaultConnection`.
   The environment-variable form of that key is `ConnectionStrings__DatabaseUrl`:
   ```bash
   export ConnectionStrings__DatabaseUrl="postgresql://user:pass@localhost:5432/gamescoringdb"
   ```
   
   Or create the local `Api/appsettings.json` (it is **gitignored**, so it is not in the
   repo and is never deployed) with a `DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=gamescoringdb;Username=postgres;Password=postgres"
   }
   ```

2. **Build & Apply Migrations**

   ```bash
   dotnet build GameScoringAPI.sln
   cd Api
   dotnet ef database update
   ```
   
   This creates the schema and the PL/pgSQL trigger functions/triggers.

3. **Run API**

   ```bash
   dotnet run
   ```
   
   Open Swagger UI at `https://localhost:{port}/` (app serves Swagger at root).

## Database

**Schema:** Games, Matches, MatchDataPoints with cascade deletes.

**Triggers:** Four PL/pgSQL triggers auto-update counters:
- `Games.MatchesCount` → increments/decrements on Match insert/delete
- `Matches.PlayerCount` → updates (distinct player count) on MatchDataPoint insert/delete

**Trigger Files:**
- Function/trigger definitions: [Api/Migrations/20261014211106_AddMatchCountTriggers.cs](Api/Migrations/20261014211106_AddMatchCountTriggers.cs)
- Initialization: [Api/Extensions/DatabaseSetup.cs](Api/Extensions/DatabaseSetup.cs) `InitializeDatabase()` (applies migrations)
- Schema: [Api/Migrations/20260701204629_InitialCreate.cs](Api/Migrations/20260701204629_InitialCreate.cs)

**Migration Workflow:** `Program.cs` calls `Migrate()` on startup; the trigger functions/triggers are created by the `AddMatchCountTriggers` migration (no separate startup DDL).

## API Summary
- Entry point: [Api/Program.cs](Api/Program.cs)
- Endpoints grouped in [Api/Endpoints](Api/Endpoints)
  - Games: [Api/Endpoints/Game/MapGameEndpoints.cs](Api/Endpoints/Game/MapGameEndpoints.cs)
  - Matches: [Api/Endpoints/Match/MapMatchEndpoints.cs](Api/Endpoints/Match/MapMatchEndpoints.cs)
  - MatchDataPoints: [Api/Endpoints/MatchDataPoint/MapMatchDataPointEndpoints.cs](Api/Endpoints/MatchDataPoint/MapMatchDataPointEndpoints.cs)

### Key Endpoints
- `GET /games`, `GET /game/{id}`
- `POST /game`, `POST /games`
- `GET /matches`, `GET /match/{id}` (optional `includeDataPoints`)
- `POST /match`, `PUT /match/{id}`
- `GET /match-data-points/all`, `POST /match-data-point/{MatchId}`
- `GET /health` — readiness check (200 Healthy / 503 Unhealthy; reports database availability)

See code for full list and request/response shapes.

## Tests

Hybrid test strategy: unit tests are fast and isolated; integration tests validate real PostgreSQL behavior including trigger execution.

### Unit Tests (no Docker required)
Test service logic in isolation using EF Core InMemory provider.
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests|DatabaseConnectionInfoTests|DatabaseInitializationStateTests"
```
Covers `GameServiceTests` (15), `MatchServiceTests` (11), `MatchDataPointServiceTests` (15), and the connection/state unit tests (7) — **48 tests**. See [Api.Tests/TESTING.md](Api.Tests/TESTING.md) for the full breakdown.

### Integration Tests (requires Docker)
Test actual API endpoints against a real PostgreSQL instance. Validates trigger behavior (`Games.MatchesCount`, `Matches.PlayerCount` auto-update).

1. Start the test database:
   ```bash
   docker-compose -f docker-compose.test.yml up -d
   ```
   Starts PostgreSQL 16 on `localhost:5433` (`gamescoringapi_test` / `testuser` / `testpassword`).

2. Run integration tests:
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj
   ```

3. Stop when done:
   ```bash
   docker-compose -f docker-compose.test.yml down
   ```

Each test gets its own isolated database (unique name per `TestBase` instance) — no shared state between tests.

### CI/CD
GitHub Actions runs the full test suite on every pull request ([`.github/workflows/run-api-tests.yml`](.github/workflows/run-api-tests.yml)):
- Spins up PostgreSQL 16 as a service container
- Runs unit + integration tests against it
- No local Docker required in CI

## Notes
- **Triggers & Postgres Only**: Triggers are defined in the `AddMatchCountTriggers` migration and applied through `Migrate()`. Unit tests use EF Core InMemory and skip trigger behavior — use integration tests to validate trigger logic.
- **Migration Workflow**: Schema and triggers are created via `dotnet ef database update` / `Database.Migrate()`; no separate startup DDL runs.
- **Connection Priority**: `ConnectionStrings:DatabaseUrl` (env var `ConnectionStrings__DatabaseUrl`) takes precedence over `ConnectionStrings:DefaultConnection`.
- **Startup Logging**: the resolved database target (source/host/port/database/user — never the password) and migration lifecycle are logged via `ILogger` category `GameScoringAPI.Database`. A `DefaultConnection` fallback outside Development logs a warning.
- **Degraded Mode**: if migrations fail at startup, the app logs `Critical`, records the failure, and keeps running so `GET /health` returns `503` instead of a bare `HTTP 500.30`. Set `Database:FailFastOnStartup` to `true` to crash on startup instead.
- **Gitignored config:** `appsettings.json` / `appsettings.Development.json` are not tracked. Create them locally (or set the `ConnectionStrings__DatabaseUrl` env var); in CI the deploy workflow generates `appsettings.Production.json` from the `DATABASE_URL` repository secret.
- **PutMatch Security:** Endpoint prevents changing `GameId` when match has data points to avoid data leakage.
- **CORS Policy**: Allows `http://localhost:3000` and one Azure static app; adjust in [Api/Program.cs](Api/Program.cs) if needed.

## Development

### Add New Migration
```bash
cd Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Regenerate Triggers
The trigger functions/triggers are defined in the `AddMatchCountTriggers` migration
(`CREATE OR REPLACE FUNCTION` + `DROP TRIGGER IF EXISTS`/`CREATE TRIGGER`), so a
normal migration update (or app startup) is idempotent. To force a clean re-apply:
```bash
cd Api
dotnet ef database update 0        # roll back (drops the triggers/functions)
dotnet ef database update          # re-apply (recreates them)
```

## Deployment

1. Ensure the Postgres database exists and is accessible.
2. Provide the connection string (see **Configuration** below).
3. Run the app — migrations (which include the triggers) initialize automatically:
   ```bash
   dotnet run
   ```

### Configuration

`appsettings.json` and `appsettings.Development.json` are **gitignored** — they are
not in the repo and are never deployed. At startup the app resolves the connection
from `ConnectionStrings:DatabaseUrl` first, then `ConnectionStrings:DefaultConnection`.

| Environment | Where the settings come from |
| --- | --- |
| Local dev | a local `appsettings.Development.json`, or the `ConnectionStrings__DatabaseUrl` env var |
| Azure (production) | `appsettings.Production.json`, generated by the deploy workflow (below) |

`web.config` forces `ASPNETCORE_ENVIRONMENT=Production`, so `appsettings.Development.json` is **not** loaded in Azure.

### Azure App Service (production)

Deploys run through [`.github/workflows/master_gamescoringapi.yml`](.github/workflows/master_gamescoringapi.yml) on every push to `master`. The **build job generates `appsettings.Production.json`** containing the non-secret settings (`Logging`, `AllowedHosts`, `Database:FailFastOnStartup`) plus the connection string, and publishes it with the app.

The connection string comes from the **repository secret `DATABASE_URL`**:

1. Go to **Settings → Secrets and variables → Actions → New repository secret**.
2. Name: `DATABASE_URL`.
3. Value: the full `postgresql://<user>:<pass>@<host>/<db>?sslmode=require` URI.

It must be a **repository** secret (not an environment secret): the build job that
reads it does not declare an `environment:`, so it can only see repository/org
secrets. If the secret is missing the generated `DatabaseUrl` is empty and the app
crashes on startup with `HTTP 500.30`.

> **Alternative — keep the secret out of the artifact.** Instead of generating the
> file, set the connection as an Azure **App Setting** and remove the generation
> step from the workflow:
>
> ```
> Name:  ConnectionStrings__DatabaseUrl
> Value: postgresql://<user>:<pass>@<host>/<db>?sslmode=require
> ```
>
> Use an App Setting, not a portal "Connection string" — the latter maps to a
> `POSTGRESQLCONNSTR_` env var the app does not read. The secret then stays out of
> the published package and can be rotated without a redeploy.

Verify after deploy:
- Log stream / `D:\home\LogFiles\stdout\stdout_*.log` shows `Database connection resolved. Source=DatabaseUrl, Host=...` (never the password).
- `GET https://<app>.azurewebsites.net/health` returns `200 Healthy`.
- If it returns `503`, the response body and the `Critical`/`Warning` startup logs state why.

## Next Steps
- Monitor trigger performance on high-insert scenarios.
- Add database backup strategy.

## License
MIT-style. Check repository owner for license details.
