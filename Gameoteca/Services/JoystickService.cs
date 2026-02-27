using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace Gameoteca.Services
{
    public class JoystickService : IDisposable
    {
        private readonly DirectInput _directInput;
        private Joystick? _joystick;
        private readonly DispatcherTimer _timer;
        private JoystickState _previousState;
        private bool _isRunning;

        // Eventos para botões
        public event EventHandler<int>? ButtonPressed;
        public event EventHandler<int>? ButtonReleased;
        public event EventHandler<JoystickAxisEventArgs>? AxisChanged;

        public JoystickService()
        {
            _directInput = new DirectInput();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += PollJoystick;
        }

        public void Start()
        {
            if (_isRunning) return;

            // Encontrar o primeiro joystick/gamepad
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            var deviceInstance = devices.FirstOrDefault();
            if (deviceInstance == null)
            {
                // Nenhum joystick encontrado
                return;
            }

            _joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
            _joystick.Properties.BufferSize = 128;
            _joystick.Acquire();

            // Ler estado inicial
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

                // Detectar mudanças nos botões
                for (int i = 0; i < state.Buttons.Length; i++)
                {
                    bool currentPressed = state.Buttons[i];
                    bool previousPressed = _previousState.Buttons[i];

                    if (currentPressed && !previousPressed)
                        ButtonPressed?.Invoke(this, i);
                    else if (!currentPressed && previousPressed)
                        ButtonReleased?.Invoke(this, i);
                }

                // Detectar mudanças nos eixos (ex: X, Y)
                if (state.X != _previousState.X)
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.X, state.X));
                if (state.Y != _previousState.Y)
                    AxisChanged?.Invoke(this, new JoystickAxisEventArgs(AxisType.Y, state.Y));

                _previousState = state;
            }
            catch
            {
                // Joystick pode ter sido desconectado
                Stop();
                // Tentar reconectar depois? Por simplicidade, paramos.
            }
        }

        public void Dispose()
        {
            Stop();
            _joystick?.Dispose();
            _directInput.Dispose();
        }
    }

    public enum AxisType { X, Y, Z, Rx, Ry, Rz, Slider0, Slider1 }

    public class JoystickAxisEventArgs : EventArgs
    {
        public AxisType Axis { get; }
        public int Value { get; }

        public JoystickAxisEventArgs(AxisType axis, int value)
        {
            Axis = axis;
            Value = value;
        }
    }
}