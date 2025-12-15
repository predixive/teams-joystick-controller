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

public partial class App : System.Windows.Application
{
    private const int WM_INPUT = 0x00FF;

    private ConfigService? _configService;
    private AppConfig? _config;
    private HwndSource? _hwndSource;
    private RawInputJoystick? _rawInputJoystick;
    private JoystickInputService? _joystickInputService;
    private ButtonPatternService? _buttonPatternService;
    private CommandDispatcher? _commandDispatcher;
    private TeamsControllerShortcuts? _teamsController;
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _learningMenuItem;

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
        _buttonPatternService = new ButtonPatternService(_config.Timing, _config.Buttons.Keys);
        _teamsController = new TeamsControllerShortcuts(_config.Teams);
        _commandDispatcher = new CommandDispatcher(_config, _teamsController);

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
        _learningMenuItem = new ToolStripMenuItem
        {
            CheckOnClick = true
        };
        UpdateLearningMenuItem();
        _learningMenuItem.Click += (_, _) => ToggleLearningMode();
        contextMenu.Items.Add(_learningMenuItem);
        contextMenu.Items.Add("Test: Toggle Mute", null, (_, _) => TestToggleMute());
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
            _buttonPatternService?.UpdateAllowedButtons(newConfig.Buttons.Keys);

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

    private void ToggleLearningMode()
    {
        if (_rawInputJoystick == null || _learningMenuItem == null)
        {
            return;
        }

        bool newState = _learningMenuItem.Checked;
        _rawInputJoystick.LearningMode = newState;
        Log.Info(newState ? "LearningMode ON" : "LearningMode OFF");
        UpdateLearningMenuItem();
        ShowLearningModeBalloon();
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

    private void UpdateLearningMenuItem()
    {
        if (_learningMenuItem == null)
        {
            return;
        }

        bool isOn = _rawInputJoystick?.LearningMode == true;
        _learningMenuItem.Checked = isOn;
        _learningMenuItem.Text = isOn ? "Learning Mode (ON)" : "Learning Mode (OFF)";
    }

    private void ShowLearningModeBalloon()
    {
        if (_notifyIcon == null)
        {
            return;
        }

        bool isOn = _rawInputJoystick?.LearningMode == true;
        _notifyIcon.BalloonTipTitle = "Learning Mode";
        _notifyIcon.BalloonTipText = isOn ? "Learning Mode ON" : "Learning Mode OFF";
        _notifyIcon.ShowBalloonTip(2000);
    }

    private void TestToggleMute()
    {
        if (_teamsController == null)
        {
            return;
        }

        Log.Info("Test ToggleMute starting.");
        bool result = _teamsController.ToggleMuteDebugNoRestore();
        Log.Info($"Test ToggleMute result={result}");
        ShowToggleMuteBalloon(result);
    }

    private void ShowToggleMuteBalloon(bool success)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "Test: Toggle Mute";
        _notifyIcon.BalloonTipText = success ? "ToggleMute sent (success)" : "ToggleMute sent (fail)";
        _notifyIcon.ShowBalloonTip(2000);
    }
}
