namespace ZeroAlloc.Notify;

/// <summary>
/// The arguments of <see cref="INotifyPropertyChangedAsync.PropertyChangedAsync"/>. A struct, so
/// raising the event allocates nothing; the values are held as <see cref="PropertyValue"/>, which
/// does not box them.
/// </summary>
public readonly struct AsyncPropertyChangedEventArgs
{
    public AsyncPropertyChangedEventArgs(string propertyName, PropertyValue oldValue, PropertyValue newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>The name of the property that changed.</summary>
    public string PropertyName { get; }

    /// <summary>The value before the change.</summary>
    public PropertyValue OldValue { get; }

    /// <summary>The value after the change.</summary>
    public PropertyValue NewValue { get; }

    /// <summary>Creates the arguments for a change of a property of type <typeparamref name="T"/>.</summary>
    public static AsyncPropertyChangedEventArgs Create<T>(string propertyName, T oldValue, T newValue)
        => new(propertyName, PropertyValue.From(oldValue), PropertyValue.From(newValue));
}
