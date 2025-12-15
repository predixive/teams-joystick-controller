using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TeamsJoystickController.Interop.RawInput;

public class RawInputJoystick
{
    private readonly IntPtr _hwnd;
    private static readonly Action<string>? _logInfo = ResolveLogger();
    private static readonly Action<string>? _logDebug = ResolveLogger("Debug");
    private static readonly Stopwatch _logStopwatch = Stopwatch.StartNew();
    private static readonly HashSet<int> _ignoredByteIndices = new() { 3 };
    private static int _logWindowCount;
    private bool _initialised;
    private byte[] _previousButtonReport = Array.Empty<byte>();
    private readonly bool[] _previousLogicalState = new bool[5];

    public bool LearningMode { get; set; }

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
        LogDebug("WM_INPUT received");

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
                LogInfo($"Non-HID input: {header.Type}");
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

            bool reportChanged = !_initialised || ReportsDiffer(_previousButtonReport, hidData);
            bool hasRelevantChange = !_initialised || !LearningMode || HasNonIgnoredChanges(_previousButtonReport, hidData);
            bool shouldLog = LearningMode && reportChanged && hasRelevantChange && ShouldLogChangedReport();

            if (!_initialised)
            {
                _previousButtonReport = hidData.ToArray();
                DecodeLogicalState(hidData, _previousLogicalState);
                _initialised = true;
                LogInfo("Initial HID report captured");
                return;
            }

            if (shouldLog)
            {
                LogInfo($"HID report {totalBytes} bytes: {FormatHidPreview(hidData)}");

                if (_initialised)
                {
                    if (LearningMode && reportChanged)
                    {
                        LogLearningDiff(_previousButtonReport, hidData);
                    }

                    LogDiff(_previousButtonReport, hidData, skipIgnoredIndices: LearningMode);
                }
            }

            EmitButtonChanges(hidData);
            _previousButtonReport = hidData.ToArray();
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

    private static Action<string>? ResolveLogger(string methodName = "Info")
    {
        try
        {
            var logType = Type.GetType("TeamsJoystickController.Core.Logging.Log, TeamsJoystickController.Core");
            var infoMethod = logType?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, binder: null, types: new[] { typeof(string) }, modifiers: null);
            if (infoMethod == null)
            {
                return null;
            }

            return (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), infoMethod);
        }
        catch
        {
            return null;
        }
    }

    private static void LogInfo(string message)
    {
        try
        {
            _logInfo?.Invoke(message);
        }
        catch
        {
            // Swallow logging failures to avoid impacting input handling.
        }
    }

    private static string FormatHidPreview(byte[] hidData)
    {
        if (hidData.Length == 0)
        {
            return string.Empty;
        }

        int previewLength = Math.Min(32, hidData.Length);
        return BitConverter.ToString(hidData, 0, previewLength);
    }

    private static bool HasNonIgnoredChanges(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
    {
        if (previous.Length != current.Length)
        {
            return true;
        }

        for (int i = 0; i < current.Length; i++)
        {
            if (_ignoredByteIndices.Contains(i))
            {
                continue;
            }

            if (previous[i] != current[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool ReportsDiffer(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
    {
        if (previous.Length != current.Length)
        {
            return true;
        }

        for (int i = 0; i < previous.Length; i++)
        {
            if (previous[i] != current[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldLogChangedReport()
    {
        var elapsed = _logStopwatch.ElapsedMilliseconds;
        if (elapsed >= 1000)
        {
            _logStopwatch.Restart();
            _logWindowCount = 0;
        }

        if (_logWindowCount >= 10)
        {
            return false;
        }

        _logWindowCount++;
        return true;
    }

    private static void LogDiff(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current, bool skipIgnoredIndices = false)
    {
        int maxLength = Math.Max(previous.Length, current.Length);
        int logged = 0;
        var builder = new StringBuilder();

        for (int i = 0; i < maxLength && logged < 16; i++)
        {
            if (skipIgnoredIndices && _ignoredByteIndices.Contains(i))
            {
                continue;
            }

            byte oldByte = i < previous.Length ? previous[i] : (byte)0;
            byte newByte = i < current.Length ? current[i] : (byte)0;

            if (oldByte == newByte)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append("diff[");
            builder.Append(i);
            builder.Append("]: old=0x");
            builder.Append(oldByte.ToString("X2"));
            builder.Append(" new=0x");
            builder.Append(newByte.ToString("X2"));

            logged++;
        }

        if (builder.Length > 0)
        {
            LogInfo(builder.ToString());
        }
    }

    private static void LogLearningDiff(ReadOnlySpan<byte> previous, ReadOnlySpan<byte> current)
    {
        int maxLength = Math.Max(previous.Length, current.Length);
        int logged = 0;
        var builder = new StringBuilder();

        for (int i = 0; i < maxLength && logged < 8; i++)
        {
            if (_ignoredByteIndices.Contains(i))
            {
                continue;
            }

            byte oldByte = i < previous.Length ? previous[i] : (byte)0;
            byte newByte = i < current.Length ? current[i] : (byte)0;

            if (oldByte == newByte)
            {
                continue;
            }

            byte changedBits = (byte)(oldByte ^ newByte);

            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append("learn byte[");
            builder.Append(i);
            builder.Append("] old=0x");
            builder.Append(oldByte.ToString("X2"));
            builder.Append(" new=0x");
            builder.Append(newByte.ToString("X2"));
            builder.Append(" changedBits=0b");
            builder.Append(Convert.ToString(changedBits, 2).PadLeft(8, '0'));

            logged++;
        }

        if (builder.Length > 0)
        {
            LogInfo(builder.ToString());
        }
    }

    private void EmitButtonChanges(ReadOnlySpan<byte> currentReport)
    {
        if (currentReport.Length < 8)
        {
            return;
        }

        Span<bool> currentState = stackalloc bool[5];
        DecodeLogicalState(currentReport, currentState);

        for (int i = 0; i < currentState.Length; i++)
        {
            bool isCurrentlyDown = currentState[i];
            bool wasDown = _previousLogicalState[i];

            if (isCurrentlyDown == wasDown)
            {
                continue;
            }

            _previousLogicalState[i] = isCurrentlyDown;
            int buttonId = i + 1;
            ButtonStateChanged?.Invoke(buttonId, isCurrentlyDown);
            LogInfo($"Logical button {buttonId} {(isCurrentlyDown ? "DOWN" : "UP")}");
        }
    }

    private static void DecodeLogicalState(ReadOnlySpan<byte> report, Span<bool> target)
    {
        byte byte6 = report.Length > 6 ? report[6] : (byte)0;
        byte byte7 = report.Length > 7 ? report[7] : (byte)0;

        target[0] = (byte6 & 0x10) != 0;
        target[1] = (byte6 & 0x20) != 0;
        target[2] = (byte6 & 0x40) != 0;
        target[3] = (byte6 & 0x80) != 0;
        target[4] = (byte7 & 0x01) != 0;
    }

    private static void LogDebug(string message)
    {
        try
        {
            _logDebug?.Invoke(message);
        }
        catch
        {
            // Swallow logging failures to avoid impacting input handling.
        }
    }
}