using System.ComponentModel.DataAnnotations.Schema;

namespace BingoCardGenerator.Core.Models;

[Table("Cards")]
public class CardEntry
{
    public int Id { get; set; }
    public int Position { get; set; }     // 0–19
    public int? ArtistId { get; set; }
    public Artist? Artist { get; set; }

    public int CardId { get; set; }
    public BingoCard Card { get; set; } = null!;
}
