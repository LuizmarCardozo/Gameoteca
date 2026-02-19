using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Gameoteca.Models
{
    public partial class GameItem : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _title = "";

        // Continua sendo usado como "caminho base" (exe / url / lnk)
        [ObservableProperty]
        private string _filePath = "";

        [ObservableProperty]
        private string? _plataform = "PC";

        [ObservableProperty]
        private Guid? _emulatorId;

        // Caminho da imagem de capa
        [ObservableProperty]
        private string? _imagePath;

        // ✅ NOVO: indica que esse item é um atalho (Steam/Epic/URL/lnk)
        [ObservableProperty]
        private bool _isShortcut;

        // ✅ NOVO: para .url (atalho da Internet) a gente salva o URI real aqui
        // Ex.: steam://rungameid/123456  ou  com.epicgames.launcher://apps/...
        [ObservableProperty]
        private string? _launchUri;
    }
}
