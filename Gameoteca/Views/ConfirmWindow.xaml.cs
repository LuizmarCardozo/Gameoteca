using System.Windows;

namespace Gameoteca.Views
{
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow(string title, string message)
        {
            InitializeComponent();
            LblTitle.Text = title;
            LblMessage.Text = message;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}