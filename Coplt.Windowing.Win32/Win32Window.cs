using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using Coplt.Dropping;
using Coplt.Mathematics;
using static Windows.Win32.PInvoke;

namespace Coplt.Windowing.Win32;

[Dropping(Unmanaged = true)]
[SupportedOSPlatform("windows10.0")]
public partial class Win32Window : Window
{
    #region Field

    private readonly Guid m_guid;
    private readonly Win32WindowSystem m_ws;
    private GCHandle m_handle;
    private readonly HWND m_hwnd;
    private uint2? m_min_size;
    private uint2? m_max_size;
    private string m_title;
    private uint2 m_last_dpi;
    private uint2 m_last_size;
    private int2 m_last_pos;
    private uint2 m_last_pixel_size;
    private int2 m_last_pixel_pos;

    #endregion

    #region Props

    public override Guid Id => m_guid;
    public override WindowSystem WindowSystem => m_ws;

    public override unsafe void* TryGetHwnd => (void*)m_hwnd.Value;

    public override uint2? MinSize
    {
        get => m_min_size;
        set => m_min_size = value;
    }

    public override uint2? MaxSize
    {
        get => m_max_size;
        set => m_max_size = value;
    }

    #endregion

    #region Static

    internal static readonly ushort s_atom;

    internal static readonly HCURSOR s_defaultCursor;

    static unsafe Win32Window()
    {
        s_defaultCursor = LoadCursor(default, IDC_ARROW);
        fixed (char* name = "Coplt.Windowing.Win32.Win32Window")
        {
            var wnd_class = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                style = WNDCLASS_STYLES.CS_HREDRAW | WNDCLASS_STYLES.CS_VREDRAW,
                lpfnWndProc = &WndProc,
                hInstance = GetModuleHandle(default(PCWSTR)),
                hCursor = s_defaultCursor,
                hbrBackground = default,
                lpszClassName = name,
            };

            s_atom = RegisterClassEx(in wnd_class);
            if (s_atom == 0) throw new Win32Exception();
        }
    }

    #endregion

    #region Ctor

    internal unsafe Win32Window(Win32WindowSystem ws, in WindowOptions options)
    {
        m_title = options.Title ?? "";
        m_min_size = options.MinSize;
        m_max_size = options.MaxSize;
        m_guid = Guid.NewGuid();
        m_ws = ws;
        m_handle = GCHandle.Alloc(this, GCHandleType.Weak);

        var style = WINDOW_STYLE.WS_CLIPCHILDREN;
        var ex_style = default(WINDOW_EX_STYLE);
        if (options.Blur is not WindowBlur.None) ex_style |= WINDOW_EX_STYLE.WS_EX_COMPOSITED;
        if (options.Style is WindowStyle.Borderless) style |= WINDOW_STYLE.WS_POPUP;
        else style |= WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
        if (!options.Resizeable) style &= ~WINDOW_STYLE.WS_THICKFRAME;
        if (!options.Maximizable) style &= ~WINDOW_STYLE.WS_MAXIMIZEBOX;
        if (!options.Minimizable) style &= ~WINDOW_STYLE.WS_MINIMIZEBOX;

        var pos = options.Position ?? CW_USEDEFAULT;
        var size = (int2)(options.Size ?? unchecked((uint)CW_USEDEFAULT));

        var monitor = options.Position.HasValue
            ? new(MonitorFromPoint(new(pos.x, pos.y), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
            : m_ws.GetWin32PrimaryMonitorS();

        if (options.Style is not WindowStyle.Borderless)
        {
            var rect = RECT.FromXYWH(pos.x, pos.y, size.x, size.y);
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393))
            {
                var dpi = monitor.RawDpi;
                if (AdjustWindowRectExForDpi(&rect, style, default, ex_style, dpi.x) == 0) throw new Win32Exception();
            }
            else
            {
                if (AdjustWindowRectEx(&rect, style, default, ex_style) == 0) throw new Win32Exception();
            }

            if (options.Position.HasValue) pos = new(rect.X, rect.Y);
            if (options.Size.HasValue) size = new(rect.Width, rect.Height);
        }

        if (options is { Centered: true, Position: null })
        {
            var info = monitor.Info;
            var center = info.Size / 2;
            var u_pos = math.max(center - (uint2)size / 2, 0);
            pos = info.Position + (int2)u_pos;
        }

        m_last_size = (uint2)size;
        m_last_pos = pos;

        fixed (char* title = m_title)
        {
            m_hwnd = CreateWindowEx(
                ex_style, new PCWSTR((char*)s_atom), title, style,
                pos.x, pos.y, size.x, size.y,
                default, default, default,
                (void*)GCHandle.ToIntPtr(m_handle)
            );
        }
        if (m_hwnd == default) throw new Win32Exception();

        if (options.Show)
        {
            if (options.Maximize)
            {
                ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED);
            }
            else if (options.Minimize)
            {
                ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_SHOWMINIMIZED);
            }
            else
            {
                ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_SHOW);
            }
        }

        if (options.Blur != WindowBlur.None) SetBlur(options.Blur);

        if (options.MainWindow) m_ws.SetMainWindow(this);

        m_ws.m_windows.Add(m_guid, this);
    }

    #endregion

    #region Center

    public override void Center()
    {
        if (GetWindowRect(m_hwnd, out var rect) == 0) throw new Win32Exception();
        Center(in rect);
    }

    private void Center(in RECT rect)
    {
        var monitor = Monitor;
        var info = monitor.Info;
        var center = info.Size / 2;
        var size = new int2(rect.Width, rect.Height);
        var u_pos = math.max(center - (uint2)size / 2, 0);
        var pos = info.Position + (int2)u_pos;
        var flags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOSIZE;
        if (SetWindowPos(m_hwnd, default, pos.x, pos.y, size.x, size.y, flags) == 0)
            throw new Win32Exception();
    }

    #endregion

    #region SetBlur

    private unsafe void SetBlur(WindowBlur blur)
    {
        var success = false;
        if ((blur & WindowBlur.MicaAlt) == WindowBlur.MicaAlt)
        {
            // SetBackdropType 
            var value = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW;
            success = DwmSetWindowAttribute(m_hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &value,
                sizeof(DWM_SYSTEMBACKDROP_TYPE)) == 0;
        }
        else if ((blur & WindowBlur.Mica) == WindowBlur.Mica)
        {
            // SetBackdropType 
            var value = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
            success = DwmSetWindowAttribute(m_hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &value,
                sizeof(DWM_SYSTEMBACKDROP_TYPE)) == 0;
        }
        else if ((blur & WindowBlur.Blur) != 0)
        {
            // SetBackdropType 
            var value = DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW;
            success = DwmSetWindowAttribute(m_hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, &value,
                sizeof(DWM_SYSTEMBACKDROP_TYPE)) == 0;
        }
        if (blur is not WindowBlur.None)
        {
            if (success)
            {
                // ExtendFrameIntoClientArea
                var margins = new MARGINS
                    { cxLeftWidth = -1, cxRightWidth = -1, cyBottomHeight = -1, cyTopHeight = -1 };
                DwmExtendFrameIntoClientArea(m_hwnd, &margins);
            }
            else
            {
                var ex_style = GetWindowLongPtr(m_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
                ex_style &= ~(nint)WINDOW_EX_STYLE.WS_EX_COMPOSITED;
                SetWindowLongPtr(m_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, ex_style);
            }
        }
    }

    #endregion

    #region Drop

    [Drop]
    private void Drop()
    {
        DestroyWindow(m_hwnd);
        m_handle.Free();
    }

    #endregion

    #region WndProc

    private unsafe LRESULT WndProcInst(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg >= WM_USER)
        {
            // switch ((Messages)msg)
            // {
            //     
            // }
        }
        else
        {
            switch (msg)
            {
                case WM_CLOSE:
                {
                    var ev = Win32WindowSystem.ExceptionSafeScope<Win32Window, WindowCloseEvent?>(this, static self =>
                    {
                        var ev = new WindowCloseEvent(self);
                        self.m_ws.Emit(ev);
                        self.Emit(ev);
                        return ev;
                    }, static _ => null);
                    if (ev is { Canceled: true }) return default;
                    Dispose();
                    return default;
                }
                case WM_DESTROY:
                {
                    m_ws.OnWindowClosed(this);
                    return default;
                }
                case WM_GETMINMAXINFO:
                {
                    var lpMMI = (MINMAXINFO*)lParam.Value;
                    if (m_min_size is { } min_size)
                    {
                        lpMMI->ptMinTrackSize.X = (int)min_size.x;
                        lpMMI->ptMinTrackSize.Y = (int)min_size.y;
                    }
                    if (m_max_size is { } max_size)
                    {
                        lpMMI->ptMaxTrackSize.X = (int)max_size.x;
                        lpMMI->ptMaxTrackSize.Y = (int)max_size.y;
                    }
                    return default;
                }
                case WM_SIZE:
                {
                    if (wParam == SIZE_MAXIMIZED || wParam == SIZE_RESTORED)
                    {
                        Win32WindowSystem.ExceptionSafeScope(this,
                            static self => self.TryEmitWindowRectChangeEvent());
                    }
                    return default;
                }
                case WM_MOVE:
                case WM_DPICHANGED:
                {
                    Win32WindowSystem.ExceptionSafeScope(this,
                        static self => self.TryEmitWindowRectChangeEvent());
                    return default;
                }
            }
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        try
        {
            if (msg == WM_CREATE)
            {
                var pCreateStruct = (CREATESTRUCTW*)lParam.Value;
                SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, (nint)pCreateStruct->lpCreateParams);
                return default;
            }
            GCHandle handle;
            try
            {
                handle = GCHandle.FromIntPtr(GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA));
            }
            catch
            {
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
            if (handle.Target is Win32Window win) return win.WndProcInst(hWnd, msg, wParam, lParam);
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
        catch (Exception e)
        {
            try
            {
                Win32WindowSystem.EmitUnhandledException(e);
            }
            catch
            {
                // ignored
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    #endregion

    #region SetMainWindow

    public override void SetMainWindow(bool isMainWindow = true) => m_ws.SetMainWindow(this, isMainWindow);

    #endregion

    #region Title

    public override string Title
    {
        get => m_title;
        set
        {
            m_title = value;
            if (SetWindowText(m_hwnd, value) == 0) throw new Win32Exception();
        }
    }

    #endregion

    #region Show

    public override void Show()
    {
        ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_SHOWDEFAULT);
    }

    #endregion

    #region Hide

    public override void Hide()
    {
        ShowWindow(m_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
    }

    #endregion

    #region Rect

    public override int2 Position => Rect.xy;

    public override uint2 Size => (uint2)Rect.zw;

    public override int4 Rect
    {
        get
        {
            if (GetWindowRect(m_hwnd, out var rect) == 0) throw new Win32Exception();
            return new(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    public override int2 PixelPosition => PixelRect.xy;

    public override uint2 PixelSize => (uint2)PixelRect.zw;

    public override int4 PixelRect
    {
        get
        {
            if (GetWindowRect(m_hwnd, out var rect) == 0) throw new Win32Exception();
            return (int4)(new int4(rect.X, rect.Y, rect.Width, rect.Height) * ScaleByDpi.xyxy);
        }
    }

    private void TryEmitWindowRectChangeEvent()
    {
        var dpi = RawDpi;
        var scale_by_dpi = dpi / new double2(SWin32Monitor.StandardDpi);
        var rect = Rect;
        var size = (uint2)rect.zw;
        var pos = rect.xy;
        var pixel_size = (uint2)(size * scale_by_dpi);
        var pixel_pos = (int2)(pos * scale_by_dpi);

        var old_dpi = m_last_dpi;
        var old_size = m_last_size;
        var old_pos = m_last_pos;
        var old_pixel_size = m_last_pixel_size;
        var old_pixel_pos = m_last_pixel_pos;

        var dpi_changed = math.any(dpi != old_dpi);
        var size_changed = math.any(size != old_size);
        var pos_changed = math.any(pos != old_pos);
        var pixel_size_changed = math.any(pixel_size != old_pixel_size);
        var pixel_pos_changed = math.any(pixel_pos != old_pixel_pos);

        if (!(dpi_changed || size_changed || pos_changed || pixel_size_changed || pixel_pos_changed)) return;

        m_last_dpi = dpi;
        m_last_size = size;
        m_last_pos = pos;
        m_last_pixel_size = pixel_size;
        m_last_pixel_pos = pixel_pos;

        var old_scale_by_dpi = old_dpi / new double2(SWin32Monitor.StandardDpi);

        {
            var ev = new WindowRectChangeEvent(
                this,
                pos,
                size,
                pixel_pos,
                pixel_size,
                scale_by_dpi,
                old_pos,
                old_size,
                old_pixel_pos,
                old_pixel_size,
                old_scale_by_dpi,
                pos_changed,
                size_changed,
                pixel_pos_changed,
                pixel_size_changed,
                dpi_changed
            );
            m_ws.Emit(ev);
            Emit(ev);
        }
        if (pos_changed || pixel_pos_changed)
        {
            var ev = new WindowPositionChangeEvent(
                this,
                pos,
                pixel_pos,
                old_pos,
                old_pixel_pos
            );
            m_ws.Emit(ev);
            Emit(ev);
        }
        if (size_changed || pixel_size_changed)
        {
            var ev = new WindowSizeChangeEvent(
                this,
                size,
                pixel_size,
                old_size,
                old_pixel_size
            );
            m_ws.Emit(ev);
            Emit(ev);
        }
        if (dpi_changed)
        {
            var ev = new WindowDpiChangeEvent(
                this,
                scale_by_dpi,
                old_scale_by_dpi
            );
            m_ws.Emit(ev);
            Emit(ev);
        }
    }

    #endregion

    #region Monitor

    public SWin32Monitor SMonitor
    {
        get
        {
            var monitor = MonitorFromWindow(m_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            return new SWin32Monitor(monitor);
        }
    }

    public override Monitor Monitor
    {
        get
        {
            var monitor = MonitorFromWindow(m_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
            return new Win32Monitor(monitor);
        }
    }

    public override uint2 RawDpi => Monitor.RawDpi;

    public override double2 ScaleByDpi => Monitor.ScaleByDpi;

    #endregion

    #region Close

    public override void Close()
    {
        PostMessage(m_hwnd, WM_SYSCOMMAND, SC_CLOSE, default);
    }

    #endregion

    #region Minimize Maximize

    public override void Restore()
    {
        PostMessage(m_hwnd, WM_SYSCOMMAND, SC_RESTORE, 0);
    }

    public override void Minimize()
    {
        PostMessage(m_hwnd, WM_SYSCOMMAND, SC_MINIMIZE, 0);
    }

    public override void Maximize()
    {
        PostMessage(m_hwnd, WM_SYSCOMMAND, SC_MAXIMIZE, 0);
    }

    #endregion
}
