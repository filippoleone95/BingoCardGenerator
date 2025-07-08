using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using BingoCardGenerator.Core.Models;

namespace BingoCardGenerator.Data.Services
{
    [SupportedOSPlatform("windows")]
    public static class CardRenderer
    {
        // 1) Risorse statiche caricate una volta
        private static readonly Bitmap TemplateBg;
        private static readonly Bitmap Placeholder0;
        private static readonly Bitmap Placeholder1;
        private static readonly Font CustomFont;
        private static readonly Font CustomIDFont;
        private static readonly StringFormat TextFormat;
        private static readonly PrivateFontCollection _fontCollection = new();

        static CardRenderer()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Sfondo "master"
            TemplateBg    = (Bitmap)Image.FromFile(Path.Combine(baseDir, "assets", "background.png"));
            // Placeholder anonimi
            Placeholder0 = (Bitmap)Image.FromFile(Path.Combine(baseDir, "assets", "unknow-artist.png"));
            Placeholder1 = (Bitmap)Image.FromFile(Path.Combine(baseDir, "assets", "unknow-artist-2.png"));

            _fontCollection.AddFontFile(Path.Combine(baseDir, "assets", "fonts", "CroissantOne-Regular.ttf"));

            CustomFont = new Font(_fontCollection.Families[0], 20, FontStyle.Regular, GraphicsUnit.Pixel);
            CustomIDFont = new Font(_fontCollection.Families[0], 50, FontStyle.Regular, GraphicsUnit.Pixel);

            // StringFormat per centrare e limitare a 2 righe
            TextFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.LineLimit
            };
        }

        [SupportedOSPlatform("windows")]
        public static void RenderCardImage(
            BingoCard card,
            string outputPath)
        {
            // Clona lo sfondo (non stiriamo direttamente TemplateBg)
            using var bg = new Bitmap(TemplateBg);
            using var g = Graphics.FromImage(bg);

            // Layout constants
            const int slotSize = 150;
            const int gapX = 34;
            const int gapY = 87;
            const int startX = 155;
            const int startY = 207;
            const int textMarginTop = 4;
            int lineHeight = (int)(CustomFont.Size * 1.2);

            var entries = card.Entries.OrderBy(e => e.Position).ToList();
            int idx = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 5; col++, idx++)
                {
                    int x = startX + col * (slotSize + gapX);
                    int y = startY + row * (slotSize + gapY);
                    var entry = entries[idx];

                    // 1) Disegna artista o placeholder
                    if (entry.Artist != null)
                    {
                        byte[] data = Convert.FromBase64String(entry.Artist.ImageBase64);
                        using var ms = new MemoryStream(data);
                        using var artImg = Image.FromStream(ms);
                        using var resizedImg = new Bitmap(artImg, slotSize, slotSize);
                        g.DrawImage(resizedImg, new Rectangle(x, y, slotSize, slotSize));
                    }
                    else
                    {
                        var ph = ((card.Id + entry.Position) % 2 == 0)
                                     ? Placeholder0
                                     : Placeholder1;
                        using var resizedPh = new Bitmap(ph, slotSize, slotSize);
                        g.DrawImage(resizedPh, new Rectangle(x, y, slotSize, slotSize));
                    }

                    // 2) Testo: wrap “semplice” su ultimo spazio se c’è
                    if (entry.Artist != null)
                    {
                        string name = entry.Artist.Name;
                        int breakPos = name.LastIndexOf(' ');
                        if (breakPos > 0)
                        {
                            // spezza in due righe all’ultimo spazio
                            string line1 = name.Substring(0, breakPos).Trim();
                            string line2 = name.Substring(breakPos).Trim();

                            // disegna prima riga
                            var rect1 = new Rectangle(
                                x,
                                y + slotSize + textMarginTop,
                                slotSize,
                                lineHeight);
                            g.DrawString(line1, CustomFont, Brushes.White, rect1, TextFormat);

                            // disegna seconda riga
                            var rect2 = new Rectangle(
                                x,
                                y + slotSize + textMarginTop + lineHeight,
                                slotSize,
                                lineHeight);
                            g.DrawString(line2, CustomFont, Brushes.White, rect2, TextFormat);
                        }
                        else
                        {
                            // nessuno spazio: rimane tutto su una riga
                            var rect = new Rectangle(
                                x,
                                y + slotSize + textMarginTop,
                                slotSize,
                                lineHeight);
                            g.DrawString(name, CustomFont, Brushes.White, rect, TextFormat);
                        }
                    }

                }
            }

            // 3) Disegna l'ID della card
            int idX = 1060;
            int idY = 1080;
            int idWidth = 150;
            int idHeight = 100;

            var idRect = new Rectangle(idX, idY, idWidth, idHeight);

            g.DrawString(
                card.Id.ToString(),
                CustomIDFont,
                Brushes.White,
                idRect,
                new StringFormat { Alignment = StringAlignment.Center }
            );

            bg.Save(outputPath, ImageFormat.Png);
        }

    }
}
