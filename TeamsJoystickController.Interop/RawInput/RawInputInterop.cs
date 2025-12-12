using System;
using System.Runtime.InteropServices;

namespace TeamsJoystickController.Interop.RawInput;

public static class RawInputInterop
{
    private const string User32 = "user32.dll";

    [DllImport(User32, SetLastError = true)]
    public static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport(User32, SetLastError = true)]
    public static extern uint GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
}

[Flags]
public enum RawInputDeviceFlags : uint
{
    None = 0x00000000,
    Remove = 0x00000001,
    Exclude = 0x00000010,
    PageOnly = 0x00000020,
    NoLegacy = 0x00000030,
    InputSink = 0x00000100,
    CaptureMouse = 0x00000200,
    NoHotKeys = 0x00000200,
    AppKeys = 0x00000400,
    ExInputSink = 0x00001000,
    DevNotify = 0x00002000
}

public enum RawInputCommand : uint
{
    Input = 0x10000003,
    Header = 0x10000005
}

public enum RawInputType : uint
{
    Mouse = 0,
    Keyboard = 1,
    Hid = 2
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTDEVICE
{
    public ushort UsagePage;
    public ushort Usage;
    public RawInputDeviceFlags Flags;
    public IntPtr Target;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTHEADER
{
    public RawInputType Type;
    public uint Size;
    public IntPtr Device;
    public IntPtr WParam;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWHID
{
    public uint SizeHid;
    public uint Count;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public byte[] RawData;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUTDATA
{
    public RAWHID Hid;
}

[StructLayout(LayoutKind.Sequential)]
public struct RAWINPUT
{
    public RAWINPUTHEADER Header;
    public RAWINPUTDATA Data;
}
