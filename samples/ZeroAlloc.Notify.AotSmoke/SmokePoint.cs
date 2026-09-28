using System;
using System.Runtime.InteropServices;

namespace ZeroAlloc.Notify.AotSmoke;

/// <summary>An IEquatable value type used as an observable property and a nullable one.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct SmokePoint : IEquatable<SmokePoint>
{
    public SmokePoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    public bool Equals(SmokePoint other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is SmokePoint other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
}
