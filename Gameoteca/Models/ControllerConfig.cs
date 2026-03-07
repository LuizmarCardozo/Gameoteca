using System.IO;
using System.Text.Json;

namespace Gameoteca.Models
{
    public class ControllerConfig
    {
        public int ButtonPlay { get; set; } = 0;
        public int ButtonBack { get; set; } = 1;
        public int ButtonAdd { get; set; } = 2;
        public int ButtonOptions { get; set; } = 6;  // Botão para abrir o menu de contexto

        private static readonly string ConfigPath = "controller_config.json";

        public static ControllerConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ControllerConfig>(json) ?? new ControllerConfig();
                }
                catch { }
            }
            return new ControllerConfig();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}