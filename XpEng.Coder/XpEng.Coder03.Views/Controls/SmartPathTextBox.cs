using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace XpEng.Coder03.Views.Controls {
    /// <summary>
    /// Displays a shortened path while unfocused, but preserves the complete path for editing/binding.
    /// Shortening removes leading path components rather than simply clipping the end.
    /// </summary>
    public class SmartPathTextBox : TextBox {
        private bool _isSyncing;
        private readonly string _controlId = Guid.NewGuid().ToString("N")[..4];

        public static readonly DependencyProperty FullPathProperty =
            DependencyProperty.Register(
                nameof(FullPath), typeof(string), typeof(SmartPathTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFullPathChanged)
            );

        public string FullPath {
            get => (string)GetValue(FullPathProperty);
            set => SetValue(FullPathProperty, value);
        }

        private static readonly DependencyPropertyKey DisplayPathPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(DisplayPath), typeof(string), typeof(SmartPathTextBox),
                new PropertyMetadata(string.Empty)
            );

        public static readonly DependencyProperty DisplayPathProperty = DisplayPathPropertyKey.DependencyProperty;

        public string DisplayPath {
            get => (string)GetValue(DisplayPathProperty);
            private set => SetValue(DisplayPathPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ExistsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Exists), typeof(bool), typeof(SmartPathTextBox),
                new PropertyMetadata(false)
            );

        public static readonly DependencyProperty ExistsProperty = ExistsPropertyKey.DependencyProperty;

        public bool Exists {
            get => (bool)GetValue(ExistsProperty);
            private set => SetValue(ExistsPropertyKey, value);
        }

        static SmartPathTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SmartPathTextBox), new FrameworkPropertyMetadata(typeof(SmartPathTextBox))
            );
        }

        public SmartPathTextBox() {
            Loaded += (_, _) => CalculateDisplayPath();
            SizeChanged += (_, _) => CalculateDisplayPath();
        }

        private static void OnFullPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is not SmartPathTextBox box) return;

            string path = e.NewValue as string ?? string.Empty;
            box.EvaluatePathExistence(path);

            if (!box._isSyncing) {
                box._isSyncing = true;
                box.Text = path;
                box._isSyncing = false;
            }

            box.CalculateDisplayPath();
        }

        protected override void OnTextChanged(TextChangedEventArgs e) {
            base.OnTextChanged(e);
            if (_isSyncing) return;

            _isSyncing = true;
            FullPath = Text;
            _isSyncing = false;

            EvaluatePathExistence(FullPath);
            CalculateDisplayPath();
        }

        private void EvaluatePathExistence(string? path) {
            if (string.IsNullOrWhiteSpace(path)) {
                Exists = false;
                return;
            }

            try {
                Exists = Directory.Exists(path) || File.Exists(path);
            }
            catch {
                Exists = false;
            }
        }

        private void CalculateDisplayPath() {
            string path = FullPath ?? string.Empty;

            if (string.IsNullOrEmpty(path)) {
                DisplayPath = string.Empty;
                return;
            }

            // Use only the width actually available for rendered text.
            double availableWidth = ActualWidth - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right;

            // ActualWidth can legitimately be zero during early layout.
            if (availableWidth <= 0) return;

            if (MeasureTextWidth(path) <= availableWidth) {
                DisplayPath = path;
                return;
            }

            DisplayPath = BuildShortenedPath(path, availableWidth);
        }

        private string BuildShortenedPath(string fullPath, double availableWidth) {
            char separator = fullPath.Contains('\\') ? '\\' : '/';
            string sep = separator.ToString();

            string[] parts = fullPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return fullPath;

            // Preserve the most useful information: the right-most path components.
            string tail = parts[^1];
            if (parts.Length == 1) return tail;

            string result = "..." + sep + tail;

            for (int i = parts.Length - 2; i >= 0; i--) {
                string candidateTail = parts[i] + sep + tail;
                string candidate = "..." + sep + candidateTail;

                if (MeasureTextWidth(candidate) > availableWidth) break;

                tail = candidateTail;
                result = candidate;
            }

            return result;
        }

        private double MeasureTextWidth(string text) {
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            return formattedText.WidthIncludingTrailingWhitespace;
        }

        // Retained in case another caller still uses it.
        public double MeasureFullTextWidth() {
            return string.IsNullOrEmpty(FullPath) ? 0 : MeasureTextWidth(FullPath);
        }
    }
}