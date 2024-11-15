namespace Coplt.Windowing;

public abstract class WindowSystem
{
    #region UnhandledException

    public static event Action<Exception>? UnhandledException;

    protected internal static void EmitUnhandledException(Exception exception) => UnhandledException?.Invoke(exception);

    protected internal static void ExceptionSafeScope<A>(A arg, Action<A> action)
    {
        try
        {
            action(arg);
        }
        catch (Exception e)
        {
            try
            {
                UnhandledException?.Invoke(e);
            }
            catch
            {
                // ignored
            }
        }
    }

    protected internal static R ExceptionSafeScope<A, R>(A arg, Func<A, R> func, Func<A, R> defv)
    {
        try
        {
            return func(arg);
        }
        catch (Exception e)
        {
            try
            {
                UnhandledException?.Invoke(e);
            }
            catch
            {
                // ignored
            }
        }
        return defv(arg);
    }

    #endregion

    #region Monitor

    public abstract Monitor GetPrimaryMonitor();

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

    #region MessageLoop

    public void MessageLoop() => MessageLoop(CancellationToken.None);
    public abstract void MessageLoop(CancellationToken cancellationToken);

    #endregion

    #region CreateWindow

    public abstract Window CreateWindowSync(WindowOptions options);
    public abstract ValueTask<Window> CreateWindow(WindowOptions options);

    #endregion

    #region TryGetWindow

    public abstract Window? TryGetWindow(Guid id);

    #endregion
}
