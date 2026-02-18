using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Gameoteca.Models
{
    public partial class GameItem : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string _filePath = "";

        [ObservableProperty]
        private string? _plataform = "PC";

        [ObservableProperty]
        private Guid? _emulatorId;

        // NOVA PROPRIEDADE: Caminho da imagem de capa
        [ObservableProperty]
        private string? _imagePath;
    }
}