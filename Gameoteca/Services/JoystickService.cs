using SharpDX.DirectInput;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Gameoteca.Services
{
    public class JoystickService : IDisposable
    {
        // Intervalos dos timers (em milissegundos).
        // 20ms = leitura fluida enquanto você usa a Gameoteca; 150ms = "modo descanso" para poupar CPU.
        private const int ActivePollMs = 20;
        private const int IdlePollMs = 150;

        // De quanto em quanto tempo procuramos um controle quando nenhum está conectado.
        // 2s é rápido o bastante para o usuário nem perceber e barato o bastante para não pesar
        // (enumerar dispositivos do DirectInput é mais caro do que ler o estado de um já aberto).
        private const int ReconnectCheckMs = 2000;

        private readonly DirectInput _directInput;
        private Joystick? _joystick;

        // Dois DispatcherTimers: um lê o controle (só roda enquanto há controle conectado)
        // e outro fica "vigiando" a chegada de um controle novo.
        // DispatcherTimer dispara na thread da UI, então os eventos abaixo já chegam prontos para
        // mexer na interface, sem precisar de Dispatcher.Invoke.
        private readonly DispatcherTimer _pollTimer;
        private readonly DispatcherTimer _reconnectTimer;

        private JoystickState _previousState = new JoystickState();
        private DPadDirection _previousDPad = DPadDirection.None;
        private bool _isRunning;
        private bool _disposed;

        public event EventHandler<int>? ButtonPressed;
        public event EventHandler<int>? ButtonReleased;
        public event EventHandler<JoystickAxisEventArgs>? AxisChanged;
        public event EventHandler<DPadDirection>? DPadChanged;

        /// <summary>True enquanto existe um controle conectado e sendo lido.</summary>
        public bool IsConnected => _joystick != null;

        // ------------------------------------------------------------------
        // Detecção de "a Gameoteca está em primeiro plano?"
        // ------------------------------------------------------------------
        // Por que P/Invoke e não Window.IsActive? Porque IsActive só enxerga janelas WPF.
        // Diálogos nativos (OpenFileDialog, seletor de pastas) pertencem ao nosso processo, mas
        // NÃO são janelas WPF: com IsActive o controle pararia de funcionar neles.
        // Comparando o ID do processo da janela em primeiro plano com o nosso, cobrimos tudo
        // que é da Gameoteca e bloqueamos o que é de fora (jogos, emuladores, outros apps).
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        /// <summary>
        /// True quando a janela em primeiro plano no Windows é da própria Gameoteca.
        /// Serve para impedir que o controle "digite" teclas dentro de um jogo.
        /// </summary>
        public static bool IsAppInForeground
        {
            get
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return false; // sem janela em foco (ex.: troca de janela)

                GetWindowThreadProcessId(hwnd, out uint processId);
                return processId == (uint)Environment.ProcessId;
            }
        }

        public JoystickService()
        {
            _directInput = new DirectInput();

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ActivePollMs) };
            _pollTimer.Tick += PollJoystick;

            _reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ReconnectCheckMs) };
            _reconnectTimer.Tick += OnReconnectTick;
        }

        /// <summary>
        /// Liga o serviço. Se não houver controle agora, tudo bem: o timer de reconexão
        /// continua procurando e o controle passa a funcionar assim que for plugado.
        /// </summary>
        public void Start()
        {
            if (_isRunning || _disposed) return;
            _isRunning = true;

            TryConnect();
            _reconnectTimer.Start();
        }

        /// <summary>Desliga o serviço por completo (leitura + procura por controles).</summary>
        public void Stop()
        {
            _isRunning = false;
            _reconnectTimer.Stop();
            Disconnect();
        }

        // Tenta abrir o primeiro controle disponível. Devolve true se há um controle ativo.
        private bool TryConnect()
        {
            if (_joystick != null) return true;

            try
            {
                var deviceInstance = _directInput
                    .GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly)
                    .FirstOrDefault();

                if (deviceInstance == null) return false;

                var joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
                try
                {
                    joystick.Properties.BufferSize = 128;
                    joystick.Acquire();
                    _previousState = joystick.GetCurrentState();
                }
                catch
                {
                    // Se algo falhou depois de criar o objeto, precisamos liberá-lo aqui;
                    // senão ele fica "vazando" (objeto COM nativo) a cada tentativa de 2s.
                    joystick.Dispose();
                    throw;
                }

                _joystick = joystick;
                _previousDPad = DPadDirection.None;

                _pollTimer.Interval = TimeSpan.FromMilliseconds(ActivePollMs);
                _pollTimer.Start();
                return true;
            }
            catch (Exception ex)
            {
                // Falhar aqui é normal (sem controle, driver ocupado...). Só registramos e
                // tentamos de novo no próximo ciclo do timer de reconexão.
                Debug.WriteLine($"[Joystick] Falha ao conectar: {ex.Message}");
                return false;
            }
        }

        // Libera o controle atual e para a leitura. NÃO para o timer de reconexão,
        // justamente para que o app note quando o controle voltar.
        private void Disconnect()
        {
            _pollTimer.Stop();

            if (_joystick == null) return;

            // Unacquire + Dispose: o original só fazia Unacquire e depois criava outro Joystick
            // no Start(), deixando o anterior sem liberar.
            try { _joystick.Unacquire(); } catch { /* controle já desapareceu: ok */ }
            try { _joystick.Dispose(); } catch { /* idem */ }

            _joystick = null;
            _previousState = new JoystickState();
            _previousDPad = DPadDirection.None;
        }

        private void OnReconnectTick(object? sender, EventArgs e)
        {
            if (_joystick == null)
                TryConnect();
        }

        private void PollJoystick(object? sender, EventArgs e)
        {
            var joystick = _joystick;
            if (joystick == null) return;

            // Poupa CPU quando a Gameoteca não está em primeiro plano (ex.: você está jogando).
            int targetMs = IsAppInForeground ? ActivePollMs : IdlePollMs;
            if ((int)_pollTimer.Interval.TotalMilliseconds != targetMs)
                _pollTimer.Interval = TimeSpan.FromMilliseconds(targetMs);

            // 1) Leitura do hardware. Se falhar, é porque o controle foi desconectado
            //    (ou o driver o "perdeu"): soltamos tudo e deixamos o timer de reconexão trabalhar.
            JoystickState state;
            try
            {
                joystick.Poll();
                state = joystick.GetCurrentState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Joystick] Controle perdido: {ex.Message}");
                Disconnect();
                return;
            }

            // Guardamos o estado novo ANTES de disparar eventos. Assim, se algum assinante
            // lançar exceção, o mesmo botão não é "re-detectado" a cada 20ms.
            var previous = _previousState;
            _previousState = state;

            // 2) Disparo dos eventos. Uma exceção num assinante (ViewModel, janela...) é um bug de
            //    UI e não motivo para desconectar o controle (o que o código antigo fazia).
            try
            {
                for (int i = 0; i < state.Buttons.Length; i++)
                {
                    if (state.Buttons[i] && !previous.Buttons[i])
                        ButtonPressed?.Invoke(this, i);
                    else if (!state.Buttons[i] && previous.Buttons[i])
                        ButtonReleased?.Invoke(this, i);
                }

                // Eixos vão de 0 a 65535; subtrair 32767 centraliza em zero (negativo = esquerda/cima).
                // O limite de 150 evita disparar evento por "tremida" do analógico.
                int normX = state.X - 32767;
                int prevNormX = previous.X - 32767;
                if (Math.Abs(normX - prevNormX) > 150)
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.X, normX));

                int normY = state.Y - 32767;
                int prevNormY = previous.Y - 32767;
                if (Math.Abs(normY - prevNormY) > 150)
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.Y, normY));

                // D-Pad (POV hat): só avisa quando a direção MUDA.
                int pov = state.PointOfViewControllers[0];
                DPadDirection currentDpad = GetDPadDirection(pov);
                if (currentDpad != _previousDPad)
                {
                    _previousDPad = currentDpad;
                    DPadChanged?.Invoke(this, currentDpad);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Joystick] Erro em um assinante de evento: {ex}");
            }
        }

        private DPadDirection GetDPadDirection(int povValue)
        {
            // O POV vem em centésimos de grau (0 = cima, 9000 = direita...) ou -1 se solto.
            if (povValue == -1) return DPadDirection.None;
            if (povValue >= 31500 || povValue <= 4500) return DPadDirection.Up;
            if (povValue > 4500 && povValue < 13500) return DPadDirection.Right;
            if (povValue >= 13500 && povValue <= 22500) return DPadDirection.Down;
            if (povValue > 22500 && povValue < 31500) return DPadDirection.Left;
            return DPadDirection.None;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();
            _pollTimer.Tick -= PollJoystick;
            _reconnectTimer.Tick -= OnReconnectTick;
            _directInput.Dispose();
        }
    }

    public enum AxisType { X, Y, Z, Rx, Ry, Rz }
    public enum DPadDirection { None, Up, Right, Down, Left }
    public class JoystickAxisEventArgs : EventArgs
    {
        public AxisType Axis { get; }
        public int Value { get; }
        public JoystickAxisEventArgs(AxisType axis, int value) { Axis = axis; Value = value; }
    }
}