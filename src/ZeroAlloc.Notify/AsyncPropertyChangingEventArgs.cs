namespace ZeroAlloc.Notify;

/// <summary>
/// The arguments of <see cref="INotifyPropertyChangingAsync.PropertyChangingAsync"/>. A struct, so
/// raising the event allocates nothing; the values are held as <see cref="PropertyValue"/>, which
/// does not box them.
/// </summary>
public readonly struct AsyncPropertyChangingEventArgs
{
    public AsyncPropertyChangingEventArgs(string propertyName, PropertyValue oldValue, PropertyValue newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The name of the property that is about to change.</summary>
    public string PropertyName { get; }

    /// <summary>The current value, before the change.</summary>
    public PropertyValue OldValue { get; }

    /// <summary>The value the property is about to get.</summary>
    public PropertyValue NewValue { get; }

    /// <summary>Creates the arguments for a change of a property of type <typeparamref name="T"/>.</summary>
    public static AsyncPropertyChangingEventArgs Create<T>(string propertyName, T oldValue, T newValue)
        => new(propertyName, PropertyValue.From(oldValue), PropertyValue.From(newValue));
}
