using System;
using System.Runtime.InteropServices;

namespace TeamsJoystickController.Interop.Input;

public static class SendInputInterop
{
    private const string User32 = "user32.dll";

    [DllImport(User32, SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

public enum InputType : uint
{
    Mouse = 0,
    Keyboard = 1,
    Hardware = 2
}

[Flags]
public enum KeyboardEventFlags : uint
{
    None = 0x0000,
    ExtendedKey = 0x0001,
    KeyUp = 0x0002,
    Unicode = 0x0004,
    ScanCode = 0x0008
}

public enum VirtualKey : ushort
{
    Shift = 0x10,
    Control = 0x11,
    Menu = 0x12,
    LeftShift = 0xA0,
    RightShift = 0xA1,
    LeftControl = 0xA2,
    RightControl = 0xA3,
    LeftMenu = 0xA4,
    RightMenu = 0xA5,
    LeftWin = 0x5B,
    RightWin = 0x5C
}

[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
    public InputType Type;
    public InputUnion Data;
}

[StructLayout(LayoutKind.Explicit)]
public struct InputUnion
{
    [FieldOffset(0)]
    public MOUSEINPUT Mouse;

    [FieldOffset(0)]
    public KEYBDINPUT Keyboard;

    [FieldOffset(0)]
    public HARDWAREINPUT Hardware;
}

[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int Dx;
    public int Dy;
    public int MouseData;
    public uint DwFlags;
    public uint Time;
    public IntPtr DwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort WVk;
    public ushort WScan;
    public KeyboardEventFlags DwFlags;
    public uint Time;
    public IntPtr DwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
public struct HARDWAREINPUT
{
    public uint UMsg;
    public ushort WParamL;
    public ushort WParamH;
}
