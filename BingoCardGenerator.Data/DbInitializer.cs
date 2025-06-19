using Microsoft.EntityFrameworkCore;

namespace BingoCardGenerator.Data;

public static class DbInitializer
{
    public static void EnsureSchema(BingoContext db)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS Cards (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                CreatedAt TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS CardEntries (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                CardId    INTEGER NOT NULL REFERENCES Cards(Id),
                ArtistId  INTEGER REFERENCES artisti(id),
                Position  INTEGER NOT NULL
            );
        """;
        db.Database.ExecuteSqlRaw(sql);
    }

}
