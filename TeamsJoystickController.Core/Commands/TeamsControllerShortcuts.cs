using System;
using System.Threading;
using System.Windows.Forms;
using TeamsJoystickController.Core.Config;
using TeamsJoystickController.Core.Logging;
using TeamsJoystickController.Interop.Input;
using TeamsJoystickController.Interop.Window;

namespace TeamsJoystickController.Core.Commands;

public class TeamsControllerShortcuts : ITeamsController
{
    private readonly TeamsConfig _teamsConfig;

    public TeamsControllerShortcuts(TeamsConfig teamsConfig)
    {
        _teamsConfig = teamsConfig ?? throw new ArgumentNullException(nameof(teamsConfig));
    }

    public bool ToggleMute()
    {
        return ExecuteWithTeamsWindow(() =>
        {
            Log.Info("Sending ToggleMute shortcut (Ctrl+Shift+M).");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.M);
        });
    }

    public bool ToggleCamera()
    {
        return ExecuteWithTeamsWindow(() =>
        {
            Log.Info("Sending ToggleCamera shortcut (Ctrl+Shift+O).");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.O);
        });
    }

    public bool ToggleHand()
    {
        return ExecuteWithTeamsWindow(() =>
        {
            Log.Info("Sending ToggleHand shortcut (Ctrl+Shift+K) - TODO: confirm for new Teams.");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.K);
        });
    }

    public bool ShareScreenPreferred()
    {
        // TODO: Use _teamsConfig.SharePreferredMonitorIndex with UI automation to select monitor.
        Log.Info($"ShareScreenPreferred falling back to OpenShareTray (preferred monitor {_teamsConfig.SharePreferredMonitorIndex}, selection TODO).");
        return OpenShareTray();
    }

    public bool OpenShareTray()
    {
        return ExecuteWithTeamsWindow(() =>
        {
            Log.Info("Sending OpenShareTray shortcut (Ctrl+Shift+E) - TODO: confirm for new Teams.");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.E);
        });
    }

    public bool React(string? reactionName)
    {
        var name = string.IsNullOrWhiteSpace(reactionName) ? "(default)" : reactionName;
        return ExecuteWithTeamsWindow(() =>
        {
            Log.Info($"Attempting reaction '{name}'. TODO: refine navigation to specific reaction.");

            // TODO: Confirm the shortcut to open the reactions menu in the new Teams client.
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.OemPeriod);
            Thread.Sleep(50);

            // TODO: Navigate to a specific reaction; this is a placeholder best-effort sequence.
            if (!string.IsNullOrWhiteSpace(reactionName))
            {
                // Attempt to move within the reaction picker; this is highly heuristic.
                KeyboardInputHelper.SendKeyChord(Keys.Tab);
                KeyboardInputHelper.SendKeyChord(Keys.Tab);
                KeyboardInputHelper.SendKeyChord(Keys.Enter);
            }
        });
    }

    private bool ExecuteWithTeamsWindow(Action action)
    {
        IntPtr? teamsHwnd = null;
        var previousForeground = IntPtr.Zero;

        try
        {
            teamsHwnd = WindowHelper.FindTeamsMeetingWindow();
            if (teamsHwnd is null || teamsHwnd == IntPtr.Zero)
            {
                Log.Warn("Teams window not found; cannot send shortcut.");
                return false;
            }

            previousForeground = WindowHelper.GetCurrentForegroundWindow();
            if (!WindowHelper.TrySetForegroundWindow(teamsHwnd.Value))
            {
                Log.Warn("Failed to bring Teams window to foreground.");
                return false;
            }

            Thread.Sleep(50);

            action();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Error while sending shortcut to Teams.", ex);
            return false;
        }
        finally
        {
            if (previousForeground != IntPtr.Zero && teamsHwnd.HasValue && teamsHwnd.Value != IntPtr.Zero)
            {
                WindowHelper.TrySetForegroundWindow(previousForeground);
            }
        }
    }
}
