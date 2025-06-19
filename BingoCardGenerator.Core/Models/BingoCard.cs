using System.ComponentModel.DataAnnotations.Schema;
using System.Formats.Tar;

namespace BingoCardGenerator.Core.Models;

[Table("CardEntries")]
public class BingoCard
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CardEntry> Entries { get; set; } = new List<CardEntry>();
}
