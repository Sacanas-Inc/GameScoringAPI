-- ============================================================================
-- Milestone 3 Acceptance Testing: Triggers & Migrations Validation
-- ============================================================================
-- Run these queries against your Postgres database to verify:
-- 1. Schema exists (migrations applied)
-- 2. Triggers present and functional
-- 3. Games.MatchesCount auto-updates on insert/delete
-- 4. Matches.PlayerCount auto-updates on insert/delete
-- ============================================================================
-- Compatible with: psql, DBeaver, Azure Data Studio, pgAdmin, any SQL client
-- Usage: Copy/paste sections or run: psql -h localhost -U postgres -d gamescoringdb -f TEST_TRIGGERS_CLEAN.sql
-- ============================================================================

-- ============================================================================
-- PART 1: Verify Schema Exists (Migrations Applied)
-- ============================================================================

SELECT '=== PART 1: Verify Schema ===' AS "Step";

-- Check if tables exist
SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name='Games') AS "Games_exists",
       EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name='Matches') AS "Matches_exists",
       EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name='MatchDataPoints') AS "MatchDataPoints_exists";

-- List all tables
SELECT '--- All tables ---' AS "Info";
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- ============================================================================
-- PART 2: Verify Triggers Exist
-- ============================================================================

SELECT '=== PART 2: Verify Triggers Exist ===' AS "Step";

-- List all triggers
SELECT '--- All triggers ---' AS "Info";
SELECT trigger_name, event_object_table, event_manipulation
FROM information_schema.triggers
WHERE trigger_schema = 'public'
ORDER BY event_object_table, trigger_name;

-- List trigger functions
SELECT '--- All trigger functions ---' AS "Info";
SELECT proname, prosrc
FROM pg_proc
WHERE proname LIKE 'update_%'
ORDER BY proname;

-- ============================================================================
-- PART 3: Test Games.MatchesCount Auto-Update on Match Insert
-- ============================================================================

SELECT '=== PART 3: Test Games.MatchesCount Auto-Update (Insert) ===' AS "Step";

-- Clean up test data (if exists)
DELETE FROM "Games" WHERE "GameName" = 'Test_Chess';

-- Insert a test game with MatchesCount = 0
INSERT INTO "Games" ("GameName", "GameDescription", "MinPlayers", "MaxPlayers", "AverageDuration", "MatchesCount")
VALUES ('Test_Chess', 'Test game for triggers', 2, 2, 45, 0);

SELECT '--- Before insert match ---' AS "Info";
SELECT "Id", "GameName", "MatchesCount" FROM "Games" WHERE "GameName" = 'Test_Chess';

-- Insert Match #1 - MatchesCount should become 1
INSERT INTO "Matches" ("GameId", "MatchDate", "Notes", "isFinished", "PlayerCount")
VALUES (
  (SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'),
  NOW(),
  'Match 1',
  false,
  0
);

SELECT '--- After insert match 1 (MatchesCount should be 1) ---' AS "Info";
SELECT "Id", "GameName", "MatchesCount" FROM "Games" WHERE "GameName" = 'Test_Chess';

-- Insert Match #2 - MatchesCount should become 2
INSERT INTO "Matches" ("GameId", "MatchDate", "Notes", "isFinished", "PlayerCount")
VALUES (
  (SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'),
  NOW(),
  'Match 2',
  false,
  0
);

SELECT '--- After insert match 2 (MatchesCount should be 2) ---' AS "Info";
SELECT "Id", "GameName", "MatchesCount" FROM "Games" WHERE "GameName" = 'Test_Chess';

-- ============================================================================
-- PART 4: Test Games.MatchesCount Auto-Update on Match Delete
-- ============================================================================

SELECT '=== PART 4: Test Games.MatchesCount Auto-Update (Delete) ===' AS "Step";

-- Get a match ID to delete
WITH match_to_delete AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess')
  LIMIT 1
)
DELETE FROM "Matches" WHERE "Id" IN (SELECT "Id" FROM match_to_delete);

SELECT '--- After delete match (MatchesCount should be 1) ---' AS "Info";
SELECT "Id", "GameName", "MatchesCount" FROM "Games" WHERE "GameName" = 'Test_Chess';

-- ============================================================================
-- PART 5: Test Matches.PlayerCount Auto-Update on MatchDataPoint Insert
-- ============================================================================

SELECT '=== PART 5: Test Matches.PlayerCount Auto-Update (Insert) ===' AS "Step";

-- Get remaining match ID
SELECT '--- Before insert datapoint ---' AS "Info";
WITH game_id AS (
  SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'
),
match_id AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM game_id)
  LIMIT 1
)
SELECT m."Id", m."PlayerCount" 
FROM "Matches" m, match_id
WHERE m."Id" = match_id."Id";

-- Insert MatchDataPoint #1 - PlayerCount should become 1
INSERT INTO "MatchDataPoints" ("MatchId", "PlayerName", "GamePoints", "PointsDescription", "CreatedDate")
VALUES (
  (SELECT "Id" FROM "Matches" WHERE "GameId" = (SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess') LIMIT 1),
  'Alice',
  10,
  'White pieces',
  NOW()
);

SELECT '--- After insert datapoint 1 (PlayerCount should be 1) ---' AS "Info";
WITH game_id AS (
  SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'
),
match_id AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM game_id)
  LIMIT 1
)
SELECT m."Id", m."PlayerCount"
FROM "Matches" m, match_id
WHERE m."Id" = match_id."Id";

-- Insert MatchDataPoint #2 (different player) - PlayerCount should become 2
INSERT INTO "MatchDataPoints" ("MatchId", "PlayerName", "GamePoints", "PointsDescription", "CreatedDate")
VALUES (
  (SELECT "Id" FROM "Matches" WHERE "GameId" = (SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess') LIMIT 1),
  'Bob',
  5,
  'Black pieces',
  NOW()
);

SELECT '--- After insert datapoint 2 (PlayerCount should be 2) ---' AS "Info";
WITH game_id AS (
  SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'
),
match_id AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM game_id)
  LIMIT 1
)
SELECT m."Id", m."PlayerCount"
FROM "Matches" m, match_id
WHERE m."Id" = match_id."Id";

-- ============================================================================
-- PART 6: Test Matches.PlayerCount Auto-Update on MatchDataPoint Delete
-- ============================================================================

SELECT '=== PART 6: Test Matches.PlayerCount Auto-Update (Delete) ===' AS "Step";

-- Delete one MatchDataPoint - PlayerCount should become 1
WITH game_id AS (
  SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'
),
match_id AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM game_id)
  LIMIT 1
),
datapoint_to_delete AS (
  SELECT "Id" FROM "MatchDataPoints"
  WHERE "MatchId" IN (SELECT "Id" FROM match_id)
  LIMIT 1
)
DELETE FROM "MatchDataPoints"
WHERE "Id" IN (SELECT "Id" FROM datapoint_to_delete);

SELECT '--- After delete datapoint (PlayerCount should be 1) ---' AS "Info";
WITH game_id AS (
  SELECT "Id" FROM "Games" WHERE "GameName" = 'Test_Chess'
),
match_id AS (
  SELECT "Id" FROM "Matches"
  WHERE "GameId" = (SELECT "Id" FROM game_id)
  LIMIT 1
)
SELECT m."Id", m."PlayerCount"
FROM "Matches" m, match_id
WHERE m."Id" = match_id."Id";

-- ============================================================================
-- PART 7: Clean Up Test Data
-- ============================================================================

SELECT '=== PART 7: Clean Up ===' AS "Step";

DELETE FROM "Games" WHERE "GameName" = 'Test_Chess';

SELECT '--- Test data cleaned up ---' AS "Info";

-- ============================================================================
-- SUMMARY
-- ============================================================================

SELECT '=== SUMMARY ===' AS "Step";
SELECT 'If all queries executed successfully and showed:' AS "Note";
SELECT '  ✓ All 3 tables exist' AS "Criteria";
SELECT '  ✓ 4 triggers present (trg_update_game_match_count_insert, etc.)' AS "Criteria";
SELECT '  ✓ 4 trigger functions exist (update_game_match_count_on_insert, etc.)' AS "Criteria";
SELECT '  ✓ Games.MatchesCount incremented/decremented on Match insert/delete' AS "Criteria";
SELECT '  ✓ Matches.PlayerCount incremented/decremented on MatchDataPoint insert/delete' AS "Criteria";
SELECT 'Then Milestone 3 acceptance criteria are MET!' AS "Result";
