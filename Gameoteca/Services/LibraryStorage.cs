using Gameoteca.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gameoteca.Services
{
    public class LibraryStorage
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true
        };

        private readonly string _filePath;

        public LibraryStorage()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Gameoteca"
            );
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "library.json");
        }

        public async Task<LibraryState> LoadAsync()
        {
            if (!File.Exists(_filePath))
                return new LibraryState();

            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<LibraryState>(json, JsonOpts) ?? new LibraryState();
        }

        public async Task SaveAsync(LibraryState state)
        {
            var json = JsonSerializer.Serialize(state, JsonOpts);
            await File.WriteAllTextAsync(_filePath, json);
        }

    }
}
