using TeamsJoystickController.Core.Logging;

namespace TeamsJoystickController.Core.Commands;

public enum LogicalCommandType
{
    ToggleMute,
    ToggleCamera,
    ToggleHand,
    ShareScreenPreferred,
    OpenShareTray,
    React
}

public class LogicalCommand
{
    public LogicalCommandType Type { get; set; }

    public string? Parameter { get; set; }
}

public static class CommandParser
{
    public static LogicalCommand? Parse(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            Log.Warn("CommandParser received empty action string.");
            return null;
        }

        var parts = action.Split(':', 2, StringSplitOptions.TrimEntries);
        var commandName = parts[0];

        if (!TryParseType(commandName, out var type))
        {
            Log.Warn($"CommandParser could not parse action '{action}'.");
            return null;
        }

        string? parameter = null;
        if (type == LogicalCommandType.React && parts.Length > 1)
        {
            parameter = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];
        }

        return new LogicalCommand
        {
            Type = type,
            Parameter = parameter
        };
    }

    private static bool TryParseType(string commandName, out LogicalCommandType commandType)
    {
        if (commandName.Equals("ToggleMute", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.ToggleMute;
            return true;
        }

        if (commandName.Equals("ToggleCamera", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.ToggleCamera;
            return true;
        }

        if (commandName.Equals("ToggleHand", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.ToggleHand;
            return true;
        }

        if (commandName.Equals("ShareScreenPreferred", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.ShareScreenPreferred;
            return true;
        }

        if (commandName.Equals("OpenShareTray", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.OpenShareTray;
            return true;
        }

        if (commandName.Equals("React", StringComparison.OrdinalIgnoreCase))
        {
            commandType = LogicalCommandType.React;
            return true;
        }

        commandType = default;
        return false;
    }
}
