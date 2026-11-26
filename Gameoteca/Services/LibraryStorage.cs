using Gameoteca.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gameoteca.Services
{
    public class LibraryStorage
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // "Porteiro" que deixa só UMA operação de arquivo por vez (ler, salvar ou resetar).
        // Antes, vários `_ = PersistAsync()` disparados em sequência escreviam no mesmo arquivo
        // ao mesmo tempo, o que pode gerar IOException (arquivo em uso) ou arquivo misturado/corrompido.
        // É static para valer mesmo que alguém crie mais de um LibraryStorage.
        private static readonly SemaphoreSlim Gate = new(1, 1);

        // Contador de "pedidos de salvamento". Cada Save/Reset pega um número maior.
        // Quando chega a vez de um salvamento antigo, se já existe um pedido mais novo ele é
        // descartado: o mais novo tem os dados mais atuais, então gravar o velho seria trabalho
        // inútil (e arriscaria sobrescrever dado novo com dado velho se a ordem se embaralhasse).
        private static long _latestRequest;

        private readonly string _dir;
        private readonly string _filePath;
        private readonly string _tempPath;
        private readonly string _backupPath;

        /// <summary>
        /// Preenchido após o LoadAsync quando houve problema ao ler o arquivo principal
        /// (para a interface avisar o usuário). Fica null se tudo correu bem.
        /// </summary>
        public string? LoadWarning { get; private set; }

        public LibraryStorage()
        {
            _dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Gameoteca"
            );
            Directory.CreateDirectory(_dir);

            _filePath = Path.Combine(_dir, "library.json");
            _tempPath = _filePath + ".tmp";   // rascunho da gravação em andamento
            _backupPath = _filePath + ".bak"; // última versão boa conhecida
        }

        public async Task<LibraryState> LoadAsync()
        {
            await Gate.WaitAsync();
            try
            {
                LoadWarning = null;

                // Primeira execução (ou depois de um reset): biblioteca vazia, sem aviso.
                if (!File.Exists(_filePath))
                    return new LibraryState();

                var state = await TryReadAsync(_filePath);
                if (state != null)
                    return state;

                // Chegou aqui: o library.json existe mas não pôde ser lido (corrompido/vazio/bloqueado).
                // Passo 1: guardar uma cópia para você poder recuperar à mão se precisar.
                // Fazemos CÓPIA (não mover) para o arquivo original continuar no lugar.
                var corruptCopy = Path.Combine(_dir, $"library.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                try { File.Copy(_filePath, corruptCopy, overwrite: false); }
                catch (Exception ex) { Debug.WriteLine($"[Storage] Não foi possível guardar cópia do arquivo ruim: {ex.Message}"); }

                // Passo 2: tentar a última versão boa (.bak), criada automaticamente a cada salvamento.
                if (File.Exists(_backupPath))
                {
                    var fromBackup = await TryReadAsync(_backupPath);
                    if (fromBackup != null)
                    {
                        LoadWarning = "O arquivo da biblioteca estava danificado. Carreguei a última cópia de segurança, " +
                                      "então alguns dos seus últimos ajustes podem ter se perdido.\n\n" +
                                      $"Uma cópia do arquivo danificado foi guardada em:\n{corruptCopy}";
                        return fromBackup;
                    }
                }

                // Passo 3: sem backup utilizável. Começamos vazio, mas avisando, e o arquivo
                // ruim fica guardado na cópia acima.
                LoadWarning = "Não consegui ler o arquivo da biblioteca e não havia cópia de segurança válida. " +
                              "A biblioteca começou vazia.\n\n" +
                              $"Uma cópia do arquivo original foi guardada em:\n{corruptCopy}";
                return new LibraryState();
            }
            finally
            {
                Gate.Release();
            }
        }

        public async Task SaveAsync(LibraryState state)
        {
            // Serializamos AGORA, na thread de quem chamou (a UI), antes de esperar o porteiro.
            // Motivo: `state` aponta para objetos vivos (jogos, emuladores...) que a interface continua
            // alterando. Tirando a "foto" em JSON logo de cara, o que vai para o disco é exatamente
            // o estado do momento do pedido, sem risco de ler coleção enquanto ela muda.
            var json = JsonSerializer.Serialize(state, JsonOpts);
            var requestId = Interlocked.Increment(ref _latestRequest);

            await Gate.WaitAsync();
            try
            {
                // Já existe pedido mais novo (ou um Reset) na fila? Então este está obsoleto.
                if (requestId != Interlocked.Read(ref _latestRequest))
                    return;

                // Escrita ATÔMICA: gravamos num arquivo temporário e só depois trocamos pelo definitivo.
                // Se o app fechar/travar ou a luz cair no meio da gravação, o library.json antigo
                // continua intacto (antes, WriteAllTextAsync truncava o arquivo e gravava por cima,
                // deixando-o pela metade se algo interrompesse).
                await File.WriteAllTextAsync(_tempPath, json);

                if (File.Exists(_filePath))
                {
                    // File.Replace troca os arquivos de uma vez só e ainda guarda a versão anterior
                    // como .bak, nossa "última versão boa" usada na recuperação do LoadAsync.
                    File.Replace(_tempPath, _filePath, _backupPath);
                }
                else
                {
                    File.Move(_tempPath, _filePath);
                }
            }
            finally
            {
                Gate.Release();
            }
        }

        // Método para resetar (apagar os arquivos)
        public async Task ResetAsync()
        {
            // Invalida qualquer salvamento que esteja esperando na fila. Sem isso, um salvamento
            // pendente poderia rodar logo depois do reset e "ressuscitar" os dados apagados.
            Interlocked.Increment(ref _latestRequest);

            await Gate.WaitAsync();
            try
            {
                DeleteIfExists(_filePath);
                DeleteIfExists(_tempPath);
                DeleteIfExists(_backupPath); // reset de verdade: não deixa cópia antiga para trás
            }
            finally
            {
                Gate.Release();
            }
            // O "Task.Delay(100)" do código antigo foi removido: File.Delete é síncrono,
            // quando ele retorna o arquivo já foi apagado. Esperar 100ms não garantia nada.
        }

        // Lê e converte o JSON. Devolve null se não conseguir (em vez de lançar exceção),
        // para o chamador decidir o que fazer (tentar o backup, avisar o usuário...).
        private static async Task<LibraryState?> TryReadAsync(string path)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    return JsonSerializer.Deserialize<LibraryState>(json, JsonOpts);
                }
                catch (JsonException ex)
                {
                    // JSON inválido/vazio: tentar de novo não muda nada, então desistimos já.
                    Debug.WriteLine($"[Storage] JSON inválido em '{path}': {ex.Message}");
                    return null;
                }
                catch (IOException) when (attempt < 3)
                {
                    // Arquivo temporariamente em uso (antivírus, sincronizador de nuvem...):
                    // vale esperar um instante e tentar de novo, antes de tratar como "corrompido".
                    await Task.Delay(150);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Storage] Falha ao ler '{path}': {ex.Message}");
                    return null;
                }
            }
            return null;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
