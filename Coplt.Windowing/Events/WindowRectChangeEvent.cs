using Coplt.Mathematics;

namespace Coplt.Windowing;

public readonly record struct WindowRectChangeEvent(
    Window Window,
    int2 Position,
    uint2 Size,
    int2 PixelPosition,
    uint2 PixelSize,
    double2 ScaleByDpi,
    int2 OldPosition,
    uint2 OldSize,
    int2 OldPixelPosition,
    uint2 OldPixelSize,
    double2 OldScaleByDpi,
    bool PositionChanged,
    bool SizeChanged,
    bool PixelPositionChanged,
    bool PixelSizeChanged,
    bool ScaleByDpiChanged
) : IEvent;

public readonly record struct WindowPositionChangeEvent(
    Window Window,
    int2 Position,
    int2 PixelPosition,
    int2 OldPosition,
    int2 OldPixelPosition
) : IEvent;

public readonly record struct WindowSizeChangeEvent(
    Window Window,
    uint2 Size,
    uint2 PixelSize,
    uint2 OldSize,
    uint2 OldPixelSize
) : IEvent;

public readonly record struct WindowDpiChangeEvent(
    Window Window,
    double2 ScaleByDpi,
    double2 OldScaleByDpi
) : IEvent;
