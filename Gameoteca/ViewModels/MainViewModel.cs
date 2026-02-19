using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gameoteca.Models;
using Gameoteca.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Gameoteca.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LibraryStorage _storage = new();
        private readonly LaunchService _launcher = new();
        private readonly DialogService _dialogs = new();
        private readonly ScanService _scanner = new();

        public ObservableCollection<Emulator> Emulators { get; } = new();
        public ObservableCollection<GameItem> Games { get; } = new();
        public ObservableCollection<FolderMapping> Mappings { get; } = new();

        // Lista de extensões para o seletor da aba Pastas
        public ObservableCollection<string> AvailableExtensions { get; } = new()
        {
            ".zip", ".7z", ".iso", ".bin", ".cue", ".smc", ".sfc", ".n64", ".z64", ".gba", ".nds", ".exe"
        };

        [ObservableProperty] private GameItem? _selectedGame;
        [ObservableProperty] private Emulator? _selectedEmulator;
        [ObservableProperty] private FolderMapping? _selectedMapping;

        public async Task InitAsync()
        {
            var state = await _storage.LoadAsync();
            Emulators.Clear(); foreach (var e in state.Emulators) Emulators.Add(e);
            Games.Clear(); foreach (var g in state.Games) Games.Add(g);
            Mappings.Clear(); foreach (var m in state.Mappings) Mappings.Add(m);
        }

        private async Task PersistAsync() => await _storage.SaveAsync(new LibraryState
        {
            Games = Games.ToList(),
            Emulators = Emulators.ToList(),
            Mappings = Mappings.ToList()
        });

        // --- ASSOCIAÇÃO (mantive o "Definir como PC") ---
        [RelayCommand]
        private async Task ClearGameEmulator(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            target.EmulatorId = null;
            target.Plataform = "PC";
            await PersistAsync();
        }

        // --- EXTENSÕES ---
        [RelayCommand]
        private async Task AddCustomExtension(FolderMapping map)
        {
            if (map == null) return;

            var newExt = _dialogs.AskForText("Outra Extensão", "Digite o formato:");
            if (!string.IsNullOrWhiteSpace(newExt))
            {
                if (!newExt.StartsWith(".")) newExt = "." + newExt;
                map.ExtensionsText = string.IsNullOrWhiteSpace(map.ExtensionsText) ? newExt : map.ExtensionsText + "; " + newExt;
                await PersistAsync();
            }
        }

        // --- BOTÃO DIREITO E AÇÕES ---
        [RelayCommand]
        private async Task RemoveGame(GameItem? item)
        {
            var t = item ?? SelectedGame;
            if (t != null) Games.Remove(t);
            await PersistAsync();
        }

        [RelayCommand]
        private async Task RemoveEmulator(Emulator? item)
        {
            var t = item ?? SelectedEmulator;
            if (t != null) Emulators.Remove(t);
            await PersistAsync();
        }

        [RelayCommand]
        private async Task RemoveMapping(FolderMapping? item)
        {
            var t = item ?? SelectedMapping;
            if (t != null) Mappings.Remove(t);
            await PersistAsync();
        }

        // ✅ RENOMEAR JOGO (corrigido)
        [RelayCommand]
        private async Task RenameGame(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            var current = target.Title ?? "";
            var newName = _dialogs.AskForText("Renomear Jogo", current);

            if (string.IsNullOrWhiteSpace(newName)) return;

            newName = newName.Trim();
            if (newName == current) return;

            target.Title = newName;
            await PersistAsync();
        }

        // ✅ RENOMEAR EMULADOR (corrigido + atualiza plataforma dos jogos ligados)
        [RelayCommand]
        private async Task RenameEmulator(Emulator? item)
        {
            var target = item ?? SelectedEmulator;
            if (target == null) return;

            var current = target.Name ?? "";
            var newName = _dialogs.AskForText("Renomear Emulador", current);

            if (string.IsNullOrWhiteSpace(newName)) return;

            newName = newName.Trim();
            if (newName == current) return;

            target.Name = newName;

            // Atualiza texto da plataforma nos jogos que usam esse emulador
            foreach (var g in Games.Where(g => g.EmulatorId == target.Id))
                g.Plataform = newName;

            await PersistAsync();
        }

        [RelayCommand]
        private void PlaySelected(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            if (target.EmulatorId != null)
            {
                var emu = Emulators.FirstOrDefault(e => e.Id == target.EmulatorId);
                if (emu != null) { _launcher.Launch(emu, target); return; }
            }

            _launcher.LaunchGameOnly(target);
        }

        [RelayCommand]
        private async Task ScanSelectedMapping(FolderMapping? map)
        {
            var target = map ?? SelectedMapping;
            if (target == null || !Directory.Exists(target.FolderPath)) return;

            var found = _scanner.Scan(target).ToList();
            var existing = Games.Select(g => g.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var emu = Emulators.FirstOrDefault(e => e.Id == target.EmulatorId);
            var platName = emu?.Name ?? "PC";

            foreach (var g in found)
            {
                if (!existing.Contains(g.FilePath))
                {
                    g.EmulatorId = target.EmulatorId;
                    g.Plataform = platName;
                    Games.Add(g);
                }
            }

            await PersistAsync();
        }

        [RelayCommand] private async Task Save() => await PersistAsync();

        [RelayCommand]
        private async Task AddGame()
        {
            var f = _dialogs.PickFile("Add Jogo", "*.exe|*.exe");
            if (f != null)
            {
                Games.Add(new GameItem
                {
                    Title = Path.GetFileNameWithoutExtension(f),
                    FilePath = f,
                    Plataform = "PC"
                });
                await PersistAsync();
            }
        }

        [RelayCommand]
        private async Task AddEmulator()
        {
            var f = _dialogs.PickFile("Add Emulador", "*.exe|*.exe");
            if (f != null)
            {
                Emulators.Add(new Emulator
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    ExecutablePath = f
                });
                await PersistAsync();
            }
        }

        [RelayCommand]
        private async Task AddMapping()
        {
            var f = _dialogs.PickFolder("Pasta ROMs");
            if (f != null)
            {
                Mappings.Add(new FolderMapping
                {
                    FolderPath = f,
                    Plataform = "PC"
                });
                await PersistAsync();
            }
        }

        [RelayCommand]
        private async Task ChangeGameImage(GameItem? item)
        {
            var t = item ?? SelectedGame;
            if (t != null)
            {
                var img = _dialogs.PickFile("Capa", "Img|*.jpg;*.png");
                if (img != null) t.ImagePath = img;
                await PersistAsync();
            }
        }
    }
}
