using System.Runtime.CompilerServices;

namespace ZeroAlloc.Notify;

/// <summary>
/// The old or new value of a property change, held without boxing.
/// </summary>
/// <remarks>
/// <para>
/// A reference-type value is stored as the reference itself. A value type of at most 16 bytes that
/// contains no references, such as <see cref="int"/>, <see cref="double"/>, <see cref="decimal"/>,
/// <see cref="Guid"/>, <see cref="DateTime"/>, an enum or a <see cref="Nullable{T}"/> of these, is
/// stored inline in the struct. Neither case allocates. A larger value type, or one that contains
/// references, is boxed once when the value is created.
/// </para>
/// <para>
/// Read the value with <see cref="TryGet{T}(out T)"/> or <see cref="As{T}"/>, giving the property's
/// declared type. An inline value matches only its exact type: an <see cref="int"/> property does not
/// read as <see cref="long"/>. Asking for a reference type such as <see cref="object"/> or an
/// interface boxes an inline value, as does <see cref="ToObject"/>.
/// </para>
/// </remarks>
public readonly struct PropertyValue
{
    // A reference-type value, a boxed value type, or the tag of the type stored in _inline.
    private readonly object? _object;
    private readonly InlineStorage _inline;

    private PropertyValue(object? value, InlineStorage inline)
    {
        _object = value;
        _inline = inline;
    }

    /// <summary>Creates a value from <paramref name="value"/>, boxing only the value types that cannot be stored inline.</summary>
    public static PropertyValue From<T>(T value)
    {
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>() && Unsafe.SizeOf<T>() <= Unsafe.SizeOf<InlineStorage>())
        {
            var inline = default(InlineStorage);
            Unsafe.WriteUnaligned(ref Unsafe.As<InlineStorage, byte>(ref inline), value);
            return new PropertyValue(TypeTag<T>.Instance, inline);
        }

        return new PropertyValue(value, default);
    }

    /// <summary>
    /// Reads the value as <typeparamref name="T"/>. Returns <see langword="false"/> when the value is
    /// not a <typeparamref name="T"/>. A <see langword="null"/> value reads as <see langword="null"/>
    /// for every reference type and nullable value type.
    /// </summary>
    public bool TryGet<T>(out T? value)
    {
        if (_object is TypeTag tag)
        {
            if (tag is TypeTag<T>)
            {
                value = Read<T>(_inline);
                return true;
            }

            // Only a reference type, such as object or an interface, can hold a boxed inline value.
            if (!typeof(T).IsValueType)
            {
                var boxed = tag.Box(_inline);
                if (boxed is null || boxed is T)
                {
                    value = (T?)boxed;
                    return true;
                }
            }

            value = default;
            return false;
        }

        if (_object is T match)
        {
            value = match;
            return true;
        }

        value = default;
        return _object is null && default(T) is null;
    }

    /// <summary>Reads the value as <typeparamref name="T"/>.</summary>
    /// <exception cref="InvalidCastException">The value is not a <typeparamref name="T"/>.</exception>
    public T? As<T>()
        => TryGet<T>(out var value)
            ? value
            : throw new InvalidCastException($"The property value is a {DescribeType()}, not a {typeof(T)}.");

    /// <summary>Returns the value as an object. This boxes a value type that is stored inline.</summary>
    public object? ToObject() => _object is TypeTag tag ? tag.Box(_inline) : _object;

    /// <summary>Formats the value with its own <see cref="object.ToString"/>, without boxing an inline value.</summary>
    public override string ToString()
        => (_object is TypeTag tag ? tag.Format(_inline) : _object?.ToString()) ?? string.Empty;

    private string DescribeType()
        => _object switch
        {
            TypeTag tag => tag.Type.ToString(),
            null => "null",
            _ => _object.GetType().ToString(),
        };

    // The inline storage is passed by value: 16 bytes, and it keeps the readonly field unaliased.
    private static T Read<T>(InlineStorage inline)
        => Unsafe.ReadUnaligned<T>(ref Unsafe.As<InlineStorage, byte>(ref inline));

    [InlineArray(2)]
    private struct InlineStorage
    {
        private ulong _element;
    }

    /// <summary>Identifies the type of a value stored inline. One instance per type, never per value.</summary>
    private abstract class TypeTag
    {
        public abstract Type Type { get; }

        public abstract object? Box(InlineStorage inline);

        public abstract string? Format(InlineStorage inline);
    }

    private sealed class TypeTag<T> : TypeTag
    {
        public static readonly TypeTag<T> Instance = new();

        public override Type Type => typeof(T);

        public override object? Box(InlineStorage inline) => Read<T>(inline);

        public override string? Format(InlineStorage inline) => Read<T>(inline)?.ToString();
    }
}
