using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace TeamsJoystickController.Interop.Window;

public static class WindowHelper
{
    private const string User32 = "user32.dll";

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport(User32)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport(User32, CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport(User32)]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    public static IntPtr GetCurrentForegroundWindow()
    {
        return GetForegroundWindow();
    }

    public static bool TrySetForegroundWindow(IntPtr hWnd)
    {
        return hWnd != IntPtr.Zero && SetForegroundWindow(hWnd);
    }

    public static bool RestoreWindowIfMinimised(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        if (IsIconic(hWnd))
        {
            return ShowWindow(hWnd, SW_RESTORE);
        }

        return true;
    }

    public static bool IsWindowMinimised(IntPtr hWnd) => hWnd != IntPtr.Zero && IsIconic(hWnd);

    public static IntPtr? FindTeamsMeetingWindow()
    {
        var candidates = GetTeamsWindowCandidates();
        foreach (var candidate in candidates)
        {
            if (TryFindMeetingControl(candidate.Hwnd, out var matchName))
            {
                Debug.WriteLine($"UIA match '{matchName}' for hwnd=0x{candidate.Hwnd.ToInt64():X}");
                Debug.WriteLine($"Selected meeting hwnd=0x{candidate.Hwnd.ToInt64():X} via UIA");
                return candidate.Hwnd;
            }
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Title.Contains("Teams", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Fallback selecting Teams title match hwnd=0x{candidate.Hwnd.ToInt64():X}");
                return candidate.Hwnd;
            }
        }

        return candidates.FirstOrDefault().Hwnd;
    }

    public static List<(IntPtr Hwnd, string Title, string ProcessName, int ProcessId)> GetTeamsWindowCandidates()
    {
        var candidateProcessNames = new[] { "ms-teams", "ms-teams-consumer", "msteams" };
        var processes = Process.GetProcesses()
            .Where(p => candidateProcessNames.Any(name => p.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var result = new List<(IntPtr, string, string, int)>();

        foreach (var process in processes)
        {
            var windows = GetTopLevelWindowsForProcess(process.Id);
            foreach (var window in windows)
            {
                var title = GetWindowTitle(window);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                if (ShouldSkipWindow(title, window))
                {
                    continue;
                }

                result.Add((window, title, process.ProcessName, process.Id));
            }
        }

        return result;
    }

    private static List<IntPtr> GetTopLevelWindowsForProcess(int processId)
    {
        var handles = new List<IntPtr>();

        bool Callback(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd == IntPtr.Zero)
            {
                return true;
            }

            GetWindowThreadProcessId(hWnd, out var windowProcessId);
            if (windowProcessId == processId)
            {
                handles.Add(hWnd);
            }

            return true;
        }

        EnumWindows(Callback, IntPtr.Zero);
        return handles;
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var buffer = new StringBuilder(512);
        _ = GetWindowText(hWnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static bool ShouldSkipWindow(string title, IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd))
        {
            return true;
        }

        var lowered = title.ToLowerInvariant();
        if (lowered.Contains("default ime") || lowered.Contains("msctfime") || lowered.Contains("dde server window"))
        {
            return true;
        }

        return false;
    }

    private static bool TryFindMeetingControl(IntPtr hwnd, out string matchedName)
    {
        matchedName = string.Empty;

        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root == null)
            {
                return false;
            }

            var names = new[]
            {
                "Mute",
                "Unmute",
                "Turn camera on",
                "Turn camera off",
                "Raise hand",
                "Lower hand",
                "Share"
            };

            var buttons = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            foreach (AutomationElement button in buttons)
            {
                var nameObj = button.GetCurrentPropertyValue(AutomationElement.NameProperty, true);
                if (nameObj is not string buttonName || string.IsNullOrWhiteSpace(buttonName))
                {
                    continue;
                }

                foreach (var target in names)
                {
                    if (buttonName.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                        buttonName.Contains(target, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = buttonName;
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UIA search failed for hwnd=0x{hwnd.ToInt64():X}: {ex.Message}");
        }

        return false;
    }
}
