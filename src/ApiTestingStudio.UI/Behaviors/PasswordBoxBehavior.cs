using System.Windows;
using System.Windows.Controls;

namespace ApiTestingStudio.UI.Behaviors;

/// <summary>
/// Attached property enabling two-way binding of <see cref="PasswordBox.Password"/> (which is not a
/// bindable DependencyProperty for security reasons) to a view-model string. View-only helper so the
/// secret never leaves the edit control except through the bound property. Guards against feedback
/// loops while syncing.
/// </summary>
public static class PasswordBoxBehavior
{
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

    private static readonly DependencyProperty UpdatingProperty =
        DependencyProperty.RegisterAttached(
            "Updating",
            typeof(bool),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject obj) =>
        (string)obj.GetValue(BoundPasswordProperty);

    public static void SetBoundPassword(DependencyObject obj, string value) =>
        obj.SetValue(BoundPasswordProperty, value);

    private static bool GetUpdating(DependencyObject obj) => (bool)obj.GetValue(UpdatingProperty);

    private static void SetUpdating(DependencyObject obj, bool value) => obj.SetValue(UpdatingProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox passwordBox)
        {
            return;
        }

        passwordBox.PasswordChanged -= OnPasswordChanged;

        if (!GetUpdating(passwordBox))
        {
            passwordBox.Password = (string)e.NewValue ?? string.Empty;
        }

        passwordBox.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        SetUpdating(passwordBox, true);
        SetBoundPassword(passwordBox, passwordBox.Password);
        SetUpdating(passwordBox, false);
    }
}
