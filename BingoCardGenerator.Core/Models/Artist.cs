using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BingoCardGenerator.Core.Models;

[Table("artisti")]                    
public class Artist
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nome")]
    public string Name { get; set; } = "";

    [Column("tipo")]
    public string? Type { get; set; }

    [Column("paese")]
    public string? Country { get; set; }

    [Column("mbid")]
    public string? Mbid { get; set; }

    [Column("ImmagineBase64")]        
    public string? ImageBase64 { get; set; }

    [NotMapped]                     
    public byte[]? PhotoBytes =>
        ImageBase64 != null ? Convert.FromBase64String(ImageBase64) : null;
}
