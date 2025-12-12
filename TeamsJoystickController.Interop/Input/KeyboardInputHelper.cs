using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TeamsJoystickController.Interop.Input;

public static class KeyboardInputHelper
{
    private static readonly HashSet<Keys> ModifierKeys = new()
    {
        Keys.ShiftKey,
        Keys.LShiftKey,
        Keys.RShiftKey,
        Keys.ControlKey,
        Keys.LControlKey,
        Keys.RControlKey,
        Keys.Menu,
        Keys.LMenu,
        Keys.RMenu,
        Keys.LWin,
        Keys.RWin
    };

    public static void SendKeyChord(params Keys[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return;
        }

        var normalizedKeys = keys
            .Select(k => k & Keys.KeyCode)
            .Distinct()
            .ToList();

        var modifiers = normalizedKeys.Where(IsModifier).ToList();
        var primaryKeys = normalizedKeys.Where(k => !IsModifier(k)).ToList();

        if (primaryKeys.Count == 0 && modifiers.Count == 0)
        {
            return;
        }

        var inputs = new List<INPUT>();

        foreach (var modifier in modifiers)
        {
            inputs.Add(CreateKeyInput(modifier, isKeyUp: false));
        }

        foreach (var primary in primaryKeys)
        {
            inputs.Add(CreateKeyInput(primary, isKeyUp: false));
        }

        foreach (var primary in primaryKeys)
        {
            inputs.Add(CreateKeyInput(primary, isKeyUp: true));
        }

        foreach (var modifier in modifiers.AsEnumerable().Reverse())
        {
            inputs.Add(CreateKeyInput(modifier, isKeyUp: true));
        }

        if (inputs.Count == 0)
        {
            return;
        }

        SendInputInterop.SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static bool IsModifier(Keys key)
    {
        return ModifierKeys.Contains(key);
    }

    private static INPUT CreateKeyInput(Keys key, bool isKeyUp)
    {
        return new INPUT
        {
            Type = InputType.Keyboard,
            Data = new InputUnion
            {
                Keyboard = new KEYBDINPUT
                {
                    WVk = (ushort)key,
                    WScan = 0,
                    DwFlags = isKeyUp ? KeyboardEventFlags.KeyUp : KeyboardEventFlags.None,
                    Time = 0,
                    DwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
