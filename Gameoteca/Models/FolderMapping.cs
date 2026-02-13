using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gameoteca.Models
{
    public class FolderMapping
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FolderPath { get; set; } = "";

        public string? Plataform {  get; set; }

        public Guid? EmulatorId { get; set; }


        //Formatos de arquivos para emuladores.
        public List<string> Extensions { get; set; } = new();

    }
}
