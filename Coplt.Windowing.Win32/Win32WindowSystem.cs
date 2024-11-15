using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Coplt.Dropping;
using Coplt.Windowing.Utilities;
using static Windows.Win32.PInvoke;

namespace Coplt.Windowing.Win32;

[Dropping(Unmanaged = true)]
[SupportedOSPlatform("windows10.0")]
public partial class Win32WindowSystem : WindowSystem
{
    #region Fields

    private readonly ConcurrentQueue<Action> m_work_items = new();
    private Thread? m_thread;
    private GCHandle m_handle;
    private readonly HWND m_hwnd;
    private bool m_running = true;
    private Win32Window? m_main_window;
    internal readonly Dictionary<Guid, Win32Window> m_windows = new();

    #endregion

    #region Static

    internal static readonly ushort s_atom;

    static unsafe Win32WindowSystem()
    {
        fixed (char* name = "Coplt.Windowing.Win32.Win32WindowSystem")
        {
            var wnd_class = new WNDCLASSEXW
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = &WndProc,
                hInstance = GetModuleHandle(default(PCWSTR)),
                lpszClassName = name,
            };

            s_atom = RegisterClassEx(in wnd_class);
            if (s_atom == 0) throw new Win32Exception();
        }
    }

    #endregion

    #region Ctor

    public unsafe Win32WindowSystem()
    {
        m_handle = GCHandle.Alloc(this, GCHandleType.Weak);
        m_hwnd = CreateWindowEx(
            default, (char*)s_atom,
            null, default,
            0, 0, 0, 0,
            default, default, default,
            (void*)GCHandle.ToIntPtr(m_handle)
        );
        if (m_hwnd == default) throw new Win32Exception();
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

    #region Monitor

    public override Monitor GetPrimaryMonitor() => GetWin32PrimaryMonitor();

    public Win32Monitor GetWin32PrimaryMonitor()
    {
        var monitor = MonitorFromPoint(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        return new Win32Monitor(monitor);
    }

    public SWin32Monitor GetWin32PrimaryMonitorS()
    {
        var monitor = MonitorFromPoint(default, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
        return new SWin32Monitor(monitor);
    }

    #endregion

    #region Running

    public bool Running => m_running;

    #endregion

    #region Exit

    public void Exit()
    {
        m_running = false;
        PostMessage(m_hwnd, (uint)Messages.Exit, default, default);
    }

    #endregion

    #region WorkItem

    public void EnqueueWorkItem(Action action)
    {
        m_work_items.Enqueue(action);
        PostMessage(m_hwnd, (uint)Messages.DispatchingWorkItems, default, default);
    }

    private void DoDispatchingWorkItems()
    {
        if (m_work_items.TryDequeue(out var action)) action();
    }

    #endregion

    #region Event

    internal new void Emit<E>(E ev) => base.Emit(ev);

    #endregion

    #region MessageLoop

    public override void MessageLoop(CancellationToken cancellationToken)
    {
        m_thread = Thread.CurrentThread;
        try
        {
            re:
            if (!m_running || cancellationToken.IsCancellationRequested) return;
            var r = GetMessage(out var msg, default, default, default);
            if (!r) throw new Win32Exception();
            TranslateMessage(in msg);
            DispatchMessage(in msg);
            goto re;
        }
        finally
        {
            m_thread = null;
        }
    }

    #endregion

    #region WndProc

    private unsafe LRESULT WndProcInst(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg >= WM_USER)
        {
            switch ((Messages)msg)
            {
                case Messages.DispatchingWorkItems:
                    DoDispatchingWorkItems();
                    return default;
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
            if (handle.Target is Win32WindowSystem ws) return ws.WndProcInst(hWnd, msg, wParam, lParam);
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
        catch (Exception e)
        {
            try
            {
                EmitUnhandledException(e);
            }
            catch
            {
                // ignored
            }
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    #endregion

    #region SwitchToMessageThread

    public SwitchToMessageThreadAwaitable SwitchToMessageThread() => new(this);

    public readonly struct SwitchToMessageThreadAwaitable(Win32WindowSystem self)
    {
        public SwitchToMessageThreadAwaiter GetAwaiter() => new(self);
    }

    public readonly struct SwitchToMessageThreadAwaiter(Win32WindowSystem self) : INotifyCompletion
    {
        public bool IsCompleted => self.m_thread == null || Thread.CurrentThread == self.m_thread;

        public void OnCompleted(Action continuation)
        {
            self.EnqueueWorkItem(continuation);
        }

        public void GetResult()
        {
            if (self.m_thread == null) throw new ArgumentException("The message loop has stopped");
        }
    }

    #endregion

    #region CreateWindow

    public override Window CreateWindowSync(WindowOptions options)
    {
        if (Thread.CurrentThread == m_thread) return new Win32Window(this, options);
        var value_task = CreateWindow(options, false);
        if (value_task.IsCompleted) return value_task.Result;
        var task = value_task.AsTask();
        task.Wait();
        return task.Result;
    }

    public override ValueTask<Window> CreateWindow(WindowOptions options) => CreateWindow(options, true);

    private async ValueTask<Window> CreateWindow(WindowOptions options, bool should_back_thread)
    {
        if (Thread.CurrentThread == m_thread) return new Win32Window(this, options);
        var ctx = SynchronizationContext.Current;
        await SwitchToMessageThread();
        Win32Window window;
        try
        {
            window = new Win32Window(this, options);
        }
        catch
        {
            if (ctx == null) await Task.Yield();
            else
            {
                await ctx.SwitchToContext();
                if (Thread.CurrentThread == m_thread) await Task.Yield();
            }
            throw;
        }
        if (!should_back_thread) return window;
        if (ctx == null) await Task.Yield();
        else
        {
            await ctx.SwitchToContext();
            if (Thread.CurrentThread == m_thread) await Task.Yield();
        }
        return window;
    }

    #endregion

    #region TryGetWindow

    public override Window? TryGetWindow(Guid id) => m_windows.GetValueOrDefault(id);

    #endregion

    #region UnhandledException

    internal new static void EmitUnhandledException(Exception exception) =>
        WindowSystem.EmitUnhandledException(exception);

    internal new static void ExceptionSafeScope<A>(A arg, Action<A> action) =>
        WindowSystem.ExceptionSafeScope(arg, action);

    internal new static R ExceptionSafeScope<A, R>(A arg, Func<A, R> func, Func<A, R> defv) =>
        WindowSystem.ExceptionSafeScope(arg, func, defv);

    #endregion

    #region SetMainWindow

    public void SetMainWindow(Win32Window window, bool isMainWindow = true)
    {
        if (isMainWindow) m_main_window = window;
        else if (m_main_window == window) m_main_window = null;
    }

    #endregion

    #region OnWindowClosed

    internal void OnWindowClosed(Win32Window window)
    {
        m_windows.Remove(window.Id);
        if (window != m_main_window) return;
        m_main_window = null;
        Exit();
    }

    #endregion
}
