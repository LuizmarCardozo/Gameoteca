using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;


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
                dlg.InitialDirectory = initialDir;

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
    }
}
