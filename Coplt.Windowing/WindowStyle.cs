namespace Coplt.Windowing;

public enum WindowStyle
{
    Common,
    Borderless,
}

[Flags]
public enum WindowBlur
{
    None,
    Blur = 1 << 0,
    /// <summary>
    /// Windows Only
    /// </summary>
    Mica = 1 << 1 | Blur,
    /// <summary>
    /// Windows Only
    /// </summary>
    MicaAlt = 1 << 2 | Mica,
}
