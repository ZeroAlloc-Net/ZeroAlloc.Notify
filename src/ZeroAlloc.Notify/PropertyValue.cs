using System.Runtime.CompilerServices;

namespace ZeroAlloc.Notify;

/// <summary>
/// The old or new value of a property change, held without boxing.
/// </summary>
/// <remarks>
/// <para>
/// A reference-type value is stored as the reference itself. A value type of at most 16 bytes that
/// contains no references, such as <see cref="int"/>, <see cref="double"/>, <see cref="decimal"/>,
/// <see cref="Guid"/>, <see cref="DateTime"/>, an enum, or a <see cref="Nullable{T}"/> of a type of at
/// most 8 bytes such as <c>int?</c> or <c>DateTime?</c>, is stored inline in the struct. Neither case
/// allocates. A larger value type, such as <c>Guid?</c> or <c>decimal?</c>, or one that contains
/// references, is boxed once when the value is created. A <see langword="null"/> is stored as no value
/// at all, so it reads as <see langword="null"/> for every reference type and nullable value type.
/// </para>
/// <para>
/// Read the value with <see cref="TryGet{T}(out T)"/> or <see cref="As{T}"/>, giving the property's
/// declared type, which reads without boxing. A value reads as the types its boxed form would match,
/// whether it is stored inline or boxed: an <see cref="int"/> does not read as <see cref="long"/>, but
/// a non-null <c>int?</c> reads as <see cref="int"/> and an <see cref="int"/> as <c>int?</c>, just as
/// a <c>Guid?</c> and a <see cref="Guid"/> do. Reading an inline value as any type other than its own,
/// such as <see cref="object"/>, an interface or the other side of a nullable, boxes it, as does
/// <see cref="ToObject"/>.
/// </para>
/// <para>
/// The value formats with its own type's formatting: <see cref="ToString(string?, IFormatProvider?)"/>
/// and <see cref="TryFormat"/> pass the format string and the format provider on to the value, so
/// <c>$"{args.NewValue:N2}"</c> and <c>string.Create(CultureInfo.InvariantCulture, ...)</c> format it
/// as they would format the value itself.
/// </para>
/// </remarks>
public readonly struct PropertyValue : ISpanFormattable
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
        // A null, including an empty Nullable<T>, carries no type, so it reads as null for every
        // reference type and nullable value type, as a null reference does.
        if (value is null)
            return default;

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

            // Any other type gets the same answer as a boxed value: object and interfaces match, and
            // so do U and Nullable<U> in either direction, but an int does not read as a long. This
            // boxes, so it allocates only on a read whose type differs from the stored one.
            // An inline value is never null: From stores a null as no value.
            if (tag.Box(_inline) is T boxed)
            {
                value = boxed;
                return true;
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

    /// <summary>
    /// Formats the value with its own <see cref="object.ToString"/>, as
    /// <see cref="ToString(string?, IFormatProvider?)"/> does without a format or a provider.
    /// A <see langword="null"/> formats as an empty string.
    /// </summary>
    public override string ToString() => ToString(null, null);

    /// <summary>
    /// Formats the value with the given format and provider when its type implements
    /// <see cref="IFormattable"/>, and with its own <see cref="object.ToString"/> otherwise. An inline
    /// value is not boxed when its type implements the formatting itself, as the primitives,
    /// <see cref="decimal"/>, <see cref="Guid"/>, <see cref="DateTime"/> and
    /// <see cref="DateTimeOffset"/> do. An enum, or a struct that inherits its formatting, is boxed by
    /// the runtime for the call, and so, on .NET 10 only, is a nullable stored inline such as
    /// <c>int?</c>. A <see langword="null"/> formats as an empty string.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
        => _object switch
        {
            TypeTag tag => tag.Format(_inline, format, formatProvider),
            IFormattable formattable => formattable.ToString(format, formatProvider),
            null => null,
            _ => _object.ToString(),
        } ?? string.Empty;

    /// <summary>
    /// Formats the value into <paramref name="destination"/>, passing <paramref name="format"/> and
    /// <paramref name="provider"/> on to the value. A value whose type implements
    /// <see cref="ISpanFormattable"/> formats without allocating; any other value is formatted with
    /// <see cref="ToString(string?, IFormatProvider?)"/> and copied. A <see langword="null"/> writes nothing.
    /// </summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        switch (_object)
        {
            case TypeTag tag:
                return tag.TryFormat(_inline, destination, out charsWritten, format, provider);
            case ISpanFormattable formattable:
                return formattable.TryFormat(destination, out charsWritten, format, provider);
            case null:
                charsWritten = 0;
                return true;
            default:
                return CopyTo(_object is IFormattable plain ? plain.ToString(ToFormatString(format), provider) : _object.ToString(), destination, out charsWritten);
        }
    }

    private string DescribeType()
        => _object switch
        {
            TypeTag tag => tag.Type.ToString(),
            null => "null",
            _ => _object.GetType().ToString(),
        };

    private static string? ToFormatString(ReadOnlySpan<char> format) => format.IsEmpty ? null : format.ToString();

    private static bool CopyTo(string? text, Span<char> destination, out int charsWritten)
    {
        var span = text.AsSpan();
        if (span.TryCopyTo(destination))
        {
            charsWritten = span.Length;
            return true;
        }

        charsWritten = 0;
        return false;
    }

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

        public abstract object Box(InlineStorage inline);

        public abstract string? Format(InlineStorage inline, string? format, IFormatProvider? provider);

        public abstract bool TryFormat(InlineStorage inline, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider);
    }

    private sealed class TypeTag<T> : TypeTag
    {
        public static readonly TypeTag<T> Instance = new();

        public override Type Type => typeof(T);

        public override object Box(InlineStorage inline) => Read<T>(inline)!;

        // The casts from T to the interfaces compile to constrained calls, which do not box a type
        // that implements the interface itself, the pattern DefaultInterpolatedStringHandler uses.
        public override string? Format(InlineStorage inline, string? format, IFormatProvider? provider)
        {
            var value = Read<T>(inline);
            return value is IFormattable
                ? ((IFormattable)value).ToString(format, provider)
                : value!.ToString();
        }

        public override bool TryFormat(InlineStorage inline, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var value = Read<T>(inline);
            if (value is ISpanFormattable)
                return ((ISpanFormattable)value).TryFormat(destination, out charsWritten, format, provider);

            return CopyTo(Format(inline, ToFormatString(format), provider), destination, out charsWritten);
        }
    }
}
