using CloudZCrypt.Domain.Enums;
using CloudZCrypt.Domain.Services.Interfaces;
using CloudZCrypt.Domain.ValueObjects.Password;
using System.Windows;
using System.Windows.Controls;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

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
    /// Identifies an attached property that enables password strength analysis when set to true.
    /// </summary>
    public static readonly DependencyProperty AnalyzeStrengthProperty = DependencyProperty.RegisterAttached(
        "AnalyzeStrength",
        typeof(bool),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(false)
    );

    /// <summary>
    /// Identifies an attached property that supplies the <see cref="IPasswordService"/> used to analyze password strength.
    /// </summary>
    public static readonly DependencyProperty PasswordServiceProperty = DependencyProperty.RegisterAttached(
        "PasswordService",
        typeof(IPasswordService),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Identifies an attached property that exposes the numeric password strength score for binding.
    /// </summary>
    public static readonly DependencyProperty StrengthScoreProperty = DependencyProperty.RegisterAttached(
        "StrengthScore",
        typeof(double),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(0d)
    );

    /// <summary>
    /// Identifies an attached property that exposes the descriptive strength classification text for binding.
    /// </summary>
    public static readonly DependencyProperty StrengthTextProperty = DependencyProperty.RegisterAttached(
        "StrengthText",
        typeof(string),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(string.Empty)
    );

    /// <summary>
    /// Identifies an attached property that exposes the UI brush representing the strength classification color.
    /// </summary>
    public static readonly DependencyProperty StrengthColorProperty = DependencyProperty.RegisterAttached(
        "StrengthColor",
        typeof(MediaBrush),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(MediaBrushes.Transparent)
    );

    /// <summary>
    /// Identifies an attached property that exposes the visibility of the strength indicator.
    /// </summary>
    public static readonly DependencyProperty StrengthVisibilityProperty = DependencyProperty.RegisterAttached(
        "StrengthVisibility",
        typeof(Visibility),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(Visibility.Hidden)
    );

    /// <summary>
    /// Caches mapping from <see cref="PasswordStrength"/> classification to UI color brush.
    /// </summary>
    private static readonly Dictionary<PasswordStrength, SolidColorBrush> strengthColorCache = new()
    {
        [PasswordStrength.VeryWeak] = new(MediaColor.FromRgb(220, 53, 69)),
        [PasswordStrength.Weak] = new(MediaColor.FromRgb(255, 193, 7)),
        [PasswordStrength.Fair] = new(MediaColor.FromRgb(255, 193, 7)),
        [PasswordStrength.Good] = new(MediaColor.FromRgb(40, 167, 69)),
        [PasswordStrength.Strong] = new(MediaColor.FromRgb(25, 135, 84)),
    };

    /// <summary>
    /// Gets the value of the attached AnalyzeStrength property.
    /// </summary>
    public static bool GetAnalyzeStrength(DependencyObject dp) => (bool)dp.GetValue(AnalyzeStrengthProperty);

    /// <summary>
    /// Sets the value of the attached AnalyzeStrength property.
    /// </summary>
    public static void SetAnalyzeStrength(DependencyObject dp, bool value) => dp.SetValue(AnalyzeStrengthProperty, value);

    /// <summary>
    /// Gets the <see cref="IPasswordService"/> used for strength analysis.
    /// </summary>
    public static IPasswordService? GetPasswordService(DependencyObject dp) => (IPasswordService?)dp.GetValue(PasswordServiceProperty);

    /// <summary>
    /// Sets the <see cref="IPasswordService"/> used for strength analysis.
    /// </summary>
    public static void SetPasswordService(DependencyObject dp, IPasswordService? value) => dp.SetValue(PasswordServiceProperty, value);

    /// <summary>
    /// Gets the strength score associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static double GetStrengthScore(DependencyObject dp) => (double)dp.GetValue(StrengthScoreProperty);

    /// <summary>
    /// Sets the strength score associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static void SetStrengthScore(DependencyObject dp, double value) => dp.SetValue(StrengthScoreProperty, value);

    /// <summary>
    /// Gets the descriptive strength text associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static string GetStrengthText(DependencyObject dp) => (string)dp.GetValue(StrengthTextProperty);

    /// <summary>
    /// Sets the descriptive strength text associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static void SetStrengthText(DependencyObject dp, string value) => dp.SetValue(StrengthTextProperty, value);

    /// <summary>
    /// Gets the strength color associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static MediaBrush GetStrengthColor(DependencyObject dp) => (MediaBrush)dp.GetValue(StrengthColorProperty);

    /// <summary>
    /// Sets the strength color associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static void SetStrengthColor(DependencyObject dp, MediaBrush value) => dp.SetValue(StrengthColorProperty, value);

    /// <summary>
    /// Gets the visibility of the strength indicator associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static Visibility GetStrengthVisibility(DependencyObject dp) => (Visibility)dp.GetValue(StrengthVisibilityProperty);

    /// <summary>
    /// Sets the visibility of the strength indicator associated with the <see cref="PasswordBox"/>.
    /// </summary>
    public static void SetStrengthVisibility(DependencyObject dp, Visibility value) => dp.SetValue(StrengthVisibilityProperty, value);


    /// <summary>
    /// Sets the value of the attached Attach property on the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object (expected to be a <see cref="PasswordBox"/>) on which to set the property. Must not be null.</param>
    public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);

    /// <summary>
    /// Gets the value of the attached Attach property from the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object from which to retrieve the property value. Must not be null.</param>
    /// <returns>true if the behavior is attached; otherwise, false.</returns>
    public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);

    /// <summary>
    /// Gets the value of the attached Password property from the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object from which to retrieve the password value. Must not be null.</param>
    /// <returns>The current password text associated with the <see cref="PasswordBox"/>; never null, but may be empty.</returns>
    public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);

    /// <summary>
    /// Sets the value of the attached Password property on the specified dependency object.
    /// </summary>
    /// <param name="dp">The dependency object (expected to be a <see cref="PasswordBox"/>) on which to set the password. Must not be null.</param>
    /// <param name="value">The new password text to associate with the control. May be null or empty.</param>
    public static void SetPassword(DependencyObject dp, string value) => dp.SetValue(PasswordProperty, value);

    /// <summary>
    /// Gets the internal flag indicating whether a password synchronization update is currently in progress.
    /// </summary>
    /// <param name="dp">The dependency object to query. Must not be null.</param>
    /// <returns>true if an update is in progress; otherwise, false.</returns>
    private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);

    /// <summary>
    /// Sets the internal flag that indicates whether a password synchronization update is currently in progress.
    /// </summary>
    /// <param name="dp">The dependency object to update. Must not be null.</param>
    /// <param name="value">The flag value indicating update state.</param>
    private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetValue(IsUpdatingProperty, value);

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

            // Also update strength state when password is set programmatically.
            TryAnalyzeAndApplyStrength(passwordBox);
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
            // Always detach first to avoid duplicate subscriptions, then attach if requested.
            passwordBox.PasswordChanged -= PasswordChanged;
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

            // Update strength analysis if enabled
            TryAnalyzeAndApplyStrength(passwordBox);
        }
    }

    /// <summary>
    /// Attempts to analyze the current password and apply the strength-related attached properties.
    /// </summary>
    /// <param name="passwordBox">The password box instance to analyze.</param>
    private static void TryAnalyzeAndApplyStrength(PasswordBox passwordBox)
    {
        if (!GetAnalyzeStrength(passwordBox)) return;

        string pwd = passwordBox.Password ?? string.Empty;

        if (string.IsNullOrEmpty(pwd))
        {
            ResetStrength(passwordBox, clearAll: true);
            return;
        }

        IPasswordService? service = GetPasswordService(passwordBox);
        if (service == null)
        {
            // If no analyzer service is provided, hide the indicator gracefully.
            ResetStrength(passwordBox, clearAll: false);
            return;
        }

        PasswordStrengthAnalysis analysis;
        try
        {
            analysis = service.AnalyzePasswordStrength(pwd);
        }
        catch
        {
            // On analysis failure, hide indicator and avoid throwing in UI path.
            ResetStrength(passwordBox, clearAll: false);
            return;
        }

        MediaBrush color = GetStrengthBrush(analysis.Strength);
        SetStrengthScore(passwordBox, analysis.Score);
        SetStrengthText(passwordBox, analysis.Description);
        SetStrengthColor(passwordBox, color);
        SetStrengthVisibility(passwordBox, Visibility.Visible);
    }

    /// <summary>
    /// Reset the strength-related UI state.
    /// </summary>
    private static void ResetStrength(PasswordBox passwordBox, bool clearAll)
    {
        SetStrengthVisibility(passwordBox, Visibility.Hidden);
        if (!clearAll) return;
        SetStrengthScore(passwordBox, 0);
        SetStrengthText(passwordBox, string.Empty);
        SetStrengthColor(passwordBox, MediaBrushes.Transparent);
    }

    /// <summary>
    /// Resolves a color brush corresponding to the specified password strength classification.
    /// </summary>
    /// <param name="strength">The strength classification.</param>
    /// <returns>A brush representing the classification color, or transparent if undefined.</returns>
    private static MediaBrush GetStrengthBrush(PasswordStrength strength)
    {
        return strengthColorCache.GetValueOrDefault(strength, MediaBrushes.Transparent);
    }
}
