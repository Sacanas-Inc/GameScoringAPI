# Testing Guide

This document explains how to run tests for the GameScoringAPI project.

## Overview

The test suite is split into two categories:

- **Unit Tests (Service Layer):** Fast, isolated tests using in-memory database. Test service logic without database dependencies.
- **Integration Tests (Endpoint Layer):** Real PostgreSQL database via Docker. Validate actual API behavior including database triggers.

## Unit Tests (Service Layer)

Unit tests use EF Core's InMemory provider for fast, isolated testing of service business logic.

### Run All Unit Tests
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests"
```

### Run Specific Service Tests
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchDataPointServiceTests"
```

### Run with Verbose Output
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests" -v detailed
```

**Characteristics:**
- Fast execution (< 1 second)
- No external dependencies
- Each test gets a unique in-memory database
- Tests service logic isolation, not trigger behavior

### Service Test Coverage

**GameServiceTests (15 tests)**
- CRUD operations: Create, Read, Update, Delete
- Duplicate game name validation
- Error handling: NotFoundException, ValidationException
- Game retrieval by ID
- All games retrieval

**MatchServiceTests (10 tests)**
- CRUD operations: Create, Read, Update, Delete
- GameId validation
- Match with winner retrieval (using MatchMapper)
- Prevent GameId change when match has data points
- Error handling and edge cases

**MatchDataPointServiceTests (16 tests)**
- CRUD operations: Create, Read, Update, Delete
- Bulk operations: DeleteAllDataPointsForMatchAsync
- Detailed and simple data point retrieval
- Data points by match ID retrieval
- Validation: MatchId must exist, cannot change MatchId on update
- Error handling: NotFoundException, ValidationException
- Empty set handling (no data points found)

## Integration Tests (Endpoint Layer)

Integration tests require real PostgreSQL running in Docker. These tests validate actual API behavior including database trigger functionality.

### Prerequisites

1. **Docker must be installed** on your machine
2. **Start the test database container:**

```bash
docker-compose -f docker-compose.test.yml up -d
```

This starts a PostgreSQL 16 container on `localhost:5433` with:
- Database: `gamescoringapi_test`
- User: `testuser`
- Password: `testpassword`

3. **Verify the container is running:**

```bash
docker ps | findstr gamescoringapi-test-db
```

### Run All Integration Tests
```bash
dotnet test Api.Tests/Api.Tests.csproj
```

### Run Specific Endpoint Tests
```bash
dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests"
dotnet test Api.Tests/Api.Tests.csproj --filter "GameDeleteTests"
```

### Run with Verbose Output
```bash
dotnet test Api.Tests/Api.Tests.csproj -v detailed
```

### Stop the Test Database
```bash
docker-compose -f docker-compose.test.yml down
```

### Clean Up Database and Restart
```bash
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

**Characteristics:**
- Slower execution (~5-10 seconds per test)
- Tests against real PostgreSQL (production-like)
- Validates trigger behavior (Games.MatchesCount auto-update, etc.)
- Each test gets a clean database (migrated and seeded)

## Test Structure

```
Api.Tests/
├── Services/
│   ├── GameServiceTests.cs              # Unit tests for GameService (15 tests)
│   ├── MatchServiceTests.cs             # Unit tests for MatchService (10 tests)
│   └── MatchDataPointServiceTests.cs    # Unit tests for MatchDataPointService (16 tests)
├── Tests/
│   ├── TestBase.cs                      # Base class with Docker Postgres setup
│   └── Games/
│       ├── GameGetTests.cs              # Integration tests for GET endpoints
│       ├── GamePostTests.cs             # Integration tests for POST endpoints
│       └── GameDeleteTests.cs           # Integration tests for DELETE endpoints
└── Data/
    └── GamesData.json                   # Test data for seeding
```

## CI/CD Integration

When tests run in GitHub Actions:
1. CI workflow starts PostgreSQL service container
2. Runs full test suite (unit + integration)
3. Reports results

See `.github/workflows/` for CI configuration.

---

## Test Commands Reference

### Quick Test Runs
```bash
# Run all tests
dotnet test Api.Tests/Api.Tests.csproj

# Run only unit tests (no Docker needed)
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests"

# Run only integration tests (requires Docker)
dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests|GamePostTests|GameDeleteTests|MatchGetTests|MatchPostTests|MatchDeleteTests|MatchDataPointGetTests|MatchDataPointPostTests|MatchDataPointDeleteTests|MatchDataPointPutTests|MatchPutTests"

# Run with detailed output
dotnet test Api.Tests/Api.Tests.csproj -v detailed

# Run specific test class
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchGetTests"

# Run specific test method
dotnet test Api.Tests/Api.Tests.csproj --filter "MatchGetTests.GetMatchById_WithExistingId_ReturnsMatch"
```

## Troubleshooting

### Docker Container Won't Start
```bash
# Check Docker daemon is running
docker ps

# Check for port conflicts (5433)
netstat -ano | findstr :5433

# Force recreation
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

### Tests Fail with "Cannot connect to database"
- Ensure `docker-compose -f docker-compose.test.yml up -d` ran successfully
- Check container logs: `docker logs gamescoringapi-test-db`
- Verify PostgreSQL started: `docker ps`

### Tests Pass Locally but Fail in CI
- Ensure CI workflow passes PostgreSQL connection string correctly
- Check environment variables in CI workflow
- Verify .NET version matches (net10.0)

### Unit Tests vs Integration Tests Confusion
- Unit tests: Fast, isolated, no Docker needed
- Integration tests: Requires Docker, tests actual behavior including triggers
- Run unit tests first for quick feedback, integration tests for full validation

### JSON Deserialization Errors in Tests
Tests fail with `JsonSerializationException` when trying to deserialize error responses. Ensure:
- Tests check `response.StatusCode` before deserializing response body
- Error responses use appropriate status codes (400, 404, etc.)
- DTO deserialization handles both success and error cases

## Individual Test Commands

### Unit Tests - GameServiceTests (15 tests)
1. **Run all unit tests**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests"
   ```

2. **GameServiceTests - GetAllGamesAsync**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.GetAllGamesAsync_WithGames_ReturnsAllGames"
   ```

3. **GameServiceTests - GetGameByIdAsync**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.GetGameByIdAsync_WithExistingId_ReturnsGame"
   ```

4. **GameServiceTests - GetGameByIdAsync NotFound**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.GetGameByIdAsync_WithMissingId_ThrowsNotFoundException"
   ```

5. **GameServiceTests - CreateGameAsync**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.CreateGameAsync_WithValidGame_InsertsAndReturnsId"
   ```

6. **GameServiceTests - CreateGameAsync Duplicate**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.CreateGameAsync_WithDuplicateName_ThrowsValidationException"
   ```

7. **GameServiceTests - UpdateGameAsync**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.UpdateGameAsync_WithValidData_UpdatesGame"
   ```

8. **GameServiceTests - UpdateGameAsync NotFound**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.UpdateGameAsync_WithInvalidGame_ThrowsNotFoundException"
   ```

9. **GameServiceTests - DeleteGameAsync**
   ```bash
   dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.DeleteGameAsync_DeletesSuccessfully"
   ```

10. **GameServiceTests - DeleteGameAsync NotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameServiceTests.DeleteGameAsync_WithMissingId_ThrowsNotFoundException"
    ```

### Unit Tests - MatchServiceTests (11 tests)
11. **Run all MatchServiceTests**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests"
    ```

12. **MatchServiceTests - GetAllMatchesAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.GetAllMatchesAsync_WithMatches_ReturnsAllMatches"
    ```

13. **MatchServiceTests - GetMatchByIdAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.GetMatchByIdAsync_WithExistingId_ReturnsMatch"
    ```

14. **MatchServiceTests - GetMatchByIdAsync NotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.GetMatchByIdAsync_WithMissingId_ThrowsNotFoundException"
    ```

15. **MatchServiceTests - GetMatchWithWinnerAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.GetMatchWithWinnerAsync_WithDataPoints_CalculatesWinner"
    ```

16. **MatchServiceTests - CreateMatchAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.CreateMatchAsync_WithValidRequest_CreatesMatch"
    ```

17. **MatchServiceTests - CreateMatchAsync Invalid**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.CreateMatchAsync_WithInvalidGameId_ThrowsValidationException"
    ```

18. **MatchServiceTests - UpdateMatchAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.UpdateMatchAsync_WithValidData_UpdatesMatch"
    ```

19. **MatchServiceTests - UpdateMatchAsync with DataPoints**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.UpdateMatchAsync_WithGameIdChangeAndDataPoints_ThrowsValidationException"
    ```

20. **MatchServiceTests - UpdateMatchAsync NotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.UpdateMatchAsync_WithMissingId_ThrowsNotFoundException"
    ```

21. **MatchServiceTests - DeleteMatchAsync**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.DeleteMatchAsync_WithExistingId_DeletesMatch"
    ```

22. **MatchServiceTests - DeleteMatchAsync NotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "MatchServiceTests.DeleteMatchAsync_WithMissingId_ThrowsNotFoundException"
    ```

### Integration Tests - GameGetTests
23. **Run all GameGetTests**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests"
    ```

24. **GetGameById_ReturnsGame**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests.GetGameById_ReturnsGame"
    ```

25. **GetGameById_ReturnsNotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests.GetGameById_ReturnsNotFound"
    ```

26. **GetGamesById_ReturnsGameIfExists**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests.GetGamesById_ReturnsGameIfExists"
    ```

27. **GetGamesById_ReturnsNotFound**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests.GetGamesById_ReturnsNotFound"
    ```

28. **GetGamesByDescription_ReturnsGameIfExists**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameGetTests.GetGamesByDescription_ReturnsGameIfExists"
    ```

### Integration Tests - GamePostTests
29. **Run all GamePostTests**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests"
    ```

30. **CreateSingleGame_ReturnsCorrectData**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests.CreateSingleGame_ReturnsCorrectData"
    ```

31. **CreateSingleGame_ResponseHeadersContainExpectedValues**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests.CreateSingleGame_ResponseHeadersContainExpectedValues"
    ```

32. **CreateMultipleGames_ReturnsCorrectData**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests.CreateMultipleGames_ReturnsCorrectData"
    ```

33. **CreateMultipleGames_ReturnsCreated**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests.CreateMultipleGames_ReturnsCreated"
    ```

34. **CreateMultipleGames_ResponseHeadersContainExpectedValues**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests.CreateMultipleGames_ResponseHeadersContainExpectedValues"
    ```

### Integration Tests - GameDeleteTests
35. **Run all GameDeleteTests**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameDeleteTests"
    ```

36. **DeleteGameById_DeletesCorrectEntity**
    ```bash
    dotnet test Api.Tests/Api.Tests.csproj --filter "GameDeleteTests.DeleteGameById_DeletesCorrectEntity"
    ```

## Example Workflow

```bash
# 1. Start test database
docker-compose -f docker-compose.test.yml up -d

# 2. Run unit tests (quick feedback)
dotnet test Api.Tests/Api.Tests.csproj --filter "ServiceTests"

# 3. Run integration tests (full validation)
dotnet test Api.Tests/Api.Tests.csproj

# 4. Check specific endpoint tests
dotnet test Api.Tests/Api.Tests.csproj --filter "GamePostTests" -v detailed

# 5. Clean up when done
docker-compose -f docker-compose.test.yml down
```
