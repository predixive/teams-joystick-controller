namespace TeamsJoystickController.Core.Commands;

public interface ITeamsController
{
    bool ToggleMute();

    bool ToggleCamera();

    bool ToggleHand();

    bool ShareScreenPreferred();

    bool OpenShareTray();

    bool React(string? reactionName);
}
