using System.Globalization;
using System.Runtime.InteropServices;
using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.Notify.Tests;

public class PropertyValueTests
{
    private enum Color { Red, Green }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Small(int X, int Y);

    private readonly record struct WithReference(string Text, int Number);

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Large(long A, long B, long C);

    private sealed class Plain
    {
        public override string ToString() => "plain";
    }

    // Keeps a measured allocation alive, so the JIT cannot remove it.
    private static object? s_sink;
    private static PropertyValue s_valueSink;

    private static CultureInfo CommaDecimalCulture()
    {
        // Built from the invariant culture, so it works with invariant globalization too.
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        return culture;
    }

    private static string Format(PropertyValue value, string format, IFormatProvider? provider)
    {
        Span<char> buffer = stackalloc char[64];
        Assert.True(value.TryFormat(buffer, out var written, format, provider));
        return buffer[..written].ToString();
    }

    [Fact]
    public void Int_RoundTrips()
    {
        var value = PropertyValue.From(42);
        Assert.True(value.TryGet<int>(out var read));
        Assert.Equal(42, read);
        Assert.Equal(42, value.As<int>());
    }

    [Fact]
    public void InlineValue_DoesNotReadAsAnotherValueType()
    {
        var value = PropertyValue.From(42);
        Assert.False(value.TryGet<long>(out _));
        Assert.False(value.TryGet<uint>(out _));
        Assert.False(value.TryGet<long?>(out _));
        Assert.Throws<InvalidCastException>(() => value.As<long>());
    }

    [Fact]
    public void InlineValue_ReadsAsItsNullable()
    {
        var value = PropertyValue.From(42);
        Assert.True(value.TryGet<int?>(out var read));
        Assert.Equal(42, read);
        Assert.Equal(42, value.As<int?>());
    }

    [Fact]
    public void InlineValue_ReadsAsObjectOrInterfaceByBoxing()
    {
        var value = PropertyValue.From(42);
        Assert.True(value.TryGet<object>(out var boxed));
        Assert.Equal(42, boxed);
        Assert.Equal(42, value.As<IComparable>());
        Assert.False(value.TryGet<string>(out _));
        Assert.Equal(42, value.ToObject());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void Long_RoundTrips(long input) => Assert.Equal(input, PropertyValue.From(input).As<long>());

    [Fact]
    public void SixteenByteValues_RoundTrip()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, PropertyValue.From(guid).As<Guid>());
        Assert.Equal(12.345m, PropertyValue.From(12.345m).As<decimal>());
        var offset = new DateTimeOffset(2026, 11, 21, 10, 30, 0, TimeSpan.FromHours(1));
        Assert.Equal(offset, PropertyValue.From(offset).As<DateTimeOffset>());
        Assert.Equal(new Small(1, 2), PropertyValue.From(new Small(1, 2)).As<Small>());
    }

    [Fact]
    public void Enum_RoundTrips() => Assert.Equal(Color.Green, PropertyValue.From(Color.Green).As<Color>());

    [Fact]
    public void Nullable_RoundTripsWithAndWithoutAValue()
    {
        Assert.Equal(5, PropertyValue.From<int?>(5).As<int?>());
        Assert.Null(PropertyValue.From<int?>(null).As<int?>());
        Assert.Null(PropertyValue.From<int?>(null).ToObject());
        Assert.True(PropertyValue.From<int?>(null).TryGet<object>(out var asObject));
        Assert.Null(asObject);
    }

    [Fact]
    public void NullNullable_ReadsAsNullForEveryNullableAndReferenceType()
    {
        var value = PropertyValue.From<int?>(null);
        Assert.True(value.TryGet<long?>(out var asLong));
        Assert.Null(asLong);
        Assert.True(value.TryGet<string>(out var asString));
        Assert.Null(asString);
        Assert.False(value.TryGet<int>(out _));
    }

    [Fact]
    public void NonNullNullable_ReadsAsItsUnderlyingTypeButNotAnother()
    {
        var value = PropertyValue.From<int?>(5);
        Assert.True(value.TryGet<int>(out var read));
        Assert.Equal(5, read);
        Assert.Equal(5, value.As<int>());
        Assert.False(value.TryGet<long?>(out _));
        Assert.False(value.TryGet<long>(out _));
    }

    [Fact]
    public void InlineAndBoxedNullables_ReadTheSameWay()
    {
        // int? is stored inline and Guid? is boxed; a caller cannot see the difference.
        var number = PropertyValue.From<int?>(5);
        var guid = Guid.NewGuid();
        var boxedGuid = PropertyValue.From<Guid?>(guid);

        Assert.Equal(5, number.As<int>());
        Assert.Equal(guid, boxedGuid.As<Guid>());
        Assert.Equal(5, number.As<int?>());
        Assert.Equal(guid, boxedGuid.As<Guid?>());
        Assert.Equal(5, PropertyValue.From(5).As<int?>());
        Assert.Equal(guid, PropertyValue.From(guid).As<Guid?>());
        Assert.False(number.TryGet<long>(out _));
        Assert.False(boxedGuid.TryGet<decimal>(out _));
    }

    [Fact]
    public void InlineValue_ReadsAsItsOwnTypeWithoutAllocating()
    {
        var value = PropertyValue.From<int?>(5);
        var plain = PropertyValue.From(5);
        AllocationGate.AssertBudget(0, 10_000, () => _ = value.As<int?>(), "int? read as int?");
        AllocationGate.AssertBudget(0, 10_000, () => _ = plain.As<int>(), "int read as int");
    }

    [Fact]
    public void NullablesLargerThanTheInlineStorage_RoundTripBoxed()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, PropertyValue.From<Guid?>(guid).As<Guid?>());
        Assert.Equal(1.5m, PropertyValue.From<decimal?>(1.5m).As<decimal?>());
        Assert.Null(PropertyValue.From<Guid?>(null).As<Guid?>());
    }

    [Fact]
    public void ReferenceValue_RoundTripsAsItsOwnTypeAndBaseTypes()
    {
        var value = PropertyValue.From("text");
        Assert.Equal("text", value.As<string>());
        Assert.Equal("text", value.As<object>());
        Assert.Equal("text", value.As<IComparable<string>>());
        Assert.False(value.TryGet<Uri>(out _));
        Assert.False(value.TryGet<int>(out _));
        Assert.Same(value.As<string>(), value.ToObject());
    }

    [Fact]
    public void NullReference_ReadsAsNullForReferenceAndNullableTypes()
    {
        var value = PropertyValue.From<string?>(null);
        Assert.True(value.TryGet<string>(out var text));
        Assert.Null(text);
        Assert.True(value.TryGet<int?>(out var number));
        Assert.Null(number);
        Assert.False(value.TryGet<int>(out _));
        Assert.Null(value.ToObject());
    }

    [Fact]
    public void Default_ReadsAsNull()
    {
        var value = default(PropertyValue);
        Assert.Null(value.As<object>());
        Assert.Equal("", value.ToString(null, CultureInfo.InvariantCulture));
        Assert.Equal("", ((object)value).ToString());
    }

    [Fact]
    public void StructsThatCannotBeInlined_RoundTripBoxed()
    {
        Assert.Equal(new WithReference("a", 1), PropertyValue.From(new WithReference("a", 1)).As<WithReference>());
        Assert.Equal(new Large(1, 2, 3), PropertyValue.From(new Large(1, 2, 3)).As<Large>());
        Assert.False(PropertyValue.From(new Large(1, 2, 3)).TryGet<Small>(out _));
    }

    [Fact]
    public void ToString_FormatsTheValue()
    {
        var invariant = CultureInfo.InvariantCulture;
        Assert.Equal("42", PropertyValue.From(42).ToString(null, invariant));
        Assert.Equal("1.5", PropertyValue.From(1.5).ToString(null, invariant));
        Assert.Equal("Green", PropertyValue.From(Color.Green).ToString(null, invariant));
        Assert.Equal("text", PropertyValue.From("text").ToString(null, invariant));
        Assert.Equal("", PropertyValue.From<int?>(null).ToString(null, invariant));
        Assert.Equal("", PropertyValue.From<string?>(null).ToString(null, invariant));
    }

    [Fact]
    public void Interpolation_HonoursTheFormatString()
    {
        Assert.Equal("1.50", string.Create(CultureInfo.InvariantCulture, $"{PropertyValue.From(1.5m):N2}"));
        Assert.Equal("1,234.50", string.Create(CultureInfo.InvariantCulture, $"{PropertyValue.From(1234.5):N2}"));
        Assert.Equal("2026-11-21", $"{PropertyValue.From(new DateTime(2026, 11, 21, 10, 30, 0, DateTimeKind.Utc)):yyyy-MM-dd}");
        Assert.Equal("002A", $"{PropertyValue.From(42):X4}");
        Assert.Equal("1", $"{PropertyValue.From(Color.Green):D}");
    }

    [Fact]
    public void Formatting_HonoursTheProviderUnderACommaDecimalCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CommaDecimalCulture();
            var value = PropertyValue.From(1.5m);

            // ToString() without arguments, as an object-typed consumer such as data binding calls it.
            Assert.Equal("1,5", ((object)value).ToString());
            Assert.Equal("1,5", $"{value}");
            Assert.Equal("1.5", string.Create(CultureInfo.InvariantCulture, $"{value}"));
            Assert.Equal("1.5", string.Format(CultureInfo.InvariantCulture, "{0}", value));
            Assert.Equal("1.50", string.Format(CultureInfo.InvariantCulture, "{0:N2}", value));
            Assert.Equal("1.5", value.ToString(null, CultureInfo.InvariantCulture));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Formatting_HonoursFormatAndProviderForBoxedValues()
    {
        // Guid? and decimal? are boxed, so they format through the boxed value's own interfaces.
        var guid = Guid.NewGuid();
        Assert.Equal(guid.ToString("N"), $"{PropertyValue.From<Guid?>(guid):N}");
        Assert.Equal("1.50", string.Create(CultureInfo.InvariantCulture, $"{PropertyValue.From<decimal?>(1.5m):N2}"));
        Assert.Equal("1,50", PropertyValue.From<decimal?>(1.5m).ToString("N2", CommaDecimalCulture()));
        Assert.Equal(guid.ToString("D"), Format(PropertyValue.From<Guid?>(guid), "D", null));
    }

    [Fact]
    public void TryFormat_WritesTheFormattedValue()
    {
        var invariant = CultureInfo.InvariantCulture;
        Assert.Equal("1.50", Format(PropertyValue.From(1.5m), "N2", invariant));
        Assert.Equal("1,50", Format(PropertyValue.From(1.5m), "N2", CommaDecimalCulture()));
        Assert.Equal("Green", Format(PropertyValue.From(Color.Green), "", invariant));
        Assert.Equal("text", Format(PropertyValue.From("text"), "", invariant));
        Assert.Equal("plain", Format(PropertyValue.From(new Plain()), "", invariant));
        Assert.Equal("(1, 2)", Format(PropertyValue.From((1, 2)), "", invariant));
        Assert.Equal("", Format(PropertyValue.From<int?>(null), "", invariant));
        Assert.Equal("", Format(default, "N2", invariant));
    }

    [Fact]
    public void TryFormat_ReturnsFalseWhenTheDestinationIsTooSmall()
    {
        Span<char> buffer = stackalloc char[2];
        Assert.False(PropertyValue.From(123456).TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
        Assert.False(PropertyValue.From("text").TryFormat(buffer, out written, default, null));
        Assert.Equal(0, written);
        Assert.False(PropertyValue.From(new Plain()).TryFormat(buffer, out written, default, null));
        Assert.Equal(0, written);
    }

    [Fact]
    public void ToStringWithFormat_FormatsReferenceAndNullValues()
    {
        Assert.Equal("text", PropertyValue.From("text").ToString("N2", CultureInfo.InvariantCulture));
        Assert.Equal("plain", PropertyValue.From(new Plain()).ToString(null, null));
        Assert.Equal("", PropertyValue.From<string?>(null).ToString("N2", null));
        Assert.Equal("Small { X = 1, Y = 2 }", PropertyValue.From(new Small(1, 2)).ToString("N2", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ToString_AllocatesOnlyTheResultString()
    {
        var number = 123456;
        var amount = 12.5m;
        var guid = Guid.NewGuid();
        var date = new DateTime(2026, 11, 21, 10, 30, 0, DateTimeKind.Utc);
        var invariant = CultureInfo.InvariantCulture;
        AllocationGate.AssertNoMoreThan(10_000,
            () => s_sink = number.ToString(invariant),
            () => s_sink = PropertyValue.From(number).ToString(null, invariant),
            "PropertyValue.ToString for an int");
        AllocationGate.AssertNoMoreThan(10_000,
            () => s_sink = amount.ToString("N2", invariant),
            () => s_sink = PropertyValue.From(amount).ToString("N2", invariant),
            "PropertyValue.ToString for a decimal");
        AllocationGate.AssertNoMoreThan(10_000,
            () => s_sink = guid.ToString("D", invariant),
            () => s_sink = PropertyValue.From(guid).ToString("D", invariant),
            "PropertyValue.ToString for a Guid");
        AllocationGate.AssertNoMoreThan(10_000,
            () => s_sink = date.ToString("O", invariant),
            () => s_sink = PropertyValue.From(date).ToString("O", invariant),
            "PropertyValue.ToString for a DateTime");
    }

    [Fact]
    public void TryFormat_InlineValues_DoesNotAllocate()
    {
        var guid = Guid.NewGuid();
        var date = new DateTime(2026, 11, 21, 10, 30, 0, DateTimeKind.Utc);
        var offset = new DateTimeOffset(date);
        var invariant = CultureInfo.InvariantCulture;
        AllocationGate.AssertBudget(0, 10_000, () =>
        {
            Span<char> buffer = stackalloc char[64];
            _ = PropertyValue.From(123456).TryFormat(buffer, out _, "N0", invariant);
            _ = PropertyValue.From(12.5m).TryFormat(buffer, out _, "N2", invariant);
            _ = PropertyValue.From(1.5).TryFormat(buffer, out _, default, invariant);
            _ = PropertyValue.From(guid).TryFormat(buffer, out _, "N", invariant);
            _ = PropertyValue.From(date).TryFormat(buffer, out _, "O", invariant);
            _ = PropertyValue.From(offset).TryFormat(buffer, out _, "O", invariant);
        }, "PropertyValue.TryFormat for inline values");
    }

    [Fact]
    public void TryFormat_InlineNullable_BoxesOnlyOnDotNet10()
    {
#if NET11_0_OR_GREATER
        const long expected = 0;
#else
        // The .NET 10 JIT boxes a Nullable<T> for the interface call that formats it: one 24 B box
        // of the int. The .NET 11 JIT calls the underlying value's TryFormat without boxing.
        const long expected = 24;
#endif
        var invariant = CultureInfo.InvariantCulture;
        Assert.Equal(expected, AllocationGate.MeasureBytesPerCall(10_000, () =>
        {
            Span<char> buffer = stackalloc char[16];
            _ = PropertyValue.From<int?>(7).TryFormat(buffer, out _, default, invariant);
        }));
    }

    [Fact]
    public void As_WrongType_NamesBothTypes()
    {
        var ex = Assert.Throws<InvalidCastException>(() => PropertyValue.From(42).As<string>());
        Assert.Contains("System.Int32", ex.Message, StringComparison.Ordinal);
        Assert.Contains("System.String", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void As_WrongTypeForAReferenceValue_NamesBothTypes()
    {
        var ex = Assert.Throws<InvalidCastException>(() => PropertyValue.From("text").As<int>());
        Assert.Contains("System.String", ex.Message, StringComparison.Ordinal);
        Assert.Contains("System.Int32", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void As_ValueTypeForANull_SaysNull()
    {
        var ex = Assert.Throws<InvalidCastException>(() => PropertyValue.From<string?>(null).As<int>());
        Assert.Contains("a null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromAndRead_InlineAndReferenceValues_DoNotAllocate()
    {
        var text = "text";
        var guid = Guid.NewGuid();
        var date = new DateTime(2026, 11, 21, 10, 30, 0, DateTimeKind.Utc);
        var offset = new DateTimeOffset(date);
        AllocationGate.AssertBudget(0, 10_000, () =>
        {
            _ = PropertyValue.From(42).As<int>();
            _ = PropertyValue.From(12.5m).As<decimal>();
            _ = PropertyValue.From(guid).As<Guid>();
            _ = PropertyValue.From(date).As<DateTime>();
            _ = PropertyValue.From(offset).As<DateTimeOffset>();
            _ = PropertyValue.From(Color.Green).As<Color>();
            _ = PropertyValue.From<int?>(7).As<int?>();
            _ = PropertyValue.From<DateTime?>(date).As<DateTime?>();
            _ = PropertyValue.From(text).As<string>();
        }, "PropertyValue.From and As for inline and reference values");
    }

    [Fact]
    public void From_ValueTooLargeToInline_BoxesOncePerValue()
    {
        var guid = Guid.NewGuid();

        // One box of the Guid on 64-bit: an 8 B object header, an 8 B method table pointer and the
        // 16 B value. Reading it back as its own type does not allocate again.
        Assert.Equal(32, AllocationGate.MeasureBytesPerCall(10_000, () => s_valueSink = PropertyValue.From<Guid?>(guid)));
        Assert.Equal(32, AllocationGate.MeasureBytesPerCall(10_000, () =>
        {
            s_valueSink = PropertyValue.From<Guid?>(guid);
            _ = s_valueSink.As<Guid?>();
            _ = s_valueSink.As<Guid>();
        }));
    }
}
