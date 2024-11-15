using Coplt.Mathematics;

namespace Coplt.Windowing;

public abstract class Monitor
{
    public abstract string Name { get; }

    public abstract MonitorInfo Info { get; }

    public abstract uint2 RawDpi { get; }

    public abstract double2 ScaleByDpi { get; }
}

public record struct MonitorInfo(int2 Position, uint2 Size);
