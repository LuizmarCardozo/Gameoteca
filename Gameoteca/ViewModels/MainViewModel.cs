using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gameoteca.Models;
using Gameoteca.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;

namespace Gameoteca.ViewModels
{
    // SIMULADOR DE TECLADO
    public static class KeyboardSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public const byte VK_LEFT = 0x25;
        public const byte VK_UP = 0x26;
        public const byte VK_RIGHT = 0x27;
        public const byte VK_DOWN = 0x28;
        public const byte VK_RETURN = 0x0D;
        public const byte VK_ESCAPE = 0x1B;

        public static void PressKey(byte keyCode)
        {
            keybd_event(keyCode, 0, 0, UIntPtr.Zero);
            keybd_event(keyCode, 0, 2, UIntPtr.Zero);
        }
    }

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
        [ObservableProperty] private int _selectedTabIndex;

        // Propriedade para inicialização com o Windows
        [ObservableProperty] private bool _runOnStartup;

        public static bool IsContextMenuOpen { get; set; } = false;

        public event EventHandler? OpenContextMenuRequested;
        public ControllerConfig ControllerSettings { get; set; } = ControllerConfig.Load();

        private const int GridColumns = 5;
        private bool _isAxisXActive;
        private bool _isAxisYActive;

        public MainViewModel(JoystickService joystickService)
        {
            _joystickService = joystickService;

            _joystickService.ButtonPressed += OnJoystickButtonPressed;
            _joystickService.AxisChanged += OnJoystickAxisChanged;
            _joystickService.DPadChanged += OnDPadChanged;

            Emulators.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AvailablePlatforms));

            CheckStartupState();
            _ = InitAsync();
        }

        private void CheckStartupState()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                if (key != null)
                {
                    _runOnStartup = key.GetValue("Gameoteca") != null;
                }
            }
            catch { }
        }

        partial void OnRunOnStartupChanged(bool value)
        {
            SetStartup(value);
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                    {
                        string? path = Environment.ProcessPath;
                        if (path != null) key.SetValue("Gameoteca", $"\"{path}\"");
                    }
                    else
                    {
                        key.DeleteValue("Gameoteca", false);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogs.ShowError($"Erro ao configurar inicialização: {ex.Message}", "Erro");
            }
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

            // ✅ OTIMIZAÇÃO: Limpa a RAM após carregar tudo
            ReduceMemoryUsage();
        }

        // ✅ MÉTODO DE OTIMIZAÇÃO DE MEMÓRIA
        private void ReduceMemoryUsage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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

        [RelayCommand]
        private void OpenControllerMapping()
        {
            _joystickService.ButtonPressed -= OnJoystickButtonPressed;
            _joystickService.AxisChanged -= OnJoystickAxisChanged;
            _joystickService.DPadChanged -= OnDPadChanged;

            var mapWindow = new Views.ControllerMappingWindow(_joystickService, ControllerSettings);
            if (mapWindow.ShowDialog() == true)
            {
                ControllerSettings = ControllerConfig.Load();
            }

            // ✅ OTIMIZAÇÃO: Limpa rastros de memória da janela recém-fechada
            ReduceMemoryUsage();

            _joystickService.ButtonPressed += OnJoystickButtonPressed;
            _joystickService.AxisChanged += OnJoystickAxisChanged;
            _joystickService.DPadChanged += OnDPadChanged;
        }

        private void OnJoystickButtonPressed(object? sender, int button)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (button == ControllerSettings.ButtonPlay)
                {
                    KeyboardSimulator.PressKey(KeyboardSimulator.VK_RETURN);
                }
                else if (button == ControllerSettings.ButtonBack)
                {
                    KeyboardSimulator.PressKey(KeyboardSimulator.VK_ESCAPE);
                }
                else if (button == ControllerSettings.ButtonAdd)
                {
                    if (AddGameCommand.CanExecute(null)) AddGameCommand.Execute(null);
                }
                else if (button == ControllerSettings.ButtonOptions)
                {
                    OpenContextMenuRequested?.Invoke(this, EventArgs.Empty);
                }
            });
        }

        private void OnDPadChanged(object? sender, DPadDirection direction)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (direction == DPadDirection.Up) KeyboardSimulator.PressKey(KeyboardSimulator.VK_UP);
                else if (direction == DPadDirection.Down) KeyboardSimulator.PressKey(KeyboardSimulator.VK_DOWN);
                else if (direction == DPadDirection.Left) KeyboardSimulator.PressKey(KeyboardSimulator.VK_LEFT);
                else if (direction == DPadDirection.Right) KeyboardSimulator.PressKey(KeyboardSimulator.VK_RIGHT);
            });
        }

        private void OnJoystickAxisChanged(object? sender, JoystickAxisEventArgs e)
        {
            const int deadZone = 15000;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Axis == AxisType.X)
                {
                    if (e.Value < -deadZone && !_isAxisXActive)
                    {
                        _isAxisXActive = true;
                        KeyboardSimulator.PressKey(KeyboardSimulator.VK_LEFT);
                    }
                    else if (e.Value > deadZone && !_isAxisXActive)
                    {
                        _isAxisXActive = true;
                        KeyboardSimulator.PressKey(KeyboardSimulator.VK_RIGHT);
                    }
                    else if (e.Value > -deadZone && e.Value < deadZone)
                    {
                        _isAxisXActive = false;
                    }
                }
                else if (e.Axis == AxisType.Y)
                {
                    if (e.Value < -deadZone && !_isAxisYActive)
                    {
                        _isAxisYActive = true;
                        KeyboardSimulator.PressKey(KeyboardSimulator.VK_UP);
                    }
                    else if (e.Value > deadZone && !_isAxisYActive)
                    {
                        _isAxisYActive = true;
                        KeyboardSimulator.PressKey(KeyboardSimulator.VK_DOWN);
                    }
                    else if (e.Value > -deadZone && e.Value < deadZone)
                    {
                        _isAxisYActive = false;
                    }
                }
            });
        }

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
            foreach (var g in Games.Where(g => g.EmulatorId == target.Id)) g.Plataform = newName;
            foreach (var m in Mappings.Where(m => m.EmulatorId == target.Id)) m.Plataform = newName;
            await PersistAsync();
        }

        [RelayCommand]
        private void PlaySelected(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;
            if (IsShortcutLike(target)) { TryLaunchShortcut(target); return; }
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
            var filter = "Jogos / Atalhos (*.exe;*.url;*.lnk)|*.exe;*.url;*.lnk|Executável (*.exe)|*.exe|Atalho da Internet (*.url)|*.url|Atalho do Windows (*.lnk)|*.lnk";
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
                var newMap = new FolderMapping { FolderPath = f, Plataform = "PC", ExtensionsText = ".exe; .lnk; .url" };
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
                if (img != null) { t.ImagePath = img; await PersistAsync(); }
            }
        }

        [RelayCommand]
        private async Task ResetAllSettings()
        {
            var confirm = _dialogs.AskConfirmation("Resetar Configurações", "Tem certeza que deseja apagar TODOS os jogos, emuladores e configurações?\n\nEsta ação não pode ser desfeita!");
            if (!confirm) return;

            try
            {
                Games.Clear();
                foreach (var m in Mappings) m.PropertyChanged -= Mapping_PropertyChanged;
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