using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameoteca.Models
{
    public class Emulator
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string ExecutablePath { get; set; } = "";

        public string ArgsTemplate { get; set; } = "\"{rom}\"";

        public string? WorkingDirectory { get; set; }

    }
}
