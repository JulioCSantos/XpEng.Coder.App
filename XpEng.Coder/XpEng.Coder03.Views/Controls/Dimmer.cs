using System.Windows;

namespace XpEng.Coder03.Views.Controls {
    public static class Dimmer {
        #region IsActive
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsActive", typeof(bool), typeof(Dimmer),
                new FrameworkPropertyMetadata(true)
            );

        public static bool GetIsActive(DependencyObject obj) => (bool)obj.GetValue(IsActiveProperty);
        public static void SetIsActive(DependencyObject obj, bool value) => obj.SetValue(IsActiveProperty, value);
        #endregion IsActive
    }
}