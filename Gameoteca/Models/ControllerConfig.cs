using System;
using System.Diagnostics;
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

        private const string FileName = "controller_config.json";

        // Reaproveitamos as opções em vez de criar uma nova a cada Save() (é mais barato,
        // pois o System.Text.Json faz cache de metadados por instância de opções).
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        // Por que %AppData%? O caminho antigo era relativo ("controller_config.json"), ou seja,
        // dependia da "pasta de trabalho atual" do processo. Quando o app abre junto com o Windows
        // (opção RunOnStartup), essa pasta costuma ser C:\Windows\System32, onde o app não tem
        // permissão de escrita: o mapeamento não salvava ou "sumia" dependendo de como o app abriu.
        // %AppData%\Gameoteca é a mesma pasta já usada pelo library.json e é sempre gravável.
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Gameoteca");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, FileName);

        public static ControllerConfig Load()
        {
            MigrateLegacyFile();

            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ControllerConfig>(json) ?? new ControllerConfig();
                }
                catch (Exception ex)
                {
                    // Arquivo ilegível/corrompido: usamos os valores padrão em vez de derrubar o app.
                    Debug.WriteLine($"[ControllerConfig] Falha ao ler config: {ex.Message}");
                }
            }
            return new ControllerConfig();
        }

        /// <summary>
        /// Grava a configuração. Devolve false se não conseguiu, para a interface poder avisar
        /// o usuário (antes a falha era silenciosa ou derrubava o app).
        /// </summary>
        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = JsonSerializer.Serialize(this, JsonOpts);

                // Escrita em arquivo temporário + troca: se o PC desligar no meio da gravação,
                // o arquivo antigo continua íntegro em vez de ficar pela metade.
                var tempPath = ConfigPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, ConfigPath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ControllerConfig] Falha ao salvar config: {ex.Message}");
                return false;
            }
        }

        // Quem já tinha um mapeamento salvo no formato antigo não perde nada:
        // copiamos o arquivo antigo para o novo local na primeira vez. É uma CÓPIA (o original
        // fica onde estava), então é seguro mesmo se algo der errado.
        private static void MigrateLegacyFile()
        {
            try
            {
                if (File.Exists(ConfigPath)) return; // já migrado (ou já salvo no local novo)

                // Os dois lugares onde o arquivo antigo pode ter sido gravado:
                // a pasta de trabalho do momento e a pasta do .exe.
                var candidates = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), FileName),
                    Path.Combine(AppContext.BaseDirectory, FileName)
                };

                foreach (var legacyPath in candidates)
                {
                    if (!File.Exists(legacyPath)) continue;

                    Directory.CreateDirectory(ConfigDir);
                    File.Copy(legacyPath, ConfigPath, overwrite: false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ControllerConfig] Falha ao migrar config antiga: {ex.Message}");
            }
        }
    }
}