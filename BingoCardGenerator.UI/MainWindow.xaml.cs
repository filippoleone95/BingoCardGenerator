using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using BingoCardGenerator.Core.Models;
using BingoCardGenerator.Data;
using BingoCardGenerator.Data.Services;
using System.Windows.Media;

namespace BingoCardGenerator.UI
{
    public partial class MainWindow : Window
    {
        private BingoContext? _db;
        private CardGenerator? _generator;
        private readonly BitmapImage _emptySlot =
            new(new Uri("pack://application:,,,/assets/unknow-artist.png", UriKind.Absolute));

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "SQLite (*.db;*.sqlite)|*.db;*.sqlite"
            };
            if (dlg.ShowDialog() != true) return;

            txtPath.Text = dlg.FileName;

            _db = new BingoContext(dlg.FileName);
            DbInitializer.EnsureSchema(_db);
            _generator = new CardGenerator(_db);

            RefreshCardList();
            txtStatus.Text = "Database caricato.";
        }

        private void BtnOpenGenerateWindow_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null) return;

            var win = new GenerateWindow(_db)
            {
                Owner = this
            };
            bool? result = win.ShowDialog();
            if (result == true)
            {
                RefreshCardList();
                txtStatus.Text = "Schede generate con successo.";
            }
        }

        private void LvCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LvCards.SelectedItem is not BingoCard stub || _db is null) return;

            var card = _db.Cards
                          .Include(c => c.Entries)
                              .ThenInclude(en => en.Artist)
                          .AsNoTracking()
                          .First(c => c.Id == stub.Id);

            ShowPreview(card);
        }

        private void RefreshCardList()
        {
            if (_db is null) return;

            var cards = _db.Cards
                           .Include(c => c.Entries)
                               .ThenInclude(en => en.Artist)
                           .AsNoTracking()
                           .ToList();

            LvCards.ItemsSource = cards;
        }

        private void ShowPreview(BingoCard card)
        {
            previewCanvas.Children.Clear();
            previewCanvas.ClipToBounds = true;

            const int slotSize = 150;
            const int gapX = 34;
            const int gapY = 87;
            const int startX = 155;
            const int startY = 207;
            const int textMarginTop = 4;

            // Pack URI per i due placeholder
            var ph0 = new BitmapImage(new Uri("pack://application:,,,/assets/unknow-artist.png"));
            var ph1 = new BitmapImage(new Uri("pack://application:,,,/assets/unknow-artist-2.png"));

            // Ordina le entry e crea ciascun elemento
            var entries = card.Entries.OrderBy(e => e.Position).ToList();
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 5; col++)
                {
                    var entry = entries[row * 5 + col];
                    double x = startX + col * (slotSize + gapX);
                    double y = startY + row * (slotSize + gapY);

                    // 1) Immagine (artista o placeholder)
                    var img = new Image
                    {
                        Width = slotSize,
                        Height = slotSize,
                        Source = entry.Artist != null
                                 ? LoadBitmap(Convert.FromBase64String(entry.Artist.ImageBase64))
                                 // determinismo: (cardId + pos) % 2
                                 : ((card.Id + entry.Position) % 2 == 0 ? ph0 : ph1)
                    };
                    Canvas.SetLeft(img, x);
                    Canvas.SetTop(img, y);
                    previewCanvas.Children.Add(img);

                    // 2) Nome artista (solo se non placeholder)
                    if (entry.Artist != null)
                    {
                        var tb = new TextBlock
                        {
                            Text = entry.Artist.Name,
                            Width = slotSize,
                            FontSize = 20,
                            LineHeight = 20 * 1.4,
                            MaxHeight = 20 * 1.2 * 2.4,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = Brushes.White,
                            FontWeight = FontWeights.SemiBold
                        };
                        tb.FontFamily = (FontFamily)Resources["CroissantOneFont"];
                        Canvas.SetLeft(tb, x);
                        Canvas.SetTop(tb, y + slotSize + textMarginTop);
                        previewCanvas.Children.Add(tb);
                    }
                }
        }

        private void BtnExportImage_Click(object sender, RoutedEventArgs e)
        {
            // 1) Prendi la card selezionata
            if (LvCards.SelectedItem is not BingoCard stub || _db is null) return;

            var card = _db.Cards
                          .Include(c => c.Entries)
                              .ThenInclude(en => en.Artist)
                          .AsNoTracking()
                          .First(c => c.Id == stub.Id);

            // 2) Chiedi dove salvare
            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = $"card_{card.Id}.png"
            };
            if (dlg.ShowDialog() != true) return;

            // 3) Chiama il renderer
            var bgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "background.png");
            var outputPath = dlg.FileName;
            try
            {
                CardRenderer.RenderCardImage(card, bgPath, outputPath);
                txtStatus.Text = $"Immagine salvata in: {outputPath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore export: {ex.Message}", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private static BitmapImage LoadBitmap(byte[] data)
        {
            using var ms = new MemoryStream(data);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
    }
}
