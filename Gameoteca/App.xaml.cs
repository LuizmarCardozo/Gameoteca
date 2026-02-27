using System;
using System.Threading.Tasks;
using System.Windows;
using Gameoteca.Services;
using Gameoteca.ViewModels;

namespace Gameoteca
{
    public partial class App : Application
    {
        // Propriedade estática para acesso global ao serviço de joystick (opcional)
        public static JoystickService? JoystickService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Exibe a splash screen (sem fechamento automático)
            var splash = new SplashScreen("Assets/SPLASH.jpg");
            splash.Show(false);

            // Aguarda 3 segundos e inicia o fade
            Task.Delay(3000).ContinueWith(_ =>
            {
                // Volta para a thread da UI
                Dispatcher.InvokeAsync(async () =>
                {
                    // Inicia o fade out (0,5 segundos)
                    splash.Close(TimeSpan.FromSeconds(0.5));

                    // Aguarda o fade terminar completamente
                    await Task.Delay(500);

                    // Criar o serviço de joystick
                    JoystickService = new JoystickService();

                    // Criar o ViewModel com o serviço
                    var viewModel = new MainViewModel(JoystickService);

                    // Criar a janela principal e definir o DataContext
                    var mainWindow = new MainWindow
                    {
                        DataContext = viewModel
                    };

                    // Exibir a janela
                    mainWindow.Show();

                    // Iniciar o monitoramento do joystick (precisa ser após a janela ser mostrada para o DispatcherTimer funcionar corretamente)
                    JoystickService.Start();
                });
            });
        }
    }
}