using Coplt.Mathematics;

namespace Coplt.Windowing;

public record struct WindowOptions()
{
    public string? Title { get; set; }
    public uint2? Size { get; set; }
    public uint2? MinSize { get; set; }
    public uint2? MaxSize { get; set; }
    public int2? Position { get; set; }
    public bool Show { get; set; } = true;
    public bool Maximize { get; set; } = false;
    public bool Minimize { get; set; } = false;
    public bool Resizeable { get; set; } = true;
    public bool Maximizable { get; set; } = true;
    public bool Minimizable { get; set; } = true;
    /// <summary>
    /// Closing the main window will automatically exit the message loop
    /// </summary>
    public bool MainWindow { get; set; } = false;
    /// <summary>
    /// Ignore when <see cref="Position"/> is not null
    /// </summary>
    public bool Centered { get; set; } = false;
    public WindowStyle Style { get; set; } = WindowStyle.Common;
    public WindowBlur Blur { get; set; } = WindowBlur.None;

    public WindowOptions(string title, uint width, uint height) : this(title, new(width, height)) { }

    public WindowOptions(string title, uint2 size) : this()
    {
        Title = title;
        Size = size;
    }
}
