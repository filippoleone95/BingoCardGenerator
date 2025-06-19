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
            if (allArtists.Count < 16)
                throw new InvalidOperationException("Servono almeno 16 artisti nel database.");

            var result = new List<BingoCard>();

            // Preleva l'elenco di firme esistenti per evitare duplicati rispetto al DB
            var existingSignatures = await _db.Cards
                .Select(c => c.Entries
                               .Where(e => e.ArtistId != null)
                               .Select(e => e.ArtistId!.Value)
                               .OrderBy(id => id)
                               .ToList())
                .ToListAsync();

            while (result.Count < count)
            {
                var picks = allArtists
                    .OrderBy(_ => _rng.Next())
                    .Take(16)
                    .ToList();

                var pickSignature = picks.Select(a => a.Id).OrderBy(id => id).ToList();

                bool duplicate = existingSignatures.Any(sig => sig.SequenceEqual(pickSignature)) ||
                                  result.Any(c => c.Entries
                                                    .Where(e => e.ArtistId != null)
                                                    .Select(e => e.ArtistId!.Value)
                                                    .OrderBy(id => id)
                                                    .SequenceEqual(pickSignature));

                if (duplicate) continue;

                var card = new BingoCard { CreatedAt = DateTime.UtcNow };
                for (int pos = 0; pos < 20; pos++)
                {
                    var entry = new CardEntry { Position = pos, Card = card };
                    if (pos < 16)
                    {
                        entry.Artist = picks[pos];
                        entry.ArtistId = picks[pos].Id;
                    }
                    card.Entries.Add(entry);
                }

                result.Add(card);
                _db.Cards.Add(card);
                existingSignatures.Add(pickSignature);
            }

            await _db.SaveChangesAsync();
            return result;
        }
    }
}
