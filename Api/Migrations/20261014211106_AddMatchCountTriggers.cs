using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameScoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchCountTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PL/pgSQL functions maintaining the denormalized counters.
            // SQL embedded verbatim (migrations are immutable). Previously created at startup
            // from Api/Sql/Triggers/SqlTriggers.cs; moved here so migrations alone provision them.
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION update_game_match_count_on_insert()
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
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION update_game_match_count_on_delete()
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
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION update_match_player_count_on_insert()
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
$$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION update_match_player_count_on_delete()
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
$$ LANGUAGE plpgsql;");

            // Triggers (DROP IF EXISTS + CREATE for idempotency).
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_game_match_count_insert ON ""Matches"";
CREATE TRIGGER trg_update_game_match_count_insert
AFTER INSERT ON ""Matches""
FOR EACH ROW
EXECUTE FUNCTION update_game_match_count_on_insert();");

            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_game_match_count_delete ON ""Matches"";
CREATE TRIGGER trg_update_game_match_count_delete
AFTER DELETE ON ""Matches""
FOR EACH ROW
EXECUTE FUNCTION update_game_match_count_on_delete();");

            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_match_player_count_insert ON ""MatchDataPoints"";
CREATE TRIGGER trg_update_match_player_count_insert
AFTER INSERT ON ""MatchDataPoints""
FOR EACH ROW
EXECUTE FUNCTION update_match_player_count_on_insert();");

            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_match_player_count_delete ON ""MatchDataPoints"";
CREATE TRIGGER trg_update_match_player_count_delete
AFTER DELETE ON ""MatchDataPoints""
FOR EACH ROW
EXECUTE FUNCTION update_match_player_count_on_delete();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first (they depend on the functions), then the functions.
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_game_match_count_insert ON ""Matches"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_game_match_count_delete ON ""Matches"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_match_player_count_insert ON ""MatchDataPoints"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trg_update_match_player_count_delete ON ""MatchDataPoints"";");

            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_game_match_count_on_insert();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_game_match_count_on_delete();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_match_player_count_on_insert();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_match_player_count_on_delete();");
        }
    }
}
