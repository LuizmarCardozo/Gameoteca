using System.Windows;

namespace Gameoteca.Views
{
    public partial class MessageWindow : Window
    {
        public MessageWindow(string title, string message)
        {
            InitializeComponent();
            LblTitle.Text = title;
            LblMessage.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}