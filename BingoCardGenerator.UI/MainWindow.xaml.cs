using BingoCardGenerator.Core.Models;
using BingoCardGenerator.Data;
using BingoCardGenerator.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            // Ora che il DB è pronto, abilito i bottoni
            BtnOpenGenerateWindow.IsEnabled = true;
            BtnExportRange.IsEnabled = true;
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

        private void BtnExportRange_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null) return;

            // 1) Apri dialog per inserire range
            var rangeDlg = new ExportRangeWindow { Owner = this };
            if (rangeDlg.ShowDialog() != true) return;

            int fromId = rangeDlg.FromId;
            int toId = rangeDlg.ToId;

            // 2) Seleziona cartella di destinazione
            var totalCards = _db.Cards.Count();
            if (totalCards == 0)
            {
                MessageBox.Show("Non ci sono schede nel database.", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1.2) Trova l'ID massimo attualmente esistente
            int maxId = _db.Cards.Max(c => c.Id);

            // 1.3) Validazione intervallo
            if (fromId < 1 || fromId > maxId)
            {
                MessageBox.Show($"ID di inizio non valido: deve essere compreso tra 1 e {maxId}.",
                                "Errore intervallo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (toId < fromId || toId > maxId)
            {
                MessageBox.Show($"ID di fine non valido: deve essere compreso tra {fromId} e {maxId}.",
                                "Errore intervallo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Seleziona la cartella di destinazione"
            };
            if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
            string folder = dlg.FileName;

            // 3) Preleva le card nel range
            var cards = _db.Cards
                           .Include(c => c.Entries)
                             .ThenInclude(en => en.Artist)
                           .AsNoTracking()
                           .Where(c => c.Id >= fromId && c.Id <= toId)
                           .OrderBy(c => c.Id)
                           .ToList();

            // 3.1) Controlla che ci siano effettivamente schede da esportare
            if (cards.Count == 0)
            {
                MessageBox.Show("Nessuna scheda trovata nell'intervallo specificato.", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4) Esporta tutte le schede a PNG
            string bgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                         "assets", "background.png");
            int count = 0;
            foreach (var card in cards)
            {
                string outputPath = Path.Combine(folder,
                    $"card_{card.Id}.png");
                CardRenderer.RenderCardImage(card, bgPath, outputPath);
                count++;
            }

            // 5) Feedback
            txtStatus.Text = $"Esportate {count} schede in {folder}";
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
