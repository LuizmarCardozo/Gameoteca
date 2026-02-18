using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using Gameoteca.Views; // Importante para achar a InputWindow

namespace Gameoteca.Services
{
    public class DialogService
    {
        public string? PickFile(string title, string filter, string? initialDir = null)
        {
            var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(initialDir))
            {
                try { dlg.InitialDirectory = initialDir; } catch { }
            }

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        public string? PickFolder(string title, string? initialPath = null)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                Description = title,
                UseDescriptionForTitle = true,
                SelectedPath = initialPath ?? ""
            };

            return dlg.ShowDialog() == true ? dlg.SelectedPath : null;
        }

        // Abre a janelinha roxa de input
        public string? AskForText(string title, string currentValue)
        {
            // Certifique-se de que criou o InputWindow.xaml conforme o passo anterior
            var window = new InputWindow(title, currentValue);

            bool? result = window.ShowDialog();

            if (result == true)
            {
                return window.ResultText;
            }
            return null;
        }
    }
}