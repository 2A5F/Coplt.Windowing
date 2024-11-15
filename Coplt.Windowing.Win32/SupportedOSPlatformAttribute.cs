#if NETSTANDARD

namespace System.Runtime.Versioning;

internal abstract class OSPlatformAttribute : Attribute
{
    private protected OSPlatformAttribute(string platformName) => PlatformName = platformName;

    public string PlatformName { get; }
}

[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct |
    AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property |
    AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface, AllowMultiple = true,
    Inherited = false)]
internal sealed class SupportedOSPlatformAttribute(string platformName) : OSPlatformAttribute(platformName);


#endif
