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

        // ✅ RENOMEAR JOGO
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

        // ✅ RENOMEAR EMULADOR (atualiza plataforma dos jogos ligados)
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

            foreach (var g in Games.Where(g => g.EmulatorId == target.Id))
                g.Plataform = newName;

            await PersistAsync();
        }

        // ✅ JOGAR (agora suporta Steam/Epic via .url / .lnk)
        [RelayCommand]
        private void PlaySelected(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            // Se for atalho/URI, abre via Shell (Steam/Epic/Browser)
            if (IsShortcutLike(target))
            {
                TryLaunchShortcut(target);
                return;
            }

            // Emulador (ROM)
            if (target.EmulatorId != null)
            {
                var emu = Emulators.FirstOrDefault(e => e.Id == target.EmulatorId);
                if (emu != null) { _launcher.Launch(emu, target); return; }
            }

            // Jogo PC (exe normal)
            _launcher.LaunchGameOnly(target);
        }

        private static bool IsShortcutLike(GameItem g)
        {
            if (g.IsShortcut) return true;

            var fp = (g.FilePath ?? "").Trim();
            if (fp.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)) return true;
            if (fp.StartsWith("com.epicgames.launcher://", StringComparison.OrdinalIgnoreCase)) return true;

            var ext = Path.GetExtension(fp).ToLowerInvariant();
            return ext == ".url" || ext == ".lnk";
        }

        private static void TryLaunchShortcut(GameItem g)
        {
            try
            {
                // Preferência: LaunchUri (quando veio de .url)
                var target = (g.LaunchUri ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(target))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = target,
                        UseShellExecute = true
                    });
                    return;
                }

                // Se não tem LaunchUri, tenta abrir o próprio arquivo (.url/.lnk) ou o FilePath direto
                var fp = (g.FilePath ?? "").Trim();
                if (string.IsNullOrWhiteSpace(fp)) return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = fp,
                    UseShellExecute = true
                });
            }
            catch
            {
                // se quiser, aqui você pode mostrar um MessageBox via DialogService futuramente
            }
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

        // ✅ ADD GAME (agora aceita .exe / .url / .lnk)
        [RelayCommand]
        private async Task AddGame()
        {
            // filtro Win32 (OpenFileDialog)
            var filter =
                "Jogos / Atalhos (*.exe;*.url;*.lnk)|*.exe;*.url;*.lnk|" +
                "Executável (*.exe)|*.exe|" +
                "Atalho da Internet (*.url)|*.url|" +
                "Atalho do Windows (*.lnk)|*.lnk";

            var f = _dialogs.PickFile("Add Jogo", filter);
            if (string.IsNullOrWhiteSpace(f)) return;

            var ext = Path.GetExtension(f).ToLowerInvariant();

            // Atalho Internet (.url)
            if (ext == ".url")
            {
                var uri = TryReadInternetShortcutUrl(f);

                var platform = DetectPlatformFromUri(uri);
                Games.Add(new GameItem
                {
                    Title = Path.GetFileNameWithoutExtension(f),
                    FilePath = f,
                    Plataform = platform,
                    IsShortcut = true,
                    LaunchUri = uri
                });

                await PersistAsync();
                return;
            }

            // Atalho Windows (.lnk) -> abre via shell
            if (ext == ".lnk")
            {
                Games.Add(new GameItem
                {
                    Title = Path.GetFileNameWithoutExtension(f),
                    FilePath = f,
                    Plataform = "Atalho",
                    IsShortcut = true,
                    LaunchUri = null
                });

                await PersistAsync();
                return;
            }

            // Executável normal
            Games.Add(new GameItem
            {
                Title = Path.GetFileNameWithoutExtension(f),
                FilePath = f,
                Plataform = "PC",
                IsShortcut = false,
                LaunchUri = null
            });

            await PersistAsync();
        }

        private static string? TryReadInternetShortcutUrl(string filePath)
        {
            try
            {
                // Formato típico:
                // [InternetShortcut]
                // URL=steam://rungameid/123...
                foreach (var line in File.ReadAllLines(filePath))
                {
                    var l = (line ?? "").Trim();
                    if (l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
                    {
                        var url = l.Substring(4).Trim();
                        return string.IsNullOrWhiteSpace(url) ? null : url;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string DetectPlatformFromUri(string? uri)
        {
            var u = (uri ?? "").Trim();

            if (u.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
                return "Steam";

            if (u.StartsWith("com.epicgames.launcher://", StringComparison.OrdinalIgnoreCase))
                return "Epic";

            if (u.Contains("epicgames.com", StringComparison.OrdinalIgnoreCase))
                return "Epic";

            if (!string.IsNullOrWhiteSpace(u))
                return "Atalho";

            return "Atalho";
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
