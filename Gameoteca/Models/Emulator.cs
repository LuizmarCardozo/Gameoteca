using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Gameoteca.Models
{
    public partial class Emulator : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _name = "";

        [ObservableProperty]
        private string _executablePath = "";

        [ObservableProperty]
        private string _argsTemplate = "\"{rom}\"";

        [ObservableProperty]
        private string? _workingDirectory;

        // NOVA PROPRIEDADE: Logo/Imagem do Emulador
        [ObservableProperty]
        private string? _imagePath;
    }
}