using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

    public static IntPtr GetCurrentForegroundWindow()
    {
        return GetForegroundWindow();
    }

    public static bool TrySetForegroundWindow(IntPtr hWnd)
    {
        return hWnd != IntPtr.Zero && SetForegroundWindow(hWnd);
    }

    public static IntPtr? FindTeamsMeetingWindow()
    {
        var candidateProcessNames = new[] { "ms-teams", "ms-teams-consumer", "msteams" };
        var processes = Process.GetProcesses()
            .Where(p => candidateProcessNames.Any(name => p.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Console.WriteLine($"Searching for Teams windows among {processes.Count} candidate processes.");

        foreach (var process in processes)
        {
            var windows = GetTopLevelWindowsForProcess(process.Id);
            Console.WriteLine($"Process {process.ProcessName} ({process.Id}) has {windows.Count} top-level windows.");

            foreach (var window in windows)
            {
                var title = GetWindowTitle(window);
                if (string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                Console.WriteLine($"Window 0x{window.ToInt64():X}: '{title}'");

                if (title.Contains("Teams", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Selected Teams window: " + title);
                    return window;
                }
            }
        }

        Console.WriteLine("No Teams window found.");
        return null;
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

    private static string GetWindowTitle(IntPtr hWnd)
    {
        var buffer = new StringBuilder(512);
        _ = GetWindowText(hWnd, buffer, buffer.Capacity);
        return buffer.ToString();
    }
}
