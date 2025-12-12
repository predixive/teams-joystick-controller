using System;
using System.Collections.Generic;
using System.Threading;
using TeamsJoystickController.Core.Config;
using TeamsJoystickController.Core.Logging;

namespace TeamsJoystickController.Core.Input;

public enum PressPattern
{
    Single,
    Double,
    Triple
}

public class ButtonPatternService : IDisposable
{
    private TimingConfig _timingConfig;
    private readonly Dictionary<int, ButtonPressState> _states = new();
    private readonly object _sync = new();
    private bool _disposed;

    public ButtonPatternService(TimingConfig timingConfig)
    {
        _timingConfig = timingConfig ?? throw new ArgumentNullException(nameof(timingConfig));
    }

    public event Action<int, PressPattern>? ButtonPatternDetected;

    public void OnButtonDown(int buttonId)
    {
        Log.Info($"Button down received for button {buttonId}");
    }

    public void UpdateTiming(TimingConfig timingConfig)
    {
        if (timingConfig == null)
        {
            throw new ArgumentNullException(nameof(timingConfig));
        }

        lock (_sync)
        {
            _timingConfig = timingConfig;
        }

        Log.Info("Timing configuration updated for button pattern detection.");
    }

    public void OnButtonUp(int buttonId)
    {
        if (_disposed)
        {
            return;
        }

        Log.Info($"Button up received for button {buttonId}");

        lock (_sync)
        {
            if (!_states.TryGetValue(buttonId, out var state))
            {
                state = new ButtonPressState(buttonId);
                _states[buttonId] = state;
            }

            state.PressCount++;

            if (state.PressCount == 1)
            {
                state.Timer = new Timer(OnPatternTimeout, buttonId, _timingConfig.DoublePressThresholdMs, Timeout.Infinite);
            }
            else
            {
                ResetTimer(state, _timingConfig.TriplePressThresholdMs);
            }
        }
    }

    private void OnPatternTimeout(object? state)
    {
        if (state is not int buttonId)
        {
            return;
        }

        int pressCount;
        Timer? timerToDispose = null;

        lock (_sync)
        {
            if (!_states.TryGetValue(buttonId, out var buttonState))
            {
                return;
            }

            pressCount = buttonState.PressCount;
            timerToDispose = buttonState.Timer;
            _states.Remove(buttonId);
        }

        timerToDispose?.Dispose();

        var pattern = pressCount switch
        {
            1 => PressPattern.Single,
            2 => PressPattern.Double,
            _ => PressPattern.Triple
        };

        Log.Info($"Button {buttonId} pattern detected: {pattern} after {pressCount} presses");
        ButtonPatternDetected?.Invoke(buttonId, pattern);
    }

    private void ResetTimer(ButtonPressState state, int dueTimeMs)
    {
        if (state.Timer == null)
        {
            state.Timer = new Timer(OnPatternTimeout, state.ButtonId, dueTimeMs, Timeout.Infinite);
        }
        else
        {
            state.Timer.Change(dueTimeMs, Timeout.Infinite);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        List<Timer> timers;

        lock (_sync)
        {
            timers = new List<Timer>();
            foreach (var state in _states.Values)
            {
                if (state.Timer != null)
                {
                    timers.Add(state.Timer);
                }
            }

            _states.Clear();
            _disposed = true;
        }

        foreach (var timer in timers)
        {
            timer.Dispose();
        }
    }

    private sealed class ButtonPressState
    {
        public ButtonPressState(int buttonId)
        {
            ButtonId = buttonId;
        }

        public int ButtonId { get; }

        public int PressCount { get; set; }

        public Timer? Timer { get; set; }
    }
}
