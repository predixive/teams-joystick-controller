using System;
using System.IO;
using System.Text;

namespace TeamsJoystickController.Core.Logging;

public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _logFilePath;

    static Log()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDirectory = Path.Combine(appData, "TeamsJoystickController", "logs");
        _logFilePath = Path.Combine(logDirectory, "log.txt");

        try
        {
            Directory.CreateDirectory(logDirectory);
        }
        catch
        {
            // Swallow exceptions during log directory creation to avoid impacting the app.
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        if (ex == null)
        {
            Write("ERROR", message);
            return;
        }

        var builder = new StringBuilder();
        builder.Append(message);
        builder.Append(" Exception: ");
        builder.Append(ex.Message);
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            builder.AppendLine();
            builder.Append(ex.StackTrace);
        }

        Write("ERROR", builder.ToString());
    }

    private static void Write(string level, string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";

            lock (_lock)
            {
                File.AppendAllText(_logFilePath, line);
            }
        }
        catch
        {
            // Swallow logging failures to avoid crashing the app.
        }
    }
}

