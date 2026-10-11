# Testing Guide

How to run and understand the tests for GameScoringAPI.

## Overview

The suite has **106 tests** split into two categories:

| Category | Count | Needs Docker? | Location |
| --- | ---: | :---: | --- |
| **Unit** — service/business logic on EF Core InMemory | 48 | No | `Api.Tests/Services/`, `Api.Tests/Extensions/` |
| **Integration** — real endpoints against PostgreSQL | 58 | Yes | `Api.Tests/Tests/` |

Unit tests are fast and isolated (each gets its own in-memory database).
Integration tests validate real HTTP behavior **including the PL/pgSQL trigger
side effects** that InMemory cannot exercise (e.g. `Games.MatchesCount`,
`Matches.PlayerCount`).

Run everything from the repository root.

## Unit tests (no Docker)

Unit tests use EF Core's InMemory provider and run in about a second.

### Run all unit tests

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests|DatabaseConnectionInfoTests|DatabaseInitializationStateTests"
```

> The three terms are needed because they live in different classes. The older,
> shorter `--filter "ServiceTests"` runs only the three `*ServiceTests` classes
> (41 tests) and misses the `Extensions` unit tests.

### Run a specific class

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchDataPointServiceTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "DatabaseConnectionInfoTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "DatabaseInitializationStateTests"
```

### Run a specific method

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.GetGameByIdAsync_WithExistingId_ReturnsGame"
```

### Verbose output

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests" -v detailed
```

**Characteristics:** fast (< 1s), no external dependencies, unique in-memory
database per test, mocks validators where useful (Moq). They assert service
logic only — **not** trigger behavior.

### Unit test coverage

**`Services/GameServiceTests.cs` (15)**
- CRUD: create, read (one/all), update, delete
- Empty/all-list retrieval
- Validation: empty name, duplicate name, negative `MinPlayers`, `MinPlayers > MaxPlayers`
- Update with duplicate name / same name
- `NotFoundException` on get, update, and delete of a missing id

**`Services/MatchServiceTests.cs` (11)**
- CRUD: create, read (one/all), update, delete
- `GetMatchWithWinnerAsync` winner calculation (via `MatchMapper`)
- Validation: invalid `GameId`; changing `GameId` when data points exist
- `NotFoundException` on missing ids

**`Services/MatchDataPointServiceTests.cs` (15)**
- Retrieval: all, detailed (with match/game info), by id, by match id
- Create; partial update; delete
- Bulk delete all data points for a match (with and without data)
- Validation and `NotFoundException` handling

**`Extensions/DatabaseConnectionInfoTests.cs` (4)**
- Password is never exposed; non-secret parts map correctly
- Resolution warning is carried; `ToString()` is safe to log

**`Extensions/DatabaseConnectionInfoTests.cs` → `DatabaseInitializationStateTests` (3)**
- New state is not completed; `MarkSucceeded` clears the error; `MarkFailed` records it

## Integration tests (Docker required)

Integration tests exercise the real HTTP pipeline against a real PostgreSQL
instance and validate database triggers.

### Prerequisites

1. **Docker Desktop must be running.**
2. **Start the test database:**

```bash
docker-compose -f docker-compose.test.yml up -d
```

This starts PostgreSQL 16 on `localhost:5433`:

- Container: `gamescoringapi-test-db`
- Database: `gamescoringapi_test`
- User: `testuser`
- Password: `testpassword`

3. **Verify it is up:**

```bash
docker ps | findstr gamescoringapi-test-db
```

### Run all tests (unit + integration)

```bash
dotnet test Api.Tests/Api.Tests.csproj
```

### Run only the integration suites

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests|GamePostTests|GameDeleteTests|MatchGetTests|MatchPostTests|MatchPutTests|MatchDeleteTests|MatchDataPointGetTests|MatchDataPointPostTests|MatchDataPointPutTests|MatchDataPointDeleteTests"
```

Or a single suite:

```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchGetTests"
```

### Stop the database

```bash
docker-compose -f docker-compose.test.yml down
```

Reset it if the schema/data gets stuck:

```bash
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

**Characteristics:** slower (seconds per test), production-like PostgreSQL,
validates trigger behavior. `TestBase` creates a **unique database per test**
(`gamescoringapi_test_<8-char-guid>`), applies migrations, and seeds
`Data/GamesData.json` — so tests never share state.

### Integration test coverage

**`Tests/Games/` (16)**
- `GameGetTests` (7): get by id, not found, filter by id, filter by description, response headers
- `GamePostTests` (8): single/multiple create, created status, correct data, invalid-data `400`, response headers
- `GameDeleteTests` (1): deleting removes the entity

**`Tests/Matches/` (18)**
- `MatchGetTests` (6): all, filter by `gameId`, by id, `includeDataPoints`, not found, response headers
- `MatchPostTests` (4): create valid, invalid `GameId` `400`, no notes, trigger updates `MatchesCount`
- `MatchPutTests` (4): update valid, change `GameId` `400`, non-existing `404`, clear notes
- `MatchDeleteTests` (4): delete valid, non-existing `404`, cascades data points, trigger updates `MatchesCount`

**`Tests/MatchDataPoints/` (24)**
- `MatchDataPointGetTests` (6): all, detailed, by id, by match id, not found, response headers
- `MatchDataPointPostTests` (5): valid `201`, invalid `MatchId` `400`, trigger updates `PlayerCount`, negative points allowed, missing player name `400`
- `MatchDataPointPutTests` (5): valid `204`, partial update, non-existing `404`, change player name, change `MatchId` with existing data `400`
- `MatchDataPointDeleteTests` (8): delete valid, non-existing `404`, trigger updates `PlayerCount`, deleting several from one match, bulk delete all-valid `204`, mixed valid/invalid multi-status, all-invalid multi-status, empty `400`

## Test structure

```
Api.Tests/
├── Services/                          # Unit tests — no Docker
│   ├── GameServiceTests.cs                (15)
│   ├── MatchServiceTests.cs               (11)
│   └── MatchDataPointServiceTests.cs      (15)
├── Extensions/                        # Unit tests — no Docker
│   └── DatabaseConnectionInfoTests.cs     (7: 4 + DatabaseInitializationStateTests 3)
├── Tests/                             # Integration tests — Docker required
│   ├── TestBase.cs                        # unique DB per test, migrations, seeding
│   ├── Games/
│   │   ├── GameGetTests.cs                (7)
│   │   ├── GamePostTests.cs               (8)
│   │   └── GameDeleteTests.cs             (1)
│   ├── Matches/
│   │   ├── MatchGetTests.cs               (6)
│   │   ├── MatchPostTests.cs              (4)
│   │   ├── MatchPutTests.cs               (4)
│   │   └── MatchDeleteTests.cs            (4)
│   └── MatchDataPoints/
│       ├── MatchDataPointGetTests.cs      (6)
│       ├── MatchDataPointPostTests.cs     (5)
│       ├── MatchDataPointPutTests.cs      (5)
│       └── MatchDataPointDeleteTests.cs   (8)
└── Data/
    └── GamesData.json                     # seed data, copied to test output
```

## CI/CD

In GitHub Actions the full suite runs on every pull request
([`.github/workflows/run-api-tests.yml`](../.github/workflows/run-api-tests.yml)):

1. Spins up a PostgreSQL 16 service container on port `5433`.
2. Restores and builds in Release.
3. Runs all tests with `ConnectionStrings__DatabaseUrl` pointing at the service.

No local Docker is required in CI — GitHub provides the database service.

## Command reference

```bash
# All tests (requires Docker)
dotnet test Api.Tests/Api.Tests.csproj

# Unit only (no Docker) — 48 tests
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests|DatabaseConnectionInfoTests|DatabaseInitializationStateTests"

# Integration only (requires Docker) — 58 tests
dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests|GamePostTests|GameDeleteTests|MatchGetTests|MatchPostTests|MatchPutTests|MatchDeleteTests|MatchDataPointGetTests|MatchDataPointPostTests|MatchDataPointPutTests|MatchDataPointDeleteTests"

# Single class
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchGetTests"

# Single method
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchGetTests.GetMatchById_WithExistingId_ReturnsMatch"

# Detailed output
dotnet test Api.Tests/Api.Tests.csproj -v detailed
```

## Troubleshooting

### Docker container won't start

```bash
# Is the daemon running?
docker ps

# Port conflict on 5433?
netstat -ano | findstr :5433

# Force a clean rebuild
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

### Tests fail with "Cannot connect to database"

- Confirm `docker-compose -f docker-compose.test.yml up -d` succeeded.
- Check logs: `docker logs gamescoringapi-test-db`.
- Confirm the container is running: `docker ps`.

### Tests pass locally but fail in CI

- Verify the workflow passes `ConnectionStrings__DatabaseUrl` correctly.
- Verify the .NET version matches (`net10.0`).

### JSON deserialization errors in tests

`JsonSerializationException` usually means the test deserialized an error body
as a success DTO. Check `response.StatusCode` **before** deserializing, and use
the documented error shapes (`{ "error": ... }` / `{ "errors": [...] }`).

### Unit vs integration confusion

- Unit tests: fast, isolated, no Docker, **no trigger coverage**.
- Integration tests: Docker, real PostgreSQL, **only place triggers are validated**.
- Run unit first for fast feedback, then integration for full validation.
