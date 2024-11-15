using static Windows.Win32.PInvoke;

namespace Coplt.Windowing.Win32;

internal enum Messages : uint
{
    DispatchingWorkItems = WM_USER,
    Exit,
}
