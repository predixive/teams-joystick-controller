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
        return ExecuteWithTeamsWindow("ToggleMute", () =>
        {
            Log.Info("Sending ToggleMute shortcut (Ctrl+Shift+M).");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.M);
        });
    }

    public bool ToggleCamera()
    {
        return ExecuteWithTeamsWindow("ToggleCamera", () =>
        {
            Log.Info("Sending ToggleCamera shortcut (Ctrl+Shift+O).");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.O);
        });
    }

    public bool ToggleMuteDebugNoRestore()
    {
        try
        {
            var hwnd = WindowHelper.FindTeamsMeetingWindow();
            if (hwnd is null || hwnd == IntPtr.Zero)
            {
                Log.Warn("ToggleMuteDebugNoRestore: Teams window not found.");
                return false;
            }

            Log.Info($"ToggleMuteDebugNoRestore selected hwnd=0x{hwnd.Value.ToInt64():X}");

            var beforeForeground = WindowHelper.GetCurrentForegroundWindow();
            var beforeTitle = WindowHelper.GetWindowTitle(beforeForeground);
            Log.Info($"Before foreground hwnd=0x{beforeForeground.ToInt64():X} title='{Truncate(beforeTitle)}'");

            bool isMinimised = WindowHelper.IsWindowMinimised(hwnd.Value);
            Log.Info(isMinimised ? "Teams is minimised; restoring" : "Teams not minimised; not restoring");
            if (isMinimised)
            {
                WindowHelper.RestoreWindowIfMinimised(hwnd.Value);
            }
            bool setForeground = WindowHelper.TrySetForegroundWindow(hwnd.Value);

            var afterForeground = WindowHelper.GetCurrentForegroundWindow();
            var afterTitle = WindowHelper.GetWindowTitle(afterForeground);
            Log.Info($"After SetForeground hwnd=0x{afterForeground.ToInt64():X} title='{Truncate(afterTitle)}' result={setForeground}");

            Thread.Sleep(150);
            Log.Info("Sleeping 150ms before sending keys");

            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.M);

            Log.Info("Sleeping 500ms after sending keys");
            Thread.Sleep(500);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error("ToggleMuteDebugNoRestore failed.", ex);
            return false;
        }
    }

    public bool ToggleHand()
    {
        return ExecuteWithTeamsWindow("ToggleHand", () =>
        {
            Log.Info("Sending ToggleHand shortcut (Ctrl+Shift+K) - TODO: confirm for new Teams.");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.K);
        });
    }

    public bool ShareScreenPreferred()
    {
        // TODO: Use _teamsConfig.SharePreferredMonitorIndex with UI automation to select monitor.
        Log.Info($"ShareScreenPreferred falling back to OpenShareTray (preferred monitor {_teamsConfig.SharePreferredMonitorIndex}, selection TODO).");
        return ExecuteWithTeamsWindow("ShareScreenPreferred", () =>
        {
            Log.Info("Sending OpenShareTray shortcut (Ctrl+Shift+E) - TODO: confirm for new Teams.");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.E);
        });
    }

    public bool OpenShareTray()
    {
        return ExecuteWithTeamsWindow("OpenShareTray", () =>
        {
            Log.Info("Sending OpenShareTray shortcut (Ctrl+Shift+E) - TODO: confirm for new Teams.");
            KeyboardInputHelper.SendKeyChord(Keys.ControlKey, Keys.ShiftKey, Keys.E);
        });
    }

    public bool React(string? reactionName)
    {
        var name = string.IsNullOrWhiteSpace(reactionName) ? "(default)" : reactionName;
        return ExecuteWithTeamsWindow($"React:{name}", () =>
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

    private bool ExecuteWithTeamsWindow(string actionName, Action action)
    {
        IntPtr? teamsHwnd = null;
        var previousForeground = IntPtr.Zero;
        string previousTitle = string.Empty;
        int focusSettleMs = _teamsConfig.FocusSettleMs <= 0 ? 150 : _teamsConfig.FocusSettleMs;
        int postSendHoldMs = _teamsConfig.PostSendHoldMs <= 0 ? 350 : _teamsConfig.PostSendHoldMs;

        try
        {
            Log.Info($"Teams shortcut {actionName} (settle={focusSettleMs}ms, hold={postSendHoldMs}ms)");
            var candidates = WindowHelper.GetTeamsWindowCandidates();
            if (candidates.Count == 0)
            {
                Log.Warn("Teams window not found; cannot send shortcut.");
                return false;
            }

            foreach (var candidate in candidates)
            {
                Log.Info($"Teams candidate hwnd=0x{candidate.Hwnd.ToInt64():X} title='{Truncate(candidate.Title)}' process={candidate.ProcessName}({candidate.ProcessId})");
            }

            var selected = candidates.Find(c => c.Title.Contains("Teams", StringComparison.OrdinalIgnoreCase));
            if (selected == default)
            {
                selected = candidates[0];
            }

            teamsHwnd = selected.Hwnd;
            Log.Info($"Selected Teams hwnd=0x{teamsHwnd.Value.ToInt64():X} title='{Truncate(selected.Title)}'");
            if (teamsHwnd is null || teamsHwnd == IntPtr.Zero)
            {
                Log.Warn("Teams window not found; cannot send shortcut.");
                return false;
            }

            previousForeground = WindowHelper.GetCurrentForegroundWindow();
            previousTitle = WindowHelper.GetWindowTitle(previousForeground);
            Log.Info($"Prev foreground hwnd=0x{previousForeground.ToInt64():X} title='{Truncate(previousTitle)}'");
            bool isMinimised = WindowHelper.IsWindowMinimised(teamsHwnd.Value);
            Log.Info(isMinimised ? "Teams is minimised; restoring" : "Teams not minimised; not restoring");
            if (isMinimised)
            {
                WindowHelper.RestoreWindowIfMinimised(teamsHwnd.Value);
            }
            bool broughtToFront = WindowHelper.TrySetForegroundWindow(teamsHwnd.Value);
            Log.Info($"SetForegroundWindow result={(broughtToFront ? "true" : "false")} for Teams hwnd=0x{teamsHwnd.Value.ToInt64():X}");
            if (!broughtToFront)
            {
                Log.Warn("Failed to bring Teams window to foreground.");
                return false;
            }

            var actualForeground = WindowHelper.GetCurrentForegroundWindow();
            var actualTitle = WindowHelper.GetWindowTitle(actualForeground);
            Log.Info($"After SetForegroundWindow actual hwnd=0x{actualForeground.ToInt64():X} title='{Truncate(actualTitle)}'");

            if (actualForeground != teamsHwnd.Value)
            {
                Thread.Sleep(100);
                bool retryForeground = WindowHelper.TrySetForegroundWindow(teamsHwnd.Value);
                actualForeground = WindowHelper.GetCurrentForegroundWindow();
                actualTitle = WindowHelper.GetWindowTitle(actualForeground);
                Log.Info($"Foreground retry result={(retryForeground ? "true" : "false")} hwnd=0x{actualForeground.ToInt64():X} title='{Truncate(actualTitle)}'");
            }

            Log.Info($"Focus settle {focusSettleMs}ms");
            Thread.Sleep(focusSettleMs);

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
                Log.Info($"Post-send hold {postSendHoldMs}ms");
                Thread.Sleep(postSendHoldMs);
                bool restored = WindowHelper.TrySetForegroundWindow(previousForeground);
                Log.Info($"Restore foreground hwnd=0x{previousForeground.ToInt64():X} title='{Truncate(previousTitle)}' result={restored}");
            }
        }
    }

    private static string Truncate(string? value, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
