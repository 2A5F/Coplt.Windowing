namespace Coplt.Windowing;

public interface IEvent;
interface ICancellableEvent 
{
    public bool Canceled { get;  }

    public void Cancel();
}
