using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gameoteca.Models;
using Gameoteca.Services;
using System.Collections.ObjectModel;
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

        public ObservableCollection<Emulator> Emulators { get; } = new();
        public ObservableCollection<GameItem> Games { get; } = new();

        [ObservableProperty] private GameItem? _selectedGame;

        public async Task InitAsync()
        {
            var state = await _storage.LoadAsync();
            Emulators.Clear(); foreach (var e in state.Emulators) Emulators.Add(e);
            Games.Clear(); foreach (var g in state.Games) Games.Add(g);
        }

        [RelayCommand]
        private async Task AddGame()
        {
            var file = _dialogs.PickFile("Adicionar Jogo", "Executável (*.exe)|*.exe");
            if (file == null) return;
            var ng = new GameItem { Title = Path.GetFileNameWithoutExtension(file), FilePath = file, Plataform = "PC" };
            Games.Add(ng);
            await PersistAsync();
        }

        [RelayCommand]
        private async Task SetGameEmulator(Emulator emu)
        {
            if (SelectedGame == null || emu == null) return;
            SelectedGame.EmulatorId = emu.Id;
            SelectedGame.Plataform = emu.Name;
            await PersistAsync();
        }

        [RelayCommand]
        private async Task ClearGameEmulator()
        {
            if (SelectedGame == null) return;
            SelectedGame.EmulatorId = null;
            SelectedGame.Plataform = "PC";
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

        [RelayCommand] private async Task Save() => await PersistAsync();
        private async Task PersistAsync() => await _storage.SaveAsync(new LibraryState { Games = Games.ToList(), Emulators = Emulators.ToList() });

        [RelayCommand]
        private async Task ChangeGameImage(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target == null) return;
            var img = _dialogs.PickFile("Selecionar Capa", "Imagens|*.jpg;*.png;*.webp");
            if (img != null) { target.ImagePath = img; await PersistAsync(); }
        }

        [RelayCommand]
        private void RemoveGame(GameItem? item)
        {
            var target = item ?? SelectedGame;
            if (target != null) Games.Remove(target);
        }
    }
}