using Gameoteca.Views; // Chama as suas janelas Dark
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace Gameoteca.Services
{
    public class DialogService
    {
        // Puxa arquivos do Windows (mantido nativo)
        public string? PickFile(string title, string filter)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        // Puxa pastas do Windows (mantido nativo via Ookii)
        public string? PickFolder(string description)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            return dialog.ShowDialog() == true ? dialog.SelectedPath : null;
        }

        // ✅ Substituído: Chama a SUA tela InputWindow (Dark Mode) para renomear
        public string? AskForText(string title, string defaultValue = "")
        {
            var window = new InputWindow(title, defaultValue);

            if (window.ShowDialog() == true)
            {
                return window.InputText;
            }
            return null;
        }

        // ✅ Substituído: Chama a SUA tela ConfirmWindow (Dark Mode) para Reset
        public bool AskConfirmation(string title, string message)
        {
            var window = new ConfirmWindow(title, message);
            return window.ShowDialog() == true;
        }

        // ✅ CORREÇÃO DO ERRO: Método para Mensagens de Sucesso (Dark Mode)
        public void ShowMessage(string message, string title)
        {
            var window = new MessageWindow(title, message);
            window.ShowDialog();
        }

        // ✅ CORREÇÃO DO ERRO: Método para Erros (Dark Mode)
        public void ShowError(string message, string title)
        {
            // Para o erro, você pode usar a mesma janela de mensagem
            var window = new MessageWindow(title, message);
            window.ShowDialog();
        }
    }
}