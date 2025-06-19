using System;
using System.Windows;
using BingoCardGenerator.Data;
using BingoCardGenerator.Data.Services;

namespace BingoCardGenerator.UI
{
    public partial class GenerateWindow : Window
    {
        private readonly BingoContext _context;

        public GenerateWindow(BingoContext context)
        {
            InitializeComponent();
            _context = context;
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            // Validazione input
            if (!int.TryParse(txtCount.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Inserisci un numero valido di schede.", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var generator = new CardGenerator(_context);

            try
            {
                // Generazione
                await generator.GenerateAsync(count);

                // Feedback e chiusura
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante la generazione: {ex.Message}", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
