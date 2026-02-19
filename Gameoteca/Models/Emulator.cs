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

        [ObservableProperty]
        private string? _imagePath;

        // IMPORTANTE:
        // Se algum lugar do UI acabar exibindo o objeto Emulator direto,
        // isso garante que aparece o Nome em vez de "Gameoteca.Models.Emulator".
        public override string ToString() => Name;
    }
}
