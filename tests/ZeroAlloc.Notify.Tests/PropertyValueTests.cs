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
        Assert.False(value.TryGet<int?>(out _));
        Assert.Throws<InvalidCastException>(() => value.As<long>());
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
    public void NonNullNullable_MatchesOnlyItsExactType()
    {
        var value = PropertyValue.From<int?>(5);
        Assert.False(value.TryGet<long?>(out _));
        Assert.False(value.TryGet<int>(out _));
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
        Assert.Equal("", value.ToString());
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
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal("42", PropertyValue.From(42).ToString());
            Assert.Equal("1.5", PropertyValue.From(1.5).ToString());
            Assert.Equal("Green", PropertyValue.From(Color.Green).ToString());
            Assert.Equal("text", PropertyValue.From("text").ToString());
            Assert.Equal("", PropertyValue.From<int?>(null).ToString());
            Assert.Equal("", PropertyValue.From<string?>(null).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
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
        AllocationGate.AssertBudget(0, 10_000, () =>
        {
            _ = PropertyValue.From(42).As<int>();
            _ = PropertyValue.From(12.5m).As<decimal>();
            _ = PropertyValue.From(guid).As<Guid>();
            _ = PropertyValue.From<int?>(7).As<int?>();
            _ = PropertyValue.From(text).As<string>();
        }, "PropertyValue.From and As for inline and reference values");
    }
}
