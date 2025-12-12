using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TeamsJoystickController.Interop.RawInput;

public class RawInputJoystick
{
    private readonly IntPtr _hwnd;
    private byte[] _previousButtonReport = Array.Empty<byte>();

    public RawInputJoystick(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public event Action<int, bool>? ButtonStateChanged;

    public void RegisterForJoystickDevices()
    {
        var devices = new[]
        {
            new RAWINPUTDEVICE
            {
                UsagePage = 0x01, // Generic Desktop Controls
                Usage = 0x04,     // Joystick
                Flags = RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify,
                Target = _hwnd
            },
            new RAWINPUTDEVICE
            {
                UsagePage = 0x01, // Generic Desktop Controls
                Usage = 0x05,     // Gamepad
                Flags = RawInputDeviceFlags.InputSink | RawInputDeviceFlags.DevNotify,
                Target = _hwnd
            }
        };

        _ = RawInputInterop.RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    public void ProcessRawInput(IntPtr lParam)
    {
        uint dwSize = 0;
        uint headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();

        var result = RawInputInterop.GetRawInputData(lParam, RawInputCommand.Input, IntPtr.Zero, ref dwSize, headerSize);
        if (result == uint.MaxValue || dwSize == 0)
        {
            return;
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
        try
        {
            result = RawInputInterop.GetRawInputData(lParam, RawInputCommand.Input, buffer, ref dwSize, headerSize);
            if (result == uint.MaxValue)
            {
                return;
            }

            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
            if (header.Type != RawInputType.Hid)
            {
                return;
            }

            var hidPtr = IntPtr.Add(buffer, (int)headerSize);
            var rawHid = Marshal.PtrToStructure<RAWHID>(hidPtr);
            int dataOffset = Marshal.OffsetOf<RAWHID>(nameof(RAWHID.RawData)).ToInt32();
            int rawDataOffset = (int)headerSize + dataOffset;
            int totalBytes = checked((int)(rawHid.SizeHid * rawHid.Count));
            if (totalBytes <= 0)
            {
                return;
            }

            var hidData = new byte[totalBytes];
            Marshal.Copy(IntPtr.Add(buffer, rawDataOffset), hidData, 0, totalBytes);

            EmitButtonChanges(hidData);
            _previousButtonReport = hidData;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RawInput processing failed: {ex}");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void EmitButtonChanges(ReadOnlySpan<byte> currentReport)
    {
        if (_previousButtonReport.Length != currentReport.Length)
        {
            _previousButtonReport = new byte[currentReport.Length];
        }

        int bitCount = currentReport.Length * 8;
        for (int bitIndex = 0; bitIndex < bitCount; bitIndex++)
        {
            int byteIndex = bitIndex / 8;
            int bitOffset = bitIndex % 8;
            byte mask = (byte)(1 << bitOffset);

            bool isCurrentlyDown = (currentReport[byteIndex] & mask) != 0;
            bool wasDown = (_previousButtonReport[byteIndex] & mask) != 0;

            if (isCurrentlyDown == wasDown)
            {
                continue;
            }

            int buttonId = bitIndex + 1; // Buttons are 1-based in this model
            ButtonStateChanged?.Invoke(buttonId, isCurrentlyDown);
        }
    }
}
