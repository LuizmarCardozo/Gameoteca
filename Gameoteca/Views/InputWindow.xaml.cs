using System.Windows;

namespace Gameoteca.Views
{
    public partial class InputWindow : Window
    {
        public string ResultText { get; private set; } = string.Empty;

        public InputWindow(string title, string defaultText)
        {
            InitializeComponent();
            LblTitle.Text = title;
            TxtInput.Text = defaultText;
            TxtInput.Focus();
            TxtInput.SelectAll();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ResultText = TxtInput.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}