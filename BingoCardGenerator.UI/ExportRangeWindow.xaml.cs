using System.Windows;

namespace BingoCardGenerator.UI
{
    public partial class ExportRangeWindow : Window
    {
        public int FromId { get; private set; }
        public int ToId { get; private set; }

        public ExportRangeWindow()
        {
            InitializeComponent();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Validazione input
            if (!int.TryParse(txtFrom.Text, out int from) || from < 0)
            {
                MessageBox.Show("Inserisci un ID di inizio valido.", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!int.TryParse(txtTo.Text, out int to) || to < from)
            {
                MessageBox.Show("Inserisci un ID di fine valido (>= inizio).", "Errore",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            FromId = from;
            ToId = to;
            DialogResult = true;
            Close();
        }
    }
}
