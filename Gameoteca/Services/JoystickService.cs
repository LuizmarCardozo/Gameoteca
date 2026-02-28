using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Windows.Threading;

namespace Gameoteca.Services
{
    public class JoystickService : IDisposable
    {
        private readonly DirectInput _directInput;
        private Joystick? _joystick;
        private readonly DispatcherTimer _timer;
        private JoystickState _previousState = new JoystickState();
        private bool _isRunning;
        private DPadDirection _previousDPad = DPadDirection.None;

        // Eventos
        public event EventHandler<int>? ButtonPressed;
        public event EventHandler<int>? ButtonReleased;
        public event EventHandler<JoystickAxisEventArgs>? AxisChanged;
        public event EventHandler<DPadDirection>? DPadChanged; // NOVO: Evento para o D-Pad

        public JoystickService()
        {
            _directInput = new DirectInput();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) }; // Reduzido para respostas mais rápidas
            _timer.Tick += PollJoystick;
        }

        public void Start()
        {
            if (_isRunning) return;

            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            var deviceInstance = devices.FirstOrDefault();

            if (deviceInstance == null) return;

            _joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
            _joystick.Properties.BufferSize = 128;
            _joystick.Acquire();

            _previousState = _joystick.GetCurrentState();
            _timer.Start();
            _isRunning = true;
        }

        public void Stop()
        {
            _timer.Stop();
            _joystick?.Unacquire();
            _isRunning = false;
        }

        private void PollJoystick(object? sender, EventArgs e)
        {
            if (_joystick == null) return;

            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                // 1. Detectar Botões
                for (int i = 0; i < state.Buttons.Length; i++)
                {
                    if (state.Buttons[i] && !_previousState.Buttons[i])
                        ButtonPressed?.Invoke(this, i);
                    else if (!state.Buttons[i] && _previousState.Buttons[i])
                        ButtonReleased?.Invoke(this, i);
                }

                // 2. Detectar Analógicos (Normalizado: Centro é 0, vai de -32767 a +32768)
                int normX = state.X - 32767;
                int prevNormX = _previousState.X - 32767;
                if (Math.Abs(normX - prevNormX) > 100) // Pequeno filtro de ruído
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.X, normX));

                int normY = state.Y - 32767;
                int prevNormY = _previousState.Y - 32767;
                if (Math.Abs(normY - prevNormY) > 100)
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.Y, normY));

                // 3. Detectar D-Pad (Point of View Hat)
                int pov = state.PointOfViewControllers[0];
                DPadDirection currentDpad = GetDPadDirection(pov);

                if (currentDpad != _previousDPad)
                {
                    DPadChanged?.Invoke(this, currentDpad);
                    _previousDPad = currentDpad;
                }

                _previousState = state;
            }
            catch
            {
                Stop();
            }
        }

        // Traduz o ângulo do POV para Direções fáceis de usar
        private DPadDirection GetDPadDirection(int povValue)
        {
            if (povValue == -1) return DPadDirection.None;
            if (povValue >= 31500 || povValue <= 4500) return DPadDirection.Up;
            if (povValue > 4500 && povValue < 13500) return DPadDirection.Right;
            if (povValue >= 13500 && povValue <= 22500) return DPadDirection.Down;
            if (povValue > 22500 && povValue < 31500) return DPadDirection.Left;
            return DPadDirection.None;
        }

        public void Dispose()
        {
            Stop();
            _joystick?.Dispose();
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