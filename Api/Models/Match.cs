using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

public class Match
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int GameId { get; set; }  // Foreign key referencing the Game table
    public DateTime MatchDate { get; set; }
    public string? Notes { get; set; }
    public bool isFinished { get; set; }
    public int PlayerCount { get; set; }
    // Navigation property for related Game
    public Game Game { get; set; } = null!;

    // Navigation property for related MatchDataPoints
    public List<MatchDataPoint> MatchDataPoints { get; set; } = new();
}
