using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using BingoCardGenerator.Core.Models;

namespace BingoCardGenerator.Data.Services
{
    public static class CardRenderer
    {
        /// <summary>
        /// Disegna su background le 20 immagini degli artisti.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static void RenderCardImage(
            BingoCard card,
            string backgroundPath,
            string outputPath)
        {
            // Carica lo sfondo
            using Bitmap bg = (Bitmap)Image.FromFile(backgroundPath);
            using Graphics g = Graphics.FromImage(bg);

            const int slotSize = 200;
            const int gapX = 46;
            const int gapY = 40;
            const int startX = 557;
            const int startY = 90;

            // Prendi le entry ordinate per posizione
            var entries = card.Entries
                              .OrderBy(e => e.Position)
                              .ToList();

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    var entry = entries[row * 5 + col];
                    if (string.IsNullOrEmpty(entry.Artist?.ImageBase64))
                        continue;

                    // Decodifica e ridimensiona
                    byte[] data = Convert.FromBase64String(entry.Artist.ImageBase64);
                    using var ms = new MemoryStream(data);
                    using var artistImg = Image.FromStream(ms);
                    using var resized = new Bitmap(artistImg, slotSize, slotSize);

                    // Calcola la posizione
                    int x = startX + col * (slotSize + gapX);
                    int y = startY + row * (slotSize + gapY);

                    // Disegna con il Rectangle‐overload (nessuna ambiguità)
                    g.DrawImage(resized, new Rectangle(x, y, slotSize, slotSize));
                }
            }

            // Salva il risultato
            bg.Save(outputPath, ImageFormat.Png);
        }
    }
}
