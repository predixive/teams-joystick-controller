using System;
using System.Diagnostics;
using Microsoft.Win32;
using TeamsJoystickController.Core.Logging;

namespace TeamsJoystickController.App;

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppName = "TeamsJoystickController";

    public static void EnsureStartupRegistered()
    {
        try
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                Log.Error("Unable to determine executable path for startup registration.");
                return;
            }

            using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (runKey == null)
            {
                Log.Error("Failed to open or create Run registry key for startup registration.");
                return;
            }

            var currentValue = runKey.GetValue(AppName) as string;
            if (!string.Equals(currentValue, executablePath, StringComparison.OrdinalIgnoreCase))
            {
                runKey.SetValue(AppName, executablePath);
                Log.Info("Registered application for startup.");
            }
            else
            {
                Log.Info("Startup registration already present.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to ensure startup registration.", ex);
        }
    }
}
