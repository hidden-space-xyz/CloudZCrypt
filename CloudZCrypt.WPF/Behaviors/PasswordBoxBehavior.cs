using System.Windows;
using System.Windows.Controls;
using CloudZCrypt.Application.Services.Interfaces;
using CloudZCrypt.Application.ValueObjects.Password;
using CloudZCrypt.Domain.Enums;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace CloudZCrypt.WPF.Behaviors;

public static class PasswordBoxBehavior
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged)
        );

    public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
        "Attach",
        typeof(bool),
        typeof(PasswordBoxBehavior),
        new PropertyMetadata(false, OnAttachChanged)
    );

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(PasswordBoxBehavior)
        );

    public static readonly DependencyProperty AnalyzeStrengthProperty =
        DependencyProperty.RegisterAttached(
            "AnalyzeStrength",
            typeof(bool),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(false)
        );

    public static readonly DependencyProperty PasswordServiceProperty =
        DependencyProperty.RegisterAttached(
            "PasswordService",
            typeof(IPasswordService),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(null)
        );

    public static readonly DependencyProperty StrengthScoreProperty =
        DependencyProperty.RegisterAttached(
            "StrengthScore",
            typeof(double),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(0d)
        );

    public static readonly DependencyProperty StrengthTextProperty =
        DependencyProperty.RegisterAttached(
            "StrengthText",
            typeof(string),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(string.Empty)
        );

    public static readonly DependencyProperty StrengthColorProperty =
        DependencyProperty.RegisterAttached(
            "StrengthColor",
            typeof(MediaBrush),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(MediaBrushes.Transparent)
        );

    public static readonly DependencyProperty StrengthVisibilityProperty =
        DependencyProperty.RegisterAttached(
            "StrengthVisibility",
            typeof(Visibility),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(Visibility.Hidden)
        );

    private static readonly Dictionary<PasswordStrength, SolidColorBrush> strengthColorCache = new()
    {
        [PasswordStrength.VeryWeak] = new(MediaColor.FromRgb(220, 53, 69)),
        [PasswordStrength.Weak] = new(MediaColor.FromRgb(255, 193, 7)),
        [PasswordStrength.Fair] = new(MediaColor.FromRgb(255, 193, 7)),
        [PasswordStrength.Good] = new(MediaColor.FromRgb(40, 167, 69)),
        [PasswordStrength.Strong] = new(MediaColor.FromRgb(25, 135, 84)),
    };

    public static bool GetAnalyzeStrength(DependencyObject dp) =>
        (bool)dp.GetValue(AnalyzeStrengthProperty);

    public static void SetAnalyzeStrength(DependencyObject dp, bool value) =>
        dp.SetValue(AnalyzeStrengthProperty, value);

    public static IPasswordService? GetPasswordService(DependencyObject dp) =>
        (IPasswordService?)dp.GetValue(PasswordServiceProperty);

    public static void SetPasswordService(DependencyObject dp, IPasswordService? value) =>
        dp.SetValue(PasswordServiceProperty, value);

    public static double GetStrengthScore(DependencyObject dp) =>
        (double)dp.GetValue(StrengthScoreProperty);

    public static void SetStrengthScore(DependencyObject dp, double value) =>
        dp.SetValue(StrengthScoreProperty, value);

    public static string GetStrengthText(DependencyObject dp) =>
        (string)dp.GetValue(StrengthTextProperty);

    public static void SetStrengthText(DependencyObject dp, string value) =>
        dp.SetValue(StrengthTextProperty, value);

    public static MediaBrush GetStrengthColor(DependencyObject dp) =>
        (MediaBrush)dp.GetValue(StrengthColorProperty);

    public static void SetStrengthColor(DependencyObject dp, MediaBrush value) =>
        dp.SetValue(StrengthColorProperty, value);

    public static Visibility GetStrengthVisibility(DependencyObject dp) =>
        (Visibility)dp.GetValue(StrengthVisibilityProperty);

    public static void SetStrengthVisibility(DependencyObject dp, Visibility value) =>
        dp.SetValue(StrengthVisibilityProperty, value);

    public static void SetAttach(DependencyObject dp, bool value) =>
        dp.SetValue(AttachProperty, value);

    public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);

    public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject dp, string value) =>
        dp.SetValue(PasswordProperty, value);

    private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);

    private static void SetIsUpdating(DependencyObject dp, bool value) =>
        dp.SetValue(IsUpdatingProperty, value);

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

    private static void TryAnalyzeAndApplyStrength(PasswordBox passwordBox)
    {
        if (!GetAnalyzeStrength(passwordBox))
            return;

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

    private static void ResetStrength(PasswordBox passwordBox, bool clearAll)
    {
        SetStrengthVisibility(passwordBox, Visibility.Hidden);
        if (!clearAll)
            return;
        SetStrengthScore(passwordBox, 0);
        SetStrengthText(passwordBox, string.Empty);
        SetStrengthColor(passwordBox, MediaBrushes.Transparent);
    }

    private static MediaBrush GetStrengthBrush(PasswordStrength strength)
    {
        return strengthColorCache.GetValueOrDefault(strength, MediaBrushes.Transparent);
    }
}
