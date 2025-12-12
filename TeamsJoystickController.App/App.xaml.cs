using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;
using TeamsJoystickController.Core.Commands;
using TeamsJoystickController.Core.Config;
using TeamsJoystickController.Core.Input;
using TeamsJoystickController.Core.Logging;
using TeamsJoystickController.Interop.RawInput;

namespace TeamsJoystickController.App;

public partial class App : Application
{
    private const int WM_INPUT = 0x00FF;

    private ConfigService? _configService;
    private AppConfig? _config;
    private HwndSource? _hwndSource;
    private RawInputJoystick? _rawInputJoystick;
    private JoystickInputService? _joystickInputService;
    private ButtonPatternService? _buttonPatternService;
    private CommandDispatcher? _commandDispatcher;
    private NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Info("Application starting up.");

        _configService = new ConfigService();
        _config = _configService.Load();

        StartupRegistration.EnsureStartupRegistered();

        _hwndSource = CreateHiddenHwndSource();
        _rawInputJoystick = new RawInputJoystick(_hwndSource.Handle);
        _joystickInputService = new JoystickInputService(_rawInputJoystick);
        _buttonPatternService = new ButtonPatternService(_config.Timing);
        var teamsController = new TeamsControllerShortcuts(_config.Teams);
        _commandDispatcher = new CommandDispatcher(_config, teamsController);

        _joystickInputService.ButtonDown += _buttonPatternService.OnButtonDown;
        _joystickInputService.ButtonUp += _buttonPatternService.OnButtonUp;
        _buttonPatternService.ButtonPatternDetected += _commandDispatcher.OnButtonPatternDetected;

        _joystickInputService.Start();

        InitializeNotifyIcon();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _buttonPatternService?.Dispose();

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();

        base.OnExit(e);
    }

    private HwndSource CreateHiddenHwndSource()
    {
        var parameters = new HwndSourceParameters("TeamsJoystickControllerHiddenWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = (int)(WindowStyles.WS_DISABLED | WindowStyles.WS_POPUP),
            ExtendedWindowStyle = (int)(ExtendedWindowStyles.WS_EX_TOOLWINDOW | ExtendedWindowStyles.WS_EX_NOACTIVATE)
        };

        var source = new HwndSource(parameters);
        source.AddHook(WndProc);
        return source;
    }

    private void InitializeNotifyIcon()
    {
        if (_configService == null)
        {
            return;
        }

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open config", null, (_, _) => OpenConfigFile());
        contextMenu.Items.Add("Reload config", null, (_, _) => ReloadConfig());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Teams Joystick Controller",
            ContextMenuStrip = contextMenu
        };
    }

    private void OpenConfigFile()
    {
        if (_configService == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{_configService.ConfigFilePath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error("Failed to open configuration file in Notepad.", ex);
        }
    }

    private void ReloadConfig()
    {
        if (_configService == null)
        {
            return;
        }

        try
        {
            var newConfig = _configService.Load();
            _config = newConfig;

            _commandDispatcher?.UpdateConfig(newConfig);
            _buttonPatternService?.UpdateTiming(newConfig.Timing);

            Log.Info("Configuration reloaded from disk.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to reload configuration.", ex);
        }
    }

    private void ExitApplication()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        Shutdown();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            _rawInputJoystick?.ProcessRawInput(lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [Flags]
    private enum WindowStyles : int
    {
        WS_DISABLED = 0x08000000,
        WS_POPUP = unchecked((int)0x80000000)
    }

    [Flags]
    private enum ExtendedWindowStyles : int
    {
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_NOACTIVATE = 0x08000000
    }
}
