using System;
using System.Threading.Tasks;
using System.Windows;

namespace Gameoteca
{
    public partial class App : Application
    {
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

                    // Cria e exibe a MainWindow UMA ÚNICA VEZ
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                });
            });
        }
    }
}