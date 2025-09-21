using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CloudZCrypt.WPF.ViewModels;

/// <summary>
/// Provides a lightweight abstract base class that implements <see cref="INotifyPropertyChanged"/>,
/// supplying a reusable <c>SetProperty</c> helper to simplify property change notification
/// in view models.
/// </summary>
/// <remarks>
/// Derive your WPF / MVVM view models from this class to reduce boilerplate when implementing
/// properties. Typical usage:
/// <code><![CDATA[
/// private string _name;
/// public string Name
/// {
///     get => _name;
///     set => SetProperty(ref _name, value);
/// }
/// ]]></code>
/// The <see cref="SetProperty{T}(ref T, T, string?)"/> method performs an equality check and only
/// raises <see cref="PropertyChanged"/> when the value actually changes, avoiding unnecessary UI updates.
/// </remarks>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed. Automatically supplied by the compiler when omitted.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field to the provided value if it is different and raises <see cref="PropertyChanged"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property / backing field.</typeparam>
    /// <param name="field">A reference to the backing field to update.</param>
    /// <param name="value">The new value to assign.</param>
    /// <param name="propertyName">The name of the property being set. Automatically supplied by the compiler when omitted.</param>
    /// <returns><c>true</c> if the field value was changed and a notification was raised; otherwise <c>false</c> if the existing value was equal.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }
}
