using System.Runtime.Versioning;
using Coplt.Mathematics;

namespace Coplt.Windowing;

public abstract class Window
{
    #region Props

    public abstract Guid Id { get; }
    public abstract WindowSystem WindowSystem { get; }

    public abstract string Title { get; set; }

    public abstract uint2? MinSize { get; set; }

    public abstract uint2? MaxSize { get; set; }

    #endregion

    #region Event

    protected EventBus EventBus { get; set; } = new();

    #region On

    public EventId On<E>(Action<E> handler) => EventBus.On(handler);

    public EventId Once<E>(Action<E> handler) => EventBus.Once(handler);

    public EventId On<E, A>(A arg, Action<E, A> handler) => EventBus.On(arg, handler);

    public EventId Once<E, A>(A arg, Action<E, A> handler) => EventBus.Once(arg, handler);

    #endregion

    #region Emit

    protected void Emit<E>(E ev) => EventBus.Emit(ev);

    #endregion

    #endregion

    #region TryGetHwnd

    /// <summary>
    /// Only windows platform return non null value
    /// </summary>
    public abstract unsafe void* TryGetHwnd { get; }

    #endregion

    #region SetMainWindow

    public abstract void SetMainWindow(bool isMainWindow = true);

    #endregion

    #region Center

    public abstract void Center();

    #endregion

    #region Show

    public abstract void Show();

    #endregion

    #region Hide

    public abstract void Hide();

    #endregion

    #region Rect

    public abstract int2 Position { get; }

    public abstract uint2 Size { get; }

    public abstract int4 Rect { get; }

    public abstract int2 PixelPosition { get; }

    public abstract uint2 PixelSize { get; }

    public abstract int4 PixelRect { get; }

    #endregion

    #region Monitor

    public abstract Monitor Monitor { get; }

    public abstract uint2 RawDpi { get; }
    public abstract double2 ScaleByDpi { get; }

    #endregion

    #region Close

    public abstract void Close();

    #endregion

    #region Minimize Maximize

    public abstract void Restore();
    
    public abstract void Minimize();
    
    public abstract void Maximize();

    #endregion
}
