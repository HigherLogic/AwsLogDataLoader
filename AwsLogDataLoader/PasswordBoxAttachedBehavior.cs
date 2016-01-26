using System;
using System.Windows;
using System.Windows.Controls;

namespace AwsLogDataLoader
{
    public static class PasswordBoxAttachedBehavior
    {
        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(PasswordBoxAttachedBehavior), new PropertyMetadata { DefaultValue = false, PropertyChangedCallback = OnIsAttached });

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxAttachedBehavior));

        public static bool GetIsAttached(UIElement element)
        {
            return (bool)element.GetValue(IsAttachedProperty);
        }

        public static void SetIsAttached(UIElement element, bool value)
        {
            element.SetValue(IsAttachedProperty, value);
        }
        public static string GetPassword(UIElement element)
        {
            return (string)element.GetValue(PasswordProperty);
        }

        public static void SetPassword(UIElement element, string value)
        {
            element.SetValue(PasswordProperty, value);
        }

        private static void OnIsAttached(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = d as PasswordBox;
            if (passwordBox == null)
            {
                return;
            }
            var enabled = (bool)e.NewValue;
            if (enabled)
            {
                passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            }
            else
            {
                passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                SetPassword(passwordBox, passwordBox.Password);
            }
        }
    }
}
