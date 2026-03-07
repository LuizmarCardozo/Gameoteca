using Gameoteca.Models;
using Gameoteca.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Gameoteca.Views
{
    public partial class ControllerMappingWindow : Window
    {
        private readonly JoystickService _joystick;
        private readonly ControllerConfig _config;
        private Button? _listeningButton = null;

        public ControllerMappingWindow(JoystickService joystick, ControllerConfig config)
        {
            InitializeComponent();
            _joystick = joystick;
            _config = config;

            BtnPlay.Content = $"Botão {_config.ButtonPlay}";
            BtnBack.Content = $"Botão {_config.ButtonBack}";
            BtnAdd.Content = $"Botão {_config.ButtonAdd}";
            BtnOptions.Content = $"Botão {_config.ButtonOptions}";

            _joystick.ButtonPressed += OnJoystickButtonPressed;
        }

        private void MapBtn_Click(object sender, RoutedEventArgs e)
        {
            var defaultColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B2A86"));
            BtnPlay.Background = defaultColor;
            BtnBack.Background = defaultColor;
            BtnAdd.Background = defaultColor;
            BtnOptions.Background = defaultColor;

            _listeningButton = sender as Button;
            if (_listeningButton != null)
            {
                _listeningButton.Content = "Aperte...";
                _listeningButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A855F7"));
                TxtStatus.Text = "Pressione um botão no seu controle...";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A855F7"));
            }
        }

        private void OnJoystickButtonPressed(object? sender, int buttonId)
        {
            if (_listeningButton == null) return;

            Dispatcher.Invoke(() =>
            {
                if (_listeningButton == BtnPlay) _config.ButtonPlay = buttonId;
                else if (_listeningButton == BtnBack) _config.ButtonBack = buttonId;
                else if (_listeningButton == BtnAdd) _config.ButtonAdd = buttonId;
                else if (_listeningButton == BtnOptions) _config.ButtonOptions = buttonId;

                _listeningButton.Content = $"Botão {buttonId}";
                _listeningButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B2A86"));
                _listeningButton = null;

                TxtStatus.Text = "Mapeado com sucesso! Mapeie outro ou clique em Salvar.";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            });
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _config.Save();
            _joystick.ButtonPressed -= OnJoystickButtonPressed;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _joystick.ButtonPressed -= OnJoystickButtonPressed;
            DialogResult = false;
        }
    }
}