using System.Collections.Generic;

namespace TeamsJoystickController.Core.Config;

public class AppConfig
{
    public Dictionary<int, ButtonConfig> Buttons { get; set; } = new();

    public TeamsConfig Teams { get; set; } = new();

    public TimingConfig Timing { get; set; } = new();
}

public class ButtonConfig
{
    public string? Single { get; set; }

    public string? Double { get; set; }

    public string? Triple { get; set; }
}

public class TeamsConfig
{
    public int SharePreferredMonitorIndex { get; set; } = 1;

    public int FocusSettleMs { get; set; } = 150;

    public int PostSendHoldMs { get; set; } = 350;
}

public class TimingConfig
{
    public int DoublePressThresholdMs { get; set; } = 250;

    public int TriplePressThresholdMs { get; set; } = 350;
}
