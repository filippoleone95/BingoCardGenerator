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
            previewGrid.Children.Clear();

            foreach (var entry in card.Entries.OrderBy(en => en.Position))
            {
                var img = new Image
                {
                    Width = 80,
                    Height = 80,
                    Margin = new Thickness(4)
                };

                if (entry.Artist?.ImageBase64 is { } b64)
                {
                    img.Source = LoadBitmap(Convert.FromBase64String(b64));
                }
                else
                {
                    img.Source = _emptySlot;
                }

                previewGrid.Children.Add(img);
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
            var bgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "new-game-bg.png");
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
