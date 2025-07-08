using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Formats.Tar;

namespace BingoCardGenerator.Core.Models;

[Table("CardEntries")]
public class BingoCard
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<CardEntry> Entries { get; set; } = new List<CardEntry>();
}
