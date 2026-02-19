using System.Collections.Generic;

namespace Gameoteca.Models
{
    public class LibraryState
    {
        public List<Emulator> Emulators { get; set; } = new();
        public List<GameItem> Games { get; set; } = new();
        public List<FolderMapping> Mappings { get; set; } = new();
    }
}
