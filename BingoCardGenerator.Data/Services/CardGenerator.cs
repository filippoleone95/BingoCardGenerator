using BingoCardGenerator.Core.Models;
using BingoCardGenerator.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoCardGenerator.Data.Services
{
    /// <summary>
    /// Genera BingoCard uniche (16 artisti + 4 slot vuoti) e le salva nel database.
    /// </summary>
    public class CardGenerator
    {
        private readonly BingoContext _db;
        private readonly Random _rng = new();

        public CardGenerator(BingoContext db) => _db = db;

        /// <summary>
        /// Genera <paramref name="count"/> schede uniche e le persiste.
        /// </summary>
        public async Task<List<BingoCard>> GenerateAsync(int count)
        {
            var allArtists = await _db.Artists.ToListAsync();
            if (allArtists.Count < 17)
                throw new InvalidOperationException("Servono almeno 17 artisti nel DB");

            var rnd = new Random();
            var created = new List<BingoCard>();

            for (int i = 0; i < count; i++)
            {
                // 1) Pesca 17 artisti unici
                var picked = allArtists.OrderBy(a => rnd.Next()).Take(17).ToList();

                // 2) Estrai i 5 per la “full row” e i restanti 12 per le altre
                var fullRowArtists = picked.Take(5).ToList();
                var otherArtists = picked.Skip(5).Take(12).ToList();

                // 3) Scegli casualmente quale row (0..3) sarà full
                int fullRow = rnd.Next(4);

                var entries = new List<CardEntry>();
                int otherIdx = 0;

                // 4) Costruisci le 4 righe × 5 colonne
                for (int row = 0; row < 4; row++)
                {
                    if (row == fullRow)
                    {
                        // tutta la row piena di artisti
                        for (int col = 0; col < 5; col++)
                        {
                            entries.Add(new CardEntry
                            {
                                Position = row * 5 + col,
                                Artist = fullRowArtists[col]
                            });
                        }
                    }
                    else
                    {
                        // 4 posizioni a caso per i reali, 1 per il placeholder
                        var artistCols = Enumerable.Range(0, 5)
                                                   .OrderBy(x => rnd.Next())
                                                   .Take(4)
                                                   .ToHashSet();
                        for (int col = 0; col < 5; col++)
                        {
                            if (artistCols.Contains(col))
                            {
                                entries.Add(new CardEntry
                                {
                                    Position = row * 5 + col,
                                    Artist = otherArtists[otherIdx++]
                                });
                            }
                            else
                            {
                                // placeholder (Artist = null)
                                entries.Add(new CardEntry
                                {
                                    Position = row * 5 + col,
                                    Artist = null
                                });
                            }
                        }
                    }
                }

                // 5) Salva la card
                var card = new BingoCard { CreatedAt = DateTime.UtcNow, Entries = entries };
                _db.Cards.Add(card);
                await _db.SaveChangesAsync();
                created.Add(card);
            }

            return created;
        }

    }
}
