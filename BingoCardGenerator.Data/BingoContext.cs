using BingoCardGenerator.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Formats.Tar;

namespace BingoCardGenerator.Data;

public class BingoContext(string dbPath) : DbContext
{
    private readonly string _dbPath = dbPath;

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<BingoCard> Cards => Set<BingoCard>();
    public DbSet<CardEntry> CardEntries => Set<CardEntry>();

    protected override void OnConfiguring(DbContextOptionsBuilder o)
        => o.UseSqlite($"Data Source={_dbPath}");

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Artist già mappato via attributi

        model.Entity<BingoCard>().ToTable("Cards");
        model.Entity<CardEntry>().ToTable("CardEntries");

        model.Entity<CardEntry>()
             .HasOne(e => e.Card)
             .WithMany(c => c.Entries)
             .HasForeignKey(e => e.CardId);

        // relazione facoltativa con Artist (ArtistId può essere null)
        model.Entity<CardEntry>()
             .HasOne(e => e.Artist)
             .WithMany()
             .HasForeignKey(e => e.ArtistId);
    }

}
