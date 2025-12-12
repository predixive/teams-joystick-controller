using TeamsJoystickController.Core.Config;
using TeamsJoystickController.Core.Input;
using TeamsJoystickController.Core.Logging;

namespace TeamsJoystickController.Core.Commands;

public class CommandDispatcher
{
    private AppConfig _config;
    private readonly ITeamsController _teamsController;

    public CommandDispatcher(AppConfig config, ITeamsController teamsController)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _teamsController = teamsController ?? throw new ArgumentNullException(nameof(teamsController));
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void OnButtonPatternDetected(int buttonId, PressPattern pattern)
    {
        if (!_config.Buttons.TryGetValue(buttonId, out var buttonConfig) || buttonConfig is null)
        {
            Log.Warn($"No button configuration found for button {buttonId}.");
            return;
        }

        var action = pattern switch
        {
            PressPattern.Single => buttonConfig.Single,
            PressPattern.Double => buttonConfig.Double,
            PressPattern.Triple => buttonConfig.Triple,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(action))
        {
            Log.Info($"No action configured for button {buttonId} with pattern {pattern}.");
            return;
        }

        var command = CommandParser.Parse(action);
        if (command is null)
        {
            Log.Warn($"Unrecognized action '{action}' for button {buttonId}.");
            return;
        }

        switch (command.Type)
        {
            case LogicalCommandType.ToggleMute:
                Log.Info($"Executing ToggleMute for button {buttonId} ({pattern}).");
                _teamsController.ToggleMute();
                break;
            case LogicalCommandType.ToggleCamera:
                Log.Info($"Executing ToggleCamera for button {buttonId} ({pattern}).");
                _teamsController.ToggleCamera();
                break;
            case LogicalCommandType.ToggleHand:
                Log.Info($"Executing ToggleHand for button {buttonId} ({pattern}).");
                _teamsController.ToggleHand();
                break;
            case LogicalCommandType.ShareScreenPreferred:
                Log.Info($"Executing ShareScreenPreferred for button {buttonId} ({pattern}).");
                _teamsController.ShareScreenPreferred();
                break;
            case LogicalCommandType.OpenShareTray:
                Log.Info($"Executing OpenShareTray for button {buttonId} ({pattern}).");
                _teamsController.OpenShareTray();
                break;
            case LogicalCommandType.React:
                Log.Info($"Executing React '{command.Parameter ?? ""}' for button {buttonId} ({pattern}).");
                _teamsController.React(command.Parameter);
                break;
            default:
                Log.Warn($"No handler implemented for command type {command.Type}.");
                break;
        }
    }
}
