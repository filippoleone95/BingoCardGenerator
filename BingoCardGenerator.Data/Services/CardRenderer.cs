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
        public static void RenderCardImage(BingoCard card, string backgroundPath, string outputPath)
        {
            // Carica lo sfondo
            using Bitmap bg = (Bitmap)Image.FromFile(backgroundPath);
            using Graphics g = Graphics.FromImage(bg);

            // Parametri per il layout
            const int slotSize = 150;
            const int gapX = 34;
            const int gapY = 87;
            const int startX = 155;
            const int startY = 207;
            const int textMarginTop = 4;
            const int lineHeight = 20;  // altezza di una riga di testo

            // Preleva e ordina le entry
            var entries = card.Entries
                              .OrderBy(e => e.Position)
                              .ToList();

            // Font e formattazione per il nome
            using var font = new Font("Arial", 12, FontStyle.Bold, GraphicsUnit.Pixel);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.LineLimit
            };

            // Ciclo 4 righe × 5 colonne
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    var entry = entries[row * 5 + col];
                    if (string.IsNullOrEmpty(entry.Artist?.ImageBase64))
                        continue;

                    // Decodifica e ridimensiona l'immagine artista
                    byte[] data = Convert.FromBase64String(entry.Artist.ImageBase64);
                    using var ms = new MemoryStream(data);
                    using var artistImg = Image.FromStream(ms);
                    using var resized = new Bitmap(artistImg, slotSize, slotSize);

                    // Calcola posizione dell’immagine
                    int x = startX + col * (slotSize + gapX);
                    int y = startY + row * (slotSize + gapY);

                    // Disegna l’immagine
                    g.DrawImage(resized, new Rectangle(x, y, slotSize, slotSize));

                    // Prepara il rettangolo per il testo (2 righe al massimo)
                    var textRect = new Rectangle(
                        x,
                        y + slotSize + textMarginTop,
                        slotSize,
                        lineHeight * 2
                    );

                    // Disegna il nome dell’artista, centrato e troncato con "…"
                    g.DrawString(entry.Artist.Name, font, Brushes.Black, textRect, sf);
                }
            }

            // Salva il risultato su file PNG
            bg.Save(outputPath, ImageFormat.Png);
        }

    }
}
