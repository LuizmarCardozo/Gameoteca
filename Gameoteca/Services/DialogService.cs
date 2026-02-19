using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using Gameoteca.Views;

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

        // Mantém compatibilidade com chamadas antigas.
        public string? AskForText(string title, string currentValue)
            => AskForText(title, prompt: "", initialValue: currentValue);

        // Overload: permite passar um valor inicial (e opcionalmente um prompt futuro)
        public string? AskForText(string title, string prompt, string? initialValue = null)
        {
            // (prompt está aqui só pra facilitar evoluir depois; a InputWindow atual não usa)
            var window = new InputWindow(title, initialValue ?? "");
            var result = window.ShowDialog();
            return result == true ? window.ResultText : null;
        }
    }
}
