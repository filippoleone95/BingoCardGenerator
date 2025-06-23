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
    public static class CardRenderer
    {
        private static readonly PrivateFontCollection _fontCollection;
        static CardRenderer()
        {
            _fontCollection = new PrivateFontCollection();
            var fontPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets", "fonts", "CroissantOne-Regular.ttf");
            _fontCollection.AddFontFile(fontPath);
        }

        [SupportedOSPlatform("windows")]
        public static void RenderCardImage(BingoCard card, string backgroundPath, string outputPath)
        {
            using Bitmap bg = (Bitmap)Image.FromFile(backgroundPath);
            using Graphics g = Graphics.FromImage(bg);

            const int slotSize = 150;
            const int gapX = 34;
            const int gapY = 87;
            const int startX = 155;
            const int startY = 207;
            const int textMarginTop = 4;
            const int fontSize = 20;
            int lineHeight = (int)(fontSize * 1.2);

            // placeholder images
            using var ph0 = Image.FromFile(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets", "unknow-artist.png"));
            using var ph1 = Image.FromFile(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets", "unknow-artist-2.png"));

            // font custom
            var croissantFamily = _fontCollection.Families[0];
            using var font = new Font(
                croissantFamily,
                fontSize,
                FontStyle.Regular,
                GraphicsUnit.Pixel);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                Trimming = StringTrimming.None,
                FormatFlags = StringFormatFlags.LineLimit
            };

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
                        var ph = ((card.Id + entry.Position) % 2 == 0) ? ph0 : ph1;
                        using var resizedPh = new Bitmap(ph, slotSize, slotSize);
                        g.DrawImage(resizedPh, new Rectangle(x, y, slotSize, slotSize));
                    }

                    // 2) Disegna nome: spezzalo solo se non ci sta su una riga
                    if (entry.Artist != null)
                    {
                        string name = entry.Artist.Name;
                        // misura la stringa intera
                        var fullSize = g.MeasureString(name, font);

                        if (fullSize.Width <= slotSize)
                        {
                            // ci sta: disegna in un'unica riga
                            var rect = new Rectangle(x, y + slotSize + textMarginTop, slotSize, lineHeight);
                            g.DrawString(name, font, Brushes.White, rect, sf);
                        }
                        else
                        {
                            // non ci sta: trova l'ultimo spazio che permette di farci stare la prima parte
                            int breakPos = -1;
                            for (int i = 0; i < name.Length; i++)
                            {
                                if (name[i] != ' ') continue;
                                string part = name.Substring(0, i);
                                if (g.MeasureString(part, font).Width <= slotSize)
                                    breakPos = i;
                                else
                                    break;
                            }
                            // se non troviamo spazio valido, spezza a metà parola
                            if (breakPos < 0)
                                breakPos = name.Length / 2;

                            string line1 = name.Substring(0, breakPos).Trim();
                            string line2 = name.Substring(breakPos).Trim();

                            // disegna le due righe
                            var rect1 = new Rectangle(
                                x,
                                y + slotSize + textMarginTop,
                                slotSize,
                                lineHeight);
                            g.DrawString(line1, font, Brushes.White, rect1, sf);

                            var rect2 = new Rectangle(
                                x,
                                y + slotSize + textMarginTop + lineHeight,
                                slotSize,
                                lineHeight);
                            g.DrawString(line2, font, Brushes.White, rect2, sf);
                        }
                    }
                }
            }

            bg.Save(outputPath, ImageFormat.Png);
        }

    }
}
