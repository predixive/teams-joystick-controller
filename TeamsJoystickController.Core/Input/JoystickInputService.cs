using System;
using TeamsJoystickController.Interop.RawInput;

namespace TeamsJoystickController.Core.Input;

public class JoystickInputService
{
    private readonly RawInputJoystick _rawInputJoystick;
    private bool _isStarted;

    public JoystickInputService(RawInputJoystick rawInputJoystick)
    {
        _rawInputJoystick = rawInputJoystick ?? throw new ArgumentNullException(nameof(rawInputJoystick));
        _rawInputJoystick.ButtonStateChanged += OnButtonStateChanged;
    }

    public event Action<int>? ButtonDown;

    public event Action<int>? ButtonUp;

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        _rawInputJoystick.RegisterForJoystickDevices();
        _isStarted = true;
    }

    public void Stop()
    {
        _isStarted = false;
    }

    private void OnButtonStateChanged(int buttonId, bool isDown)
    {
        if (isDown)
        {
            ButtonDown?.Invoke(buttonId);
        }
        else
        {
            ButtonUp?.Invoke(buttonId);
        }
    }
}
