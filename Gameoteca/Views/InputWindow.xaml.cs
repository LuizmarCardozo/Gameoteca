using System.Windows;

namespace Gameoteca.Views
{
    public partial class InputWindow : Window
    {
        // Propriedade para o programa ler o que foi digitado
        public string InputText => TxtInput.Text;

        public InputWindow(string title, string defaultText = "")
        {
            InitializeComponent();
            LblTitle.Text = title;
            TxtInput.Text = defaultText;

            // Já seleciona o texto todo para facilitar na hora de renomear
            TxtInput.SelectAll();
            TxtInput.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // Diz pro sistema que deu "OK"
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Diz pro sistema que Cancelou
        }
    }
}