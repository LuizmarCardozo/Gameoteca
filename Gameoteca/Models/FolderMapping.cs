using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameoteca.Models
{
    public partial class FolderMapping : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _folderPath = "";

        [ObservableProperty]
        private string? _plataform;

        [ObservableProperty]
        private Guid? _emulatorId;

        // Lista de extensões (o banco de dados usa isso)
        public List<string> Extensions { get; set; } = new();

        // Texto para a Tabela (Ex: ".zip; .iso")
        // AGORA COM 'SET' PARA VOCÊ PODER EDITAR NA TABELA!
        public string ExtensionsText
        {
            get => string.Join("; ", Extensions);
            set
            {
                // Quebra o texto por ponto e vírgula, vírgula ou espaço
                if (value != null)
                {
                    Extensions = value.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    OnPropertyChanged(nameof(ExtensionsText));
                }
            }
        }
    }
}