using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameoteca.Models
{
    public class LibraryState
    {
        public List<Emulator> Emulators { get; set; } = new();
        public List<GameItem> Games { get; set; } = new();
        public List <FolderMapping> Mappings { get; set; } = new();


    }
}
