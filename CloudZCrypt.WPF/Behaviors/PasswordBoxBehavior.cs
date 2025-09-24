using System.Windows;
using System.Windows.Controls;

namespace CloudZCrypt.WPF.Behaviors;

/// <summary>
/// Provides attached properties that enable data binding of the <see cref="PasswordBox"/> password value.
/// </summary>
/// <remarks>
/// WPF's <see cref="PasswordBox.Password"/> property is not a <see cref="DependencyProperty"/>, which prevents direct data binding in MVVM scenarios.
/// This behavior introduces an attachable Password property that mirrors the password text and keeps it synchronized with the control while avoiding recursive updates.
/// </remarks>
public static class PasswordBoxBehavior
{
    /// <summary>
    /// Identifies the attached Password dependency property, which stores the current password text of an associated <see cref="PasswordBox"/> to enable binding.
    /// </summary>
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged)
        );

    /// <summary>
    /// Identifies the attached Attach dependency property. When set to true, the behavior hooks the <see cref="PasswordBox.PasswordChanged"/> event to maintain synchronization.
    /// </summary>
    public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
        "Attach",
        typeof(bool),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(false, OnAttachChanged)
    );

    /// <summary>
    /// Identifies the internal IsUpdating dependency property used to prevent recursive updates between the password value and the attached property.
    /// </summary>
    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBehavior)
        );

    /// <summary>
    /// Sets the value of the attached Attach property on the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object (expected to be a <see cref="PasswordBox"/>) on which to set the property. Must not be null.</param>
    public static void SetAttach(DependencyObject dp, bool value)
    {
        dp.SetValue(AttachProperty, value);
    }

    /// <summary>
    /// Gets the value of the attached Attach property from the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object from which to retrieve the property value. Must not be null.</param>
    /// <returns>true if the behavior is attached; otherwise, false.</returns>
    public static bool GetAttach(DependencyObject dp)
    {
        return (bool)dp.GetValue(AttachProperty);
    }

    /// <summary>
    /// Gets the value of the attached Password property from the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object from which to retrieve the password value. Must not be null.</param>
    /// <returns>The current password text associated with the <see cref="PasswordBox"/>; never null, but may be empty.</returns>
    public static string GetPassword(DependencyObject dp)
    {
        return (string)dp.GetValue(PasswordProperty);
    }

    /// <summary>
    /// Sets the value of the attached Password property on the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object (expected to be a <see cref="PasswordBox"/>) on which to set the password. Must not be null.</param>
    /// <param name="value">The new password text to associate with the control. May be null or empty.</param>
    public static void SetPassword(DependencyObject dp, string value)
    {
        dp.SetValue(PasswordProperty, value);
    }

    /// <summary>
    /// Gets the internal flag indicating whether a password synchronization update is currently in progress.
    /// </summary>
    /// <param name="dp">The dependency object to query. Must not be null.</param>
    /// <returns>true if an update is in progress; otherwise, false.</returns>
    private static bool GetIsUpdating(DependencyObject dp)
    {
        return (bool)dp.GetValue(IsUpdatingProperty);
    }

    /// <summary>
    /// Sets the internal flag that indicates whether a password synchronization update is currently in progress.
    /// </summary>
    /// <param name="dp">The dependency object to update. Must not be null.</param>
    /// <param name="value">The flag value indicating update state.</param>
    private static void SetIsUpdating(DependencyObject dp, bool value)
    {
        dp.SetValue(IsUpdatingProperty, value);
    }

    /// <summary>
    /// Handles changes to the attached Password property, updating the underlying <see cref="PasswordBox.Password"/> when not already synchronizing.
    /// </summary>
    /// <param name="sender">The dependency object whose password property changed.</param>
    /// <param name="e">The event data describing the change.</param>
    private static void OnPasswordPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (sender is PasswordBox passwordBox && !GetIsUpdating(passwordBox))
        {
            passwordBox.Password = (string)e.NewValue;
        }
    }

    /// <summary>
    /// Handles changes to the attached Attach property, subscribing or unsubscribing from the <see cref="PasswordBox.PasswordChanged"/> event as appropriate.
    /// </summary>
    /// <param name="sender">The dependency object whose attach state changed.</param>
    /// <param name="e">The event data describing the change.</param>
    private static void OnAttachChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (sender is PasswordBox passwordBox)
        {
            if ((bool)e.OldValue)
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }
    }

    /// <summary>
    /// Handles the <see cref="PasswordBox.PasswordChanged"/> event, propagating the new password value to the attached Password property while preventing recursive updates.
    /// </summary>
    /// <param name="sender">The <see cref="PasswordBox"/> whose password was modified.</param>
    /// <param name="e">The event data associated with the password change.</param>
    private static void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);
        }
    }
}
