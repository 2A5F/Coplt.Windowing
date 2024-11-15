using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;
using Coplt.Mathematics;
using static Windows.Win32.PInvoke;

namespace Coplt.Windowing.Win32;

[SupportedOSPlatform("windows10.0")]
public class Win32Monitor : Monitor
{
    internal SWin32Monitor m_monitor;

    internal Win32Monitor(HMONITOR mMonitor)
    {
        m_monitor = new(mMonitor);
    }

    public override string Name => m_monitor.Name;

    public override MonitorInfo Info => m_monitor.Info;

    public override uint2 RawDpi => m_monitor.RawDpi;
    public override double2 ScaleByDpi => m_monitor.ScaleByDpi;
}

[SupportedOSPlatform("windows10.0")]
public struct SWin32Monitor
{
    #region Consts

    public const double StandardDpi = 96;

    #endregion

    internal HMONITOR m_monitor;

    internal SWin32Monitor(HMONITOR mMonitor)
    {
        m_monitor = mMonitor;
    }

    public unsafe string Name
    {
        get
        {
            var info = new MONITORINFOEXW { monitorInfo = { cbSize = (uint)sizeof(MONITORINFOEXW) } };
            var r = GetMonitorInfo(m_monitor, (MONITORINFO*)&info);
            if (r == 0) throw new Win32Exception();
            return info.szDevice.ToString();
        }
    }

    public unsafe MonitorInfo Info
    {
        get
        {
            var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };
            var r = GetMonitorInfo(m_monitor, &info);
            if (r == 0) throw new Win32Exception();
            ref var rect = ref info.rcMonitor;
            var size = (uint2)new int2(rect.right - rect.left, rect.bottom - rect.top);
            var pos = new int2(rect.left, rect.top);
            return new(pos, size);
        }
    }

    public uint2 RawDpi
    {
        get
        {
            var r = GetDpiForMonitor(m_monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY);
            if (r.Succeeded) return new(dpiX, dpiY);
            throw Marshal.GetExceptionForHR(r.Value)!;
        }
    }

    public double2 ScaleByDpi => RawDpi / new double2(StandardDpi);
}
