namespace Coplt.Windowing;

public abstract record CancellableEvent : ICancellableEvent
{
    public bool Canceled { get; protected set; }
    public void Cancel() => Canceled = true;
}
