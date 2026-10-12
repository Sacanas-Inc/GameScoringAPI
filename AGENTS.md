# AGENTS.md

Guidance for AI coding agents (and humans) working in this repository.

## What this is

Minimal **.NET 10** game-scoring API. It stores Games, Matches, and Match data
points, and computes simple stats (totals, winning player). Data lives in
**PostgreSQL** through EF Core; PL/pgSQL triggers maintain denormalized counters
(`Games.MatchesCount`, `Matches.PlayerCount`) automatically.

- **API project:** `Api/GameScoringAPI.csproj` (`net10.0`, minimal APIs)
- **Tests:** `Api.Tests/Api.Tests.csproj` (xUnit)
- **Solution:** `GameScoringAPI.sln` (repo root) — includes both projects.
  There is also a legacy `Api/Api.sln`; prefer the root solution.
- The project folder is `Api` (capital A) on disk. Shell commands on
  case-insensitive Windows work either way, but keep the on-disk casing in
  paths you commit.

Read `README.md` first for setup, and `Api.Tests/TESTING.md` for the test
strategy.
## Build, run, test

Run commands from the **repository root** unless noted.

```bash
# Build everything
dotnet build GameScoringAPI.sln

# Run the API locally (default https profile -> https://localhost:7191)
dotnet run --project Api

# Unit tests only (fast, no Docker)
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests|DatabaseConnectionInfoTests|DatabaseInitializationStateTests"

# All tests (integration tests need the Docker Postgres below)
dotnet test Api.Tests/Api.Tests.csproj
```

### Integration-test database (Docker required)

Integration tests use a real PostgreSQL 16 on **localhost:5433** (distinct from
the default dev port 5432).

```bash
docker-compose -f docker-compose.test.yml up -d
dotnet test Api.Tests/Api.Tests.csproj
docker-compose -f docker-compose.test.yml down
```

`Api.Tests/Tests/TestBase.cs` spins up the app with `WebApplicationFactory`,
creates a **unique database per test** (`gamescoringapi_test_<guid8>`), runs
migrations, and seeds from `Api.Tests/Data/GamesData.json`. This is why running
the full suite locally requires Docker.

### Warnings are errors

`Directory.Build.props` sets `TreatWarningsAsErrors=true`. A new compiler or
analyzer warning **fails the build** — fix it rather than suppressing it.
NuGet audit warnings `NU1901`–`NU1904` are intentionally excluded so advisories
stay visible without breaking the build. `dotnet build` is expected to produce
**0 warnings**; if you add one, that's a regression.

## Repository layout

```
GameScoringAPI.sln            Root solution (use this one)
Directory.Build.props         TreatWarningsAsErrors + NU audit exclusions
docker-compose.test.yml       Postgres 16 for integration tests (port 5433)
Api/
  Program.cs                  Composition root: DI, CORS, Swagger, endpoint mapping, startup DB init
  Context/GameDBContext.cs    EF Core DbContext (Games, Matches, MatchDataPoints)
  Models/                     Entities AND their DTOs (Game.cs, Match.cs, MatchDataPoint.cs)
  Endpoints/
    Game/       Match/        One folder per resource; Map*Endpoints + Get/Post/Put/Delete files
    MatchDataPoint/           Also holds request DTOs (MatchDataPointRequests.cs)
    HealthEndpoints.cs        GET /health (200 Healthy / 503 Unhealthy)
    TestEndpoints.cs          GET /testok (no DB access)
  Services/
    I*Service.cs, *Service.cs Business logic (async, uses DbContext + validators)
    Validators/               I*Validator + *Validator (field + business rules)
    Exceptions/               ServiceException, ValidationException, NotFoundException
  Mapper/MatchMapper.cs       Match -> DTO mapping, stats, winner calculation
  Extensions/                 DatabaseSetup (connection + init), DatabaseConnectionInfo, DatabaseInitializationState
  Sql/Triggers/SqlTriggers.cs PL/pgSQL functions/triggers as string constants
  Migrations/                 EF Core migrations
  Validations/GameValidation.cs  DEAD CODE (see ROADMAP) — do not extend
Api.Tests/
  Services/                   Unit tests (EF Core InMemory) — GameService/MatchService/MatchDataPointService
  Tests/                      Integration tests (real Postgres) — Games/Matches/MatchDataPoints + TestBase
  Extensions/                 DatabaseConnectionInfoTests (pure unit)
  Data/GamesData.json         Seed data copied to test output
```

## Architecture & conventions

### Request flow

`Program.cs` maps endpoints → endpoint file injects an `I*Service` → service
validates via an `I*Validator` and talks to `GameDBContext` → service throws
`NotFoundException` / `ValidationException` / `ServiceException` → the endpoint
catches and maps to an `IResult`.

- **Endpoints must stay thin.** Catch service exceptions and return results;
  keep business logic in services. (Exception: the legacy
  `GET /games-with-matches-and-data-points` in `Api/Endpoints/Game/GetGameEndpoints.cs`
  still injects `GameDBContext` directly — see ROADMAP; don't copy that pattern.)
- **Register new services/validators in `Program.cs`** (`builder.Services.AddScoped<...>`).
  Every resource has a `Map<Entity>Endpoints(this WebApplication app)` extension
  that fans out to the per-verb files; call it from `Program.cs`.
- **Async everywhere**: service methods are `...Async` and awaited. Use EF Core
  async APIs (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`).
- **Entities and DTOs are plain classes**, not records (except
  `DatabaseConnectionInfo`). Nullable reference types are enabled
  (`<Nullable>enable</Nullable>`); follow the existing `= null!;` / `= new();`
  initializer style for entities and DTOs.
- **Namespaces**: `Endpoints/`, `Models/`, `Context/`, `Mapper/`, and the
  validations class live in the **global namespace**. `Services/*`,
  `Services/Validators/*`, `Services/Exceptions/*`, and `Extensions/*` use
  `namespace GameScoringAPI.*`. Match the file you're editing.

### Error response shape (contract)

Error bodies are JSON objects (this was a breaking change — see `BREAKING_CHANGES.md`):

- Validation: `400` with `{ "errors": [ "...", "..." ] }`
- Not found: `404` with `{ "error": "..." }`
- Other service errors: `400` with `{ "error": "..." }`
- **Exception:** `PUT /match-data-point/{id}` returns `409 Conflict` (not 400)
  for validation errors. Preserve this unless intentionally changing the API.
- Unhandled/DB failures in `GetMatchDataPointEndpoints.cs` return a generic `500`.

### Swagger tags

Tags use numeric prefixes to control ordering, plus a verb tag:

```csharp
.WithTags("1. Games", "GET Endpoints", "9. FrontEnd - Mockup")
```

Prefixes: `0. Full Dataset`, `1. Games`, `2. Matches`, `3. MatchDataPoints`.
`9. FrontEnd - Mockup` marks endpoints used by the sample frontend. Add matching
tags when you add endpoints; `SwaggerTagsSorter.cs` sorts tags alphabetically.
Swagger UI is served at the app root (`/`), config in `Program.cs`.

## Database

### Connection resolution (in `Api/Extensions/DatabaseSetup.cs`)

`ConnectionStrings:DatabaseUrl` takes precedence, then
`ConnectionStrings:DefaultConnection`. The env-var form is
`ConnectionStrings__DatabaseUrl` (a `postgresql://user:pass@host:port/db` URI,
rewritten to Npgsql with `SslMode=Require`). If neither is set the app throws at
startup.

- `appsettings.json` / `appsettings.Development.json` / `appsettings.Production.json`
  are **gitignored** (a local `Api/appsettings.json` may exist but is untracked).
  In CI/Azure the deploy workflow generates `appsettings.Production.json` from
  the `DATABASE_URL` repository secret.
- **Never log or commit credentials.** `DatabaseConnectionInfo` is deliberately
  password-free; log through it, not the raw connection string.

### Startup initialization & degraded mode

`Program.cs` calls `app.InitializeDatabase()`, which runs `Database.Migrate()`
and then creates the PL/pgSQL functions/triggers via `ExecuteSqlRaw`. Triggers
are created **only when `Database.IsRelational()`** — the unit tests' InMemory
provider skips them, so **trigger behavior can only be validated by integration
tests**.

If migrations fail, the app logs `Critical` and (unless
`Database:FailFastOnStartup=true`) keeps running in **degraded mode** so
`GET /health` returns `503` instead of a bare `HTTP 500.30`.

### Migrations

```bash
cd Api
dotnet ef migrations add <Name>
dotnet ef database update
```

Do **not** hand-edit `Migrations/*.Designer.cs` or the model snapshot. Triggers
are currently created at startup (not in migrations); moving them into
migrations is a known P3 item in `ROADMAP.md`.

## Testing conventions

- **Unit tests** (`Api.Tests/Services/`) use EF Core InMemory, mock validators
  where useful (Moq), and run fast without Docker. Assert service logic only.
- **Integration tests** (`Api.Tests/Tests/`) extend `TestBase`, hit real
  endpoints through `HttpClient`, and validate trigger side effects. They need
  the Docker DB on port 5433.
- Test data lives in `Api.Tests/Data/*.json`, loaded via `TestBase.GetDto<T>` and
  copied to the output directory.
- Totals: **48 unit + 58 integration = 106**. `TESTING.md` has the per-class
  breakdown and run commands; keep it in sync when you add tests.

## CI/CD

- `.github/workflows/run-api-tests.yml` — on every PR: restores, builds in
  Release, spins up a Postgres 16 service on port 5433, runs the full suite with
  `ConnectionStrings__DatabaseUrl` set.
- `.github/workflows/master_gamescoringapi.yml` — on push to `master` (or manual):
  builds, generates `appsettings.Production.json` from the `DATABASE_URL`
  secret, publishes, and deploys to the Azure Web App `GameScoringAPI`.
  `web.config` forces `ASPNETCORE_ENVIRONMENT=Production`.

## Documentation notes

- `README.md` — setup, endpoints, deployment. Authoritative for config.
- `Api.Tests/TESTING.md` — test strategy, per-class counts, and commands.
- `BREAKING_CHANGES.md` — API contract changes vs `master`; read before
  changing response shapes or status codes.
- `ROADMAP.md` — outstanding work (P1–P3). Useful TODO source.

## Keeping the README and tests in sync

A change is not done until the documentation and tests that describe it are
updated. Treat stale docs or failing/absent tests as part of the change, not a
follow-up task.

### Update the README with every behavior change

`README.md` is authoritative for setup, configuration, endpoints, and
deployment. Whenever you:

- add, rename, or remove an endpoint → update the endpoint list (method, path,
  request/response shape, and status codes).
- change config keys, environment variables, ports, or connection resolution →
  update the setup/config sections.
- add a migration, dependency, or startup step → update the setup/run
  instructions.

Match the README's existing structure and tone, and don't duplicate details that
belong in `BREAKING_CHANGES.md` or `ROADMAP.md`.

### Update the tests with every behavior change

- Add or extend tests for any new or changed behavior. Service logic →
  unit tests in `Api.Tests/Services/` (InMemory); endpoints, mapping, triggers,
  or schema → integration tests in `Api.Tests/Tests/` (real Postgres).
- Bug fix → add a regression test that fails before the fix and passes after.
- Never delete, skip, or weaken a test to get the suite green — fix the code or
  correct the test's expectations.
- Prefer unit tests when the logic can be asserted without the DB; use
  integration tests when trigger side effects or real Postgres behavior matter.
- When you add or remove tests, update the counts and per-class breakdown in
  `Api.Tests/TESTING.md` (currently **48 unit + 58 integration = 106**) and the
  totals in this file.

## Known tech debt (don't accidentally make it worse)

- `Api/Validations/GameValidation.cs` is unreferenced dead code (superseded by
  `Services/Validators/GameValidator.cs`).
- `GET /games-with-matches-and-data-points` bypasses the service layer and
  repeats mapping/winner logic inline.
- Triggers are created at app startup rather than in migrations.
- Some duplicated DTO/mapping code (see `TODO`s in `GetGameEndpoints.cs`) and
  the misspelled `isFinished` property name (kept for API compatibility).

## Quick checklist before committing

1. `dotnet build GameScoringAPI.sln` → 0 warnings, 0 errors.
2. `dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests|DatabaseConnectionInfoTests|DatabaseInitializationStateTests"` passes (no Docker).
3. Integration tests pass if you touched endpoints, services, mapping, triggers,
   or the DB schema (start the Docker DB first).
4. Error responses follow the shapes above; new endpoints carry correct
   `WithName`/`WithTags`.
5. `README.md` updated for any endpoint, config, or setup change.
6. Tests added/updated for any behavior change, and `TESTING.md` counts (plus
   the totals here) kept in sync.
7. No secrets or `appsettings*.json` added to git.
