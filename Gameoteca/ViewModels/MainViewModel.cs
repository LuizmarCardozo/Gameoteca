using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gameoteca.Models;
using Gameoteca.Services;
using System;
using System.Collections.Generic;
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
        private readonly JoystickService _joystickService;

        public ObservableCollection<Emulator> Emulators { get; } = new();
        public ObservableCollection<GameItem> Games { get; } = new();
        public ObservableCollection<FolderMapping> Mappings { get; } = new();

        public ObservableCollection<string> AvailableExtensions { get; } = new()
        {
            ".zip", ".7z", ".iso", ".bin", ".cue", ".smc", ".sfc", ".n64", ".z64", ".gba", ".nds", ".exe", ".lnk", ".url"
        };

        [ObservableProperty] private GameItem? _selectedGame;
        [ObservableProperty] private Emulator? _selectedEmulator;
        [ObservableProperty] private FolderMapping? _selectedMapping;

        // Novo: número de colunas da grade (valor fixo por enquanto)
        private const int GridColumns = 5;

        public MainViewModel(JoystickService joystickService)
        {
            _joystickService = joystickService;

            // Assinar eventos do joystick
            _joystickService.ButtonPressed += OnJoystickButtonPressed;
            _joystickService.AxisChanged += OnJoystickAxisChanged;

            Emulators.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AvailablePlatforms));

            // Carregar dados
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            var state = await _storage.LoadAsync();
            Emulators.Clear(); foreach (var e in state.Emulators) Emulators.Add(e);
            Games.Clear(); foreach (var g in state.Games) Games.Add(g);
            Mappings.Clear();

            foreach (var m in state.Mappings)
            {
                m.PropertyChanged += Mapping_PropertyChanged;
                Mappings.Add(m);
            }
        }

        public IEnumerable<PlatformOption> AvailablePlatforms
        {
            get
            {
                yield return new PlatformOption { Id = null, Name = "PC" };
                foreach (var emu in Emulators)
                {
                    yield return new PlatformOption { Id = emu.Id, Name = emu.Name };
                }
            }
        }

        // Event handlers do joystick
        private void OnJoystickButtonPressed(object? sender, int button)
        {
            // Mapeamento de botões (ajuste conforme seu joystick)
            switch (button)
            {
                case 0: // Botão A (geralmente confirma)
                    PlaySelectedCommand.Execute(null);
                    break;
                case 1: // Botão B (geralmente cancela)
                    // Talvez voltar? Sem função por enquanto.
                    break;
                case 2: // Botão X
                    AddGameCommand.Execute(null);
                    break;
                case 3: // Botão Y
                    // Pode ser usado para abrir configurações futuramente
                    break;
                case 4: // Botão LB
                    // Mudar de aba? Implementar depois.
                    break;
                case 5: // Botão RB
                    // Mudar de aba? Implementar depois.
                    break;
                case 6: // Botão Back
                    // Abrir menu de contexto? Ou reset?
                    break;
                case 7: // Botão Start
                    // Play também, ou algo similar
                    PlaySelectedCommand.Execute(null);
                    break;
                default:
                    break;
            }
        }

        private void OnJoystickAxisChanged(object? sender, JoystickAxisEventArgs e)
        {
            const int deadZone = 3000; // Zona morta para evitar movimentos acidentais

            if (e.Axis == AxisType.X)
            {
                if (e.Value < -deadZone)
                    MoveSelectionLeft();
                else if (e.Value > deadZone)
                    MoveSelectionRight();
            }
            else if (e.Axis == AxisType.Y)
            {
                if (e.Value < -deadZone)
                    MoveSelectionUp();
                else if (e.Value > deadZone)
                    MoveSelectionDown();
            }
        }

        // Métodos de navegação
        private void MoveSelectionUp()
        {
            if (Games.Count == 0) return;

            int currentIndex = Games.IndexOf(SelectedGame);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = currentIndex - GridColumns;
            if (newIndex >= 0)
                SelectedGame = Games[newIndex];
        }

        private void MoveSelectionDown()
        {
            if (Games.Count == 0) return;

            int currentIndex = Games.IndexOf(SelectedGame);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = currentIndex + GridColumns;
            if (newIndex < Games.Count)
                SelectedGame = Games[newIndex];
        }

        private void MoveSelectionLeft()
        {
            if (Games.Count == 0) return;

            int currentIndex = Games.IndexOf(SelectedGame);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = currentIndex - 1;
            if (newIndex >= 0 && (newIndex / GridColumns) == (currentIndex / GridColumns))
                SelectedGame = Games[newIndex];
        }

        private void MoveSelectionRight()
        {
            if (Games.Count == 0) return;

            int currentIndex = Games.IndexOf(SelectedGame);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = currentIndex + 1;
            if (newIndex < Games.Count && (newIndex / GridColumns) == (currentIndex / GridColumns))
                SelectedGame = Games[newIndex];
        }

        // Métodos existentes (mantidos)
        private void Mapping_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderMapping.EmulatorId))
            {
                if (sender is FolderMapping map)
                {
                    if (map.EmulatorId == null)
                    {
                        map.Plataform = "PC";
                        map.ExtensionsText = ".exe; .lnk; .url";
                    }
                    else
                    {
                        var emu = Emulators.FirstOrDefault(x => x.Id == map.EmulatorId);
                        map.Plataform = emu?.Name ?? "Desconhecido";
                    }
                    _ = PersistAsync();
                }
            }
        }

        private async Task PersistAsync() => await _storage.SaveAsync(new LibraryState
        {
            Games = Games.ToList(),
            Emulators = Emulators.ToList(),
            Mappings = Mappings.ToList()
        });

        [RelayCommand]
        private async Task ClearGameEmulator(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            target.EmulatorId = null;
            target.Plataform = "PC";
            await PersistAsync();
        }

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
            if (t != null)
            {
                t.PropertyChanged -= Mapping_PropertyChanged;
                Mappings.Remove(t);
            }
            await PersistAsync();
        }

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

            foreach (var m in Mappings.Where(m => m.EmulatorId == target.Id))
                m.Plataform = newName;

            await PersistAsync();
        }

        [RelayCommand]
        private void PlaySelected(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;

            if (IsShortcutLike(target))
            {
                TryLaunchShortcut(target);
                return;
            }

            if (target.EmulatorId != null)
            {
                var emu = Emulators.FirstOrDefault(e => e.Id == target.EmulatorId);
                if (emu != null) { _launcher.Launch(emu, target); return; }
            }

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
                var target = (g.LaunchUri ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(target))
                {
                    Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
                    return;
                }

                var fp = (g.FilePath ?? "").Trim();
                if (string.IsNullOrWhiteSpace(fp)) return;

                Process.Start(new ProcessStartInfo { FileName = fp, UseShellExecute = true });
            }
            catch { }
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
            var filter =
                "Jogos / Atalhos (*.exe;*.url;*.lnk)|*.exe;*.url;*.lnk|" +
                "Executável (*.exe)|*.exe|" +
                "Atalho da Internet (*.url)|*.url|" +
                "Atalho do Windows (*.lnk)|*.lnk";

            var f = _dialogs.PickFile("Add Jogo", filter);
            if (string.IsNullOrWhiteSpace(f)) return;

            var ext = Path.GetExtension(f).ToLowerInvariant();

            if (ext == ".url")
            {
                var uri = TryReadInternetShortcutUrl(f);
                var platform = DetectPlatformFromUri(uri);
                Games.Add(new GameItem { Title = Path.GetFileNameWithoutExtension(f), FilePath = f, Plataform = platform, IsShortcut = true, LaunchUri = uri });
                await PersistAsync();
                return;
            }

            if (ext == ".lnk")
            {
                Games.Add(new GameItem { Title = Path.GetFileNameWithoutExtension(f), FilePath = f, Plataform = "Atalho", IsShortcut = true, LaunchUri = null });
                await PersistAsync();
                return;
            }

            Games.Add(new GameItem { Title = Path.GetFileNameWithoutExtension(f), FilePath = f, Plataform = "PC", IsShortcut = false, LaunchUri = null });
            await PersistAsync();
        }

        private static string? TryReadInternetShortcutUrl(string filePath)
        {
            try
            {
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
            if (u.StartsWith("steam://", StringComparison.OrdinalIgnoreCase)) return "Steam";
            if (u.StartsWith("com.epicgames.launcher://", StringComparison.OrdinalIgnoreCase)) return "Epic";
            if (u.Contains("epicgames.com", StringComparison.OrdinalIgnoreCase)) return "Epic";
            if (!string.IsNullOrWhiteSpace(u)) return "Atalho";
            return "Atalho";
        }

        [RelayCommand]
        private async Task AddEmulator()
        {
            var f = _dialogs.PickFile("Add Emulador", "*.exe|*.exe");
            if (f != null)
            {
                Emulators.Add(new Emulator { Name = Path.GetFileNameWithoutExtension(f), ExecutablePath = f });
                await PersistAsync();
            }
        }

        [RelayCommand]
        private async Task AddMapping()
        {
            var f = _dialogs.PickFolder("Pasta ROMs");
            if (f != null)
            {
                var newMap = new FolderMapping
                {
                    FolderPath = f,
                    Plataform = "PC",
                    ExtensionsText = ".exe; .lnk; .url"
                };
                newMap.PropertyChanged += Mapping_PropertyChanged;
                Mappings.Add(newMap);
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

        [RelayCommand]
        private async Task ResetAllSettings()
        {
            var confirm = _dialogs.AskConfirmation(
                "Resetar Configurações",
                "Tem certeza que deseja apagar TODOS os jogos, emuladores e configurações?\n\nEsta ação não pode ser desfeita!"
            );

            if (!confirm) return;

            try
            {
                Games.Clear();

                foreach (var m in Mappings)
                {
                    m.PropertyChanged -= Mapping_PropertyChanged;
                }
                Mappings.Clear();

                Emulators.Clear();

                await _storage.ResetAsync();

                _dialogs.ShowMessage("Configurações resetadas com sucesso!", "Reset Concluído");
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Erro ao resetar configurações: {ex.Message}", "Erro");
            }
        }
    }

    public class PlatformOption
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = "";
    }
}