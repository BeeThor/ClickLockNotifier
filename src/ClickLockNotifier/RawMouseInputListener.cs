using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClickLockNotifier;

internal sealed class RawMouseInputListener : NativeWindow, IDisposable
{
    private const int WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIM_TYPEMOUSE = 0;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    private const ushort HID_USAGE_GENERIC_MOUSE = 0x02;
    private const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
    private const ushort RI_MOUSE_LEFT_BUTTON_UP = 0x0002;

    private readonly Action<MouseHookMessage> _handler;
    private IntPtr _buffer;
    private uint _bufferSize;

    public RawMouseInputListener(Action<MouseHookMessage> handler)
    {
        _handler = handler;
        CreateHandle(new CreateParams
        {
            Caption = "ClickLockNotifierRawInputWindow"
        });
        RegisterMouseInput();
    }

    public void Dispose()
    {
        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
            _buffer = IntPtr.Zero;
            _bufferSize = 0;
        }

        DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_INPUT)
        {
            ProcessRawInput(m.LParam);
        }

        base.WndProc(ref m);
    }

    private void RegisterMouseInput()
    {
        var devices = new[]
        {
            new RawInputDevice
            {
                UsagePage = HID_USAGE_PAGE_GENERIC,
                Usage = HID_USAGE_GENERIC_MOUSE,
                Flags = RIDEV_INPUTSINK,
                Target = Handle
            }
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to register raw mouse input.");
        }
    }

    private void ProcessRawInput(IntPtr rawInputHandle)
    {
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
        var dataSize = 0u;
        var sizeResult = GetRawInputData(rawInputHandle, RID_INPUT, IntPtr.Zero, ref dataSize, headerSize);
        if (sizeResult != 0 || dataSize == 0)
        {
            return;
        }

        EnsureBuffer(dataSize);

        var readSize = GetRawInputData(rawInputHandle, RID_INPUT, _buffer, ref dataSize, headerSize);
        if (readSize != dataSize)
        {
            return;
        }

        var rawInput = Marshal.PtrToStructure<RawInput>(_buffer);
        if (rawInput.Header.Type != RIM_TYPEMOUSE)
        {
            return;
        }

        var buttonFlags = rawInput.Mouse.ButtonFlags;
        if ((buttonFlags & RI_MOUSE_LEFT_BUTTON_DOWN) != 0)
        {
            _handler(MouseHookMessage.LeftButtonDown);
        }

        if ((buttonFlags & RI_MOUSE_LEFT_BUTTON_UP) != 0)
        {
            _handler(MouseHookMessage.LeftButtonUp);
        }
    }

    private void EnsureBuffer(uint dataSize)
    {
        if (_buffer != IntPtr.Zero && _bufferSize >= dataSize)
        {
            return;
        }

        if (_buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_buffer);
        }

        _buffer = Marshal.AllocHGlobal((int)dataSize);
        _bufferSize = dataSize;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(
        RawInputDevice[] rawInputDevices,
        uint numberDevices,
        uint size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr rawInput,
        uint command,
        IntPtr data,
        ref uint size,
        uint sizeHeader);

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputDevice
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInputHeader
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawMouse
    {
        public ushort Flags;
        public uint Buttons;
        public uint RawButtons;
        public int LastX;
        public int LastY;
        public uint ExtraInformation;

        public ushort ButtonFlags => (ushort)(Buttons & 0xFFFF);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RawInput
    {
        public RawInputHeader Header;
        public RawMouse Mouse;
    }
}
