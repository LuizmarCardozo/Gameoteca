using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Windows;

namespace Gameoteca.Services
{
    public class DialogService
    {
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

        public string? AskForText(string title, string defaultValue = "")
        {
            // Implementação simples - você pode criar uma janela personalizada depois
            // Por enquanto, vamos usar um InputBox simples
            return Microsoft.VisualBasic.Interaction.InputBox(title, "Gameoteca", defaultValue);
        }

        public bool AskConfirmation(string title, string message)
        {
            var result = MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No
            );

            return result == MessageBoxResult.Yes;
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
