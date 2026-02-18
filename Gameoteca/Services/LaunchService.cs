using Gameoteca.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows; // Necessário para MessageBox

namespace Gameoteca.Services
{
    public class LaunchService
    {
        // 1. Lança Jogos de Emulador
        public void Launch(Emulator emulator, GameItem game)
        {
            // Validações antes de tentar abrir
            if (!File.Exists(emulator.ExecutablePath))
            {
                MessageBox.Show($"O executável do emulador não foi encontrado:\n{emulator.ExecutablePath}",
                                "Erro de Arquivo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(game.FilePath))
            {
                MessageBox.Show($"O arquivo do jogo/ROM não foi encontrado:\n{game.FilePath}",
                                "Erro de Arquivo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Substitui o coringa {rom} pelo caminho do jogo entre aspas
                var args = (emulator.ArgsTemplate ?? "\"{rom}\"")
                           .Replace("{rom}", $"\"{game.FilePath}\"");

                var psi = new ProcessStartInfo
                {
                    FileName = emulator.ExecutablePath,
                    Arguments = args,
                    UseShellExecute = false // Necessário false para passar argumentos complexos para emuladores
                };

                // Define a pasta de trabalho (importante para alguns emuladores carregarem configs)
                if (!string.IsNullOrWhiteSpace(emulator.WorkingDirectory))
                {
                    psi.WorkingDirectory = emulator.WorkingDirectory;
                }
                else
                {
                    // Se não tiver definido, usa a pasta do próprio executável do emulador
                    psi.WorkingDirectory = Path.GetDirectoryName(emulator.ExecutablePath);
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar o emulador:\n{ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 2. Lança Jogos de PC (Sem emulador)
        // Essa é a adição necessária para o botão funcionar com jogos nativos
        public void LaunchGameOnly(GameItem game)
        {
            if (!File.Exists(game.FilePath))
            {
                MessageBox.Show($"O executável do jogo não foi encontrado:\n{game.FilePath}",
                                "Erro de Arquivo", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var workingDir = Path.GetDirectoryName(game.FilePath);

                var psi = new ProcessStartInfo
                {
                    FileName = game.FilePath,
                    UseShellExecute = true, // Importante para jogos de PC (lida com atalhos, steam, permissões)
                    WorkingDirectory = workingDir
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar o jogo:\n{ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}