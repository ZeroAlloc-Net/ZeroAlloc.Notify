using System.Runtime.InteropServices;

namespace ZeroAlloc.Notify.AotSmoke;

/// <summary>
/// A value type that is deliberately not IEquatable, so the generated setter's
/// EqualityComparer falls back to ValueType.Equals.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct SmokeSize
{
    public SmokeSize(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public double Width { get; }
    public double Height { get; }
    public override string ToString() => $"{Width}x{Height}";
}
