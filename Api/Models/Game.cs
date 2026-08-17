using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Game
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string GameName { get; set; }
    public string? GameDescription { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int AverageDuration { get; set; }
    public int MatchesCount { get; set; }
    public ICollection<Match> Matches { get; set; }  // Navigation property to Matches
}
public class GameDto
{
    public int Id { get; set; }  // Add the Id property
    public string GameName { get; set; }
    public string GameDescription { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int AverageDuration { get; set; }
    public int MatchesCount { get; set; }
}

public class GameWithMatchDataPointDto
{
    public int Id { get; set; }
    public string GameName { get; set; }
    public string GameDescription { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int AverageDuration { get; set; }
    public int MatchesCount { get; set; }
    public List<MatchForGameDto> Matches { get; set; } = new List<MatchForGameDto>();
}

/// <summary>
/// Request DTO for creating a new game.
/// </summary>
public class CreateGameRequest
{
    public string GameName { get; set; }
    public string GameDescription { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int AverageDuration { get; set; }
}

/// <summary>
/// Request DTO for updating an existing game.
/// </summary>
public class UpdateGameRequest
{
    public string GameName { get; set; }
    public string GameDescription { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int AverageDuration { get; set; }
}