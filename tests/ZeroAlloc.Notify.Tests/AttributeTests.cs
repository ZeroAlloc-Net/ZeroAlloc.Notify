using ZeroAlloc.Notify;

namespace ZeroAlloc.Notify.Tests;

public class AttributeTests
{
    [Fact]
    public void AllAttributes_AreDefinedWithCorrectTargets()
    {
        Assert.NotNull(typeof(NotifyPropertyChangedAsyncAttribute));
        Assert.NotNull(typeof(NotifyPropertyChangingAsyncAttribute));
        Assert.NotNull(typeof(NotifyCollectionChangedAsyncAttribute));
        Assert.NotNull(typeof(NotifyDataErrorInfoAsyncAttribute));
        Assert.NotNull(typeof(ObservablePropertyAttribute));
        Assert.NotNull(typeof(InvokeSequentiallyAttribute));
    }

    [Fact]
    public void NotifyPropertyChangedAsyncAttribute_TargetsClass()
    {
        var usage = typeof(NotifyPropertyChangedAsyncAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.False(usage.AllowMultiple);
        Assert.False(usage.Inherited);
    }

    [Fact]
    public void ObservablePropertyAttribute_TargetsField()
    {
        var usage = typeof(ObservablePropertyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Field));
    }

    [Fact]
    public void InvokeSequentiallyAttribute_TargetsClassAndField()
    {
        var usage = typeof(InvokeSequentiallyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Field));
    }
}
