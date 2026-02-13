using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameoteca.Models
{
    public class GameItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = ""; //Pasta de jogos e Roms

        public string? Plataform { get; set; } = ""; // Pasta de Emuladores


        public Guid? EmulatorId { get; set; }

    }
}
