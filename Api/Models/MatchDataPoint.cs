using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

public class MatchDataPoint
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int MatchId { get; set; }

    public string PlayerName { get; set; } = null!;
    public int GamePoints { get; set; }
    public string PointsDescription { get; set; } = null!;
    public DateTime CreatedDate { get; set; }

    [ForeignKey("MatchId")]
    public Match Match { get; set; } = null!;  // Navigation property to Match
}

public class MatchDataPointDto
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string GameName { get; set; } = null!;  // Include GameName in DTO
    public int MatchId { get; set; }
    public bool isMatchFinished { get; set; }
    public string PlayerName { get; set; } = null!;
    public int GamePoints { get; set; }
    public string PointsDescription { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
}
