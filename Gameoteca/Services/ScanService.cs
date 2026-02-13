using Gameoteca.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Gameoteca.Services
{
    public class ScanService
    {
        public IEnumerable<GameItem> Scan(FolderMapping mapping)
        {
            if (!Directory.Exists(mapping.FolderPath))
                yield break;

            var extSet = new HashSet<string>(
                mapping.Extensions.Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
            );

            foreach (var file in Directory.EnumerateFiles(mapping.FolderPath, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!extSet.Contains(ext)) continue;

                yield return new GameItem
                {
                    Title = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    Plataform = mapping.Plataform,
                    EmulatorId = mapping.EmulatorId
                };
            }
        }

    }
}
