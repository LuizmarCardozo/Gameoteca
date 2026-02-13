using Gameoteca.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameoteca.Services
{
    public class LaunchService
    {
        public void Launch(Emulator emulator, GameItem game)
        {
            if (!File.Exists(emulator.ExecutablePath))
                throw new FileNotFoundException("Emulador não encontrado.", emulator.ExecutablePath);

            if (!File.Exists(game.FilePath))
                throw new FileNotFoundException("Jogo/ROM não encontrado.", game.FilePath);

            var romQuoted = $"\"{game.FilePath}\"";
            var args = (emulator.ArgsTemplate ?? "\"{rom}\"")
                .Replace("{rom}", game.FilePath)
                .Replace("\"{rom}\"", romQuoted);

            var psi = new ProcessStartInfo
            {
                FileName = emulator.ExecutablePath,
                Arguments = args,
                UseShellExecute = false
            };

            if (!string.IsNullOrWhiteSpace(emulator.WorkingDirectory))
                psi.WorkingDirectory = emulator.WorkingDirectory;

            Process.Start(psi);
        }
    }
}
