public static class SqlTriggers
{
    // PL/pgSQL function: Update Games.MatchesCount when Match inserted
    public const string MatchesTriggerInsertFunction = @"
        CREATE OR REPLACE FUNCTION update_game_match_count_on_insert()
        RETURNS TRIGGER AS $$
        BEGIN
            UPDATE ""Games""
            SET ""MatchesCount"" = (
                SELECT COUNT(*)
                FROM ""Matches""
                WHERE ""GameId"" = NEW.""GameId""
            )
            WHERE ""Id"" = NEW.""GameId"";
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
    ";

    // PL/pgSQL function: Update Games.MatchesCount when Match deleted
    public const string MatchesTriggerDeleteFunction = @"
        CREATE OR REPLACE FUNCTION update_game_match_count_on_delete()
        RETURNS TRIGGER AS $$
        BEGIN
            UPDATE ""Games""
            SET ""MatchesCount"" = (
                SELECT COUNT(*)
                FROM ""Matches""
                WHERE ""GameId"" = OLD.""GameId""
            )
            WHERE ""Id"" = OLD.""GameId"";
            RETURN OLD;
        END;
        $$ LANGUAGE plpgsql;
    ";

    // PL/pgSQL function: Update Matches.PlayerCount when MatchDataPoint inserted
    public const string MatchesDataPointTriggerInsertFunction = @"
        CREATE OR REPLACE FUNCTION update_match_player_count_on_insert()
        RETURNS TRIGGER AS $$
        BEGIN
            UPDATE ""Matches""
            SET ""PlayerCount"" = (
                SELECT COUNT(DISTINCT ""PlayerName"")
                FROM ""MatchDataPoints""
                WHERE ""MatchId"" = NEW.""MatchId""
            )
            WHERE ""Id"" = NEW.""MatchId"";
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
    ";

    // PL/pgSQL function: Update Matches.PlayerCount when MatchDataPoint deleted
    public const string MatchesDataPointTriggerDeleteFunction = @"
        CREATE OR REPLACE FUNCTION update_match_player_count_on_delete()
        RETURNS TRIGGER AS $$
        BEGIN
            UPDATE ""Matches""
            SET ""PlayerCount"" = (
                SELECT COUNT(DISTINCT ""PlayerName"")
                FROM ""MatchDataPoints""
                WHERE ""MatchId"" = OLD.""MatchId""
            )
            WHERE ""Id"" = OLD.""MatchId"";
            RETURN OLD;
        END;
        $$ LANGUAGE plpgsql;
    ";

    // Trigger: Call function on Match insert
    public const string MatchesTriggerInsert = @"
        DROP TRIGGER IF EXISTS trg_update_game_match_count_insert ON ""Matches"";
        CREATE TRIGGER trg_update_game_match_count_insert
        AFTER INSERT ON ""Matches""
        FOR EACH ROW
        EXECUTE FUNCTION update_game_match_count_on_insert();
    ";

    // Trigger: Call function on Match delete
    public const string MatchesTriggerDelete = @"
        DROP TRIGGER IF EXISTS trg_update_game_match_count_delete ON ""Matches"";
        CREATE TRIGGER trg_update_game_match_count_delete
        AFTER DELETE ON ""Matches""
        FOR EACH ROW
        EXECUTE FUNCTION update_game_match_count_on_delete();
    ";

    // Trigger: Call function on MatchDataPoint insert
    public const string MatchesDataPointTriggerInsert = @"
        DROP TRIGGER IF EXISTS trg_update_match_player_count_insert ON ""MatchDataPoints"";
        CREATE TRIGGER trg_update_match_player_count_insert
        AFTER INSERT ON ""MatchDataPoints""
        FOR EACH ROW
        EXECUTE FUNCTION update_match_player_count_on_insert();
    ";

    // Trigger: Call function on MatchDataPoint delete
    public const string MatchesDataPointTriggerDelete = @"
        DROP TRIGGER IF EXISTS trg_update_match_player_count_delete ON ""MatchDataPoints"";
        CREATE TRIGGER trg_update_match_player_count_delete
        AFTER DELETE ON ""MatchDataPoints""
        FOR EACH ROW
        EXECUTE FUNCTION update_match_player_count_on_delete();
    ";
}