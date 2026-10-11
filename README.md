# GameScoringAPI

Minimal game scoring API. Store games, matches, match data points, compute simple stats (winning player, totals).

## Features
- CRUD for Games, Matches, Match Data Points
- Compute match stats and winning player
- Postgres DB with PL/pgSQL triggers auto-updating match/player counts
- Swagger UI for API exploration
- EF Core migrations with trigger initialization

## Tech Stack
- .NET 10 minimal APIs
- Entity Framework Core 10.0.0 (Postgres Npgsql provider)
- Postgres 12+ with PL/pgSQL functions and triggers
- xUnit tests — unit (EF Core InMemory) + integration (Docker PostgreSQL)

## Quick Start

### Prerequisites
- .NET 10 SDK
- Postgres 12+ (local or remote)

### Setup

1. **Configure Database Connection**

   Set connection string via `appsettings.json` (DefaultConnection) or `DATABASE_URL` env var:
   ```bash
   export DATABASE_URL="postgresql://user:pass@localhost:5432/gamescoringdb"
   ```
   
   Or edit [Api/appsettings.json](Api/appsettings.json):
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
   
   This creates schema and initializes PL/pgSQL triggers.

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
- Function definitions: [Api/Sql/Triggers/SqlTriggers.cs](Api/Sql/Triggers/SqlTriggers.cs)
- Initialization: [Api/Extensions/DatabaseSetup.cs](Api/Extensions/DatabaseSetup.cs) `InitializeDatabase()`
- Schema: [Api/Migrations/20260701204629_InitialCreate.cs](Api/Migrations/20260701204629_InitialCreate.cs)

**Migration Workflow:** `Program.cs` calls `Migrate()` on startup, then creates triggers via `ExecuteSqlRaw`.

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
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests"
```
Covers: `GameServiceTests` (15), `MatchServiceTests` (10), `MatchDataPointServiceTests` (16) — **41 tests**.

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
- **Triggers & Postgres Only**: Triggers execute via `ExecuteSqlRaw` only when `Database.IsRelational()` is true. Unit tests use EF Core InMemory and skip trigger behavior — use integration tests to validate trigger logic.
- **Migration Workflow**: Schema created via `dotnet ef database update`, triggers initialized on app startup in `InitializeDatabase()`.
- **Connection Priority**: `DATABASE_URL` env var takes precedence over `appsettings.json` DefaultConnection.
- **Startup Logging**: the resolved database target (source/host/port/database/user — never the password), migration lifecycle, and trigger creation are logged via `ILogger` category `GameScoringAPI.Database`. A `DefaultConnection` fallback outside Development logs a warning.
- **Degraded Mode**: if migrations fail at startup, the app logs `Critical`, records the failure, and keeps running so `GET /health` returns `503` instead of a bare `HTTP 500.30`. Set `Database:FailFastOnStartup` to `true` to crash on startup instead.
- **PutMatch Security**: Endpoint prevents changing `GameId` when match has data points to avoid data leakage.
- **CORS Policy**: Allows `http://localhost:3000` and one Azure static app; adjust in [Api/Program.cs](Api/Program.cs) if needed.

## Development

### Add New Migration
```bash
cd Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Regenerate Triggers
Triggers re-created on app startup if they exist (DROP + CREATE). Manual re-trigger:
```bash
cd Api
dotnet run  # InitializeDatabase() re-creates triggers
```

## Deployment

1. Ensure Postgres database exists and is accessible.
2. Set `DATABASE_URL` or configure `appsettings.json` connection string.
3. Run app (migrations + triggers initialize automatically):
   ```bash
   dotnet run
   ```

### Azure App Service (production)

`web.config` forces `ASPNETCORE_ENVIRONMENT=Production`, so `appsettings.Development.json` is **not** loaded. Configure the connection as an **App Setting** (not a portal "Connection string", which maps to a `POSTGRESQLCONNSTR_` env var the app does not read):

```
Name:  ConnectionStrings__DatabaseUrl
Value: postgresql://<user>:<pass>@<host>/<db>?sslmode=require
```

Verify after deploy:
- Log stream / `D:\home\LogFiles\stdout\stdout_*.log` shows `Database connection resolved. Source=DatabaseUrl, Host=...` (never the password).
- `GET https://<app>.azurewebsites.net/health` returns `200 Healthy`.
- If it returns `503`, the response body and the `Critical`/`Warning` startup logs state why.

## Next Steps
- Monitor trigger performance on high-insert scenarios.
- Add database backup strategy.

## License
MIT-style. Check repository owner for license details.
