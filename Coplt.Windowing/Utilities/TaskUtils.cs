using System.Runtime.CompilerServices;

namespace Coplt.Windowing.Utilities;

public static class TaskUtils
{
    #region SwitchToContext

    public static SwitchToContextAwaitable SwitchToContext(this SynchronizationContext ctx) => new(ctx);

    public readonly struct SwitchToContextAwaitable(SynchronizationContext ctx)
    {
        public SwitchToContextAwaiter GetAwaiter() => new(ctx);
    }

    public readonly struct SwitchToContextAwaiter(SynchronizationContext ctx) : INotifyCompletion
    {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            ctx.Send(static continuation => ((Action)continuation!)(), continuation);
        }

        public void GetResult() { }
    }

    #endregion
}
