using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gameoteca.Models;
using Gameoteca.Services;

namespace Gameoteca.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LibraryStorage _storage = new();
        private readonly LaunchService _launcher = new();
        private readonly ScanService _scanner = new();
        private readonly DialogService _dialogs = new();

        [ObservableProperty]
        private LibraryState _state = new();

        public ObservableCollection<Emulator> Emulators { get; } = new();
        public ObservableCollection<GameItem> Games { get; } = new();
        public ObservableCollection<FolderMapping> Mappings { get; } = new();

        [ObservableProperty] private Emulator? _selectedEmulator;
        [ObservableProperty] private GameItem? _selectedGame;
        [ObservableProperty] private FolderMapping? _selectedMapping;

        public async Task InitAsync()
        {
            State = await _storage.LoadAsync();

            Emulators.Clear();
            foreach (var e in State.Emulators) Emulators.Add(e);

            Games.Clear();
            foreach (var g in State.Games) Games.Add(g);

            Mappings.Clear();
            foreach (var m in State.Mappings) Mappings.Add(m);
        }

        private async Task PersistAsync()
        {
            State.Emulators = Emulators.ToList();
            State.Games = Games.ToList();
            State.Mappings = Mappings.ToList();

            await _storage.SaveAsync(State);
        }

        [RelayCommand]
        private async Task Save()
        {
            await PersistAsync();
        }

        // =========================
        // EMULADORES
        // =========================

        [RelayCommand]
        private async Task AddEmulator()
        {
            var emu = new Emulator
            {
                Name = "Novo Emulador",
                ExecutablePath = "",
                ArgsTemplate = "\"{rom}\""
            };

            Emulators.Add(emu);
            SelectedEmulator = emu;

            await PersistAsync();
        }

        [RelayCommand]
        private async Task RemoveSelectedEmulator()
        {
            if (SelectedEmulator is null) return;

            foreach (var g in Games.Where(x => x.EmulatorId == SelectedEmulator.Id))
                g.EmulatorId = null;

            Emulators.Remove(SelectedEmulator);
            SelectedEmulator = null;

            await PersistAsync();
        }

        [RelayCommand]
        private void BrowseEmulatorExe()
        {
            if (SelectedEmulator is null) return;

            var initialDir = Path.GetDirectoryName(SelectedEmulator.ExecutablePath);

            var file = _dialogs.PickFile(
                title: "Selecione o executável do emulador (.exe)",
                filter: "Executável (*.exe)|*.exe|Todos os arquivos (*.*)|*.*",
                initialDir: initialDir
            );

            if (file is null) return;

            SelectedEmulator.ExecutablePath = file;

            var wd = Path.GetDirectoryName(file);
            if (!string.IsNullOrWhiteSpace(wd))
                SelectedEmulator.WorkingDirectory = wd;
        }

        // =========================
        // JOGOS
        // =========================

        [RelayCommand]
        private void BrowseGameExe()
        {
            if (SelectedGame is null) return;

            var initialDir = Path.GetDirectoryName(SelectedGame.FilePath);

            var file = _dialogs.PickFile(
                title: "Selecione o jogo (.exe)",
                filter: "Executável (*.exe)|*.exe|Todos os arquivos (*.*)|*.*",
                initialDir: initialDir
            );

            if (file is null) return;

            SelectedGame.FilePath = file;

            if (string.IsNullOrWhiteSpace(SelectedGame.Title))
                SelectedGame.Title = Path.GetFileNameWithoutExtension(file);
        }

        // =========================
        // PASTAS / SCAN
        // =========================

        [RelayCommand]
        private async Task AddMapping()
        {
            var map = new FolderMapping
            {
                FolderPath = "",
                Plataform = "Plataforma",
                EmulatorId = null
            };

            map.Extensions.Add(".zip");

            Mappings.Add(map);
            SelectedMapping = map;

            await PersistAsync();
        }

        [RelayCommand]
        private async Task RemoveSelectedMapping()
        {
            if (SelectedMapping is null) return;

            Mappings.Remove(SelectedMapping);
            SelectedMapping = null;

            await PersistAsync();
        }

        [RelayCommand]
        private void BrowseMappingFolder()
        {
            if (SelectedMapping is null) return;

            var folder = _dialogs.PickFolder(
                title: "Selecione a pasta para mapear (ROMs)",
                initialPath: SelectedMapping.FolderPath
            );

            if (folder is null) return;

            SelectedMapping.FolderPath = folder;
        }

        [RelayCommand]
        private async Task ScanSelectedMapping()
        {
            if (SelectedMapping is null) return;

            var found = _scanner.Scan(SelectedMapping).ToList();

            var existing = Games
                .Select(g => g.FilePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var g in found)
            {
                if (existing.Contains(g.FilePath)) continue;
                Games.Add(g);
            }

            await PersistAsync();
        }

        // =========================
        // EXECUTAR
        // =========================

        [RelayCommand]
        private void PlaySelected()
        {
            if (SelectedGame is null) return;
            if (SelectedGame.EmulatorId is null) return;

            var emu = Emulators.FirstOrDefault(e => e.Id == SelectedGame.EmulatorId.Value);
            if (emu is null) return;

            _launcher.Launch(emu, SelectedGame);
        }
    }
}
