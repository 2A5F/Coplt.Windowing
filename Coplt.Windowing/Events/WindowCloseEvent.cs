namespace Coplt.Windowing;

public record WindowCloseEvent(Window Window) : CancellableEvent;
