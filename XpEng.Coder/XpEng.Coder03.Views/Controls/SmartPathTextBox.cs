using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace XpEng.Coder03.Views.Controls {
    public class SmartPathTextBox : TextBox {
        private bool _isSyncing;

        private readonly string _controlId =
            Guid.NewGuid().ToString("N")[..4];

        public static readonly DependencyProperty FullPathProperty =
            DependencyProperty.Register(
                nameof(FullPath),
                typeof(string),
                typeof(SmartPathTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnFullPathChanged));

        public string FullPath {
            get => (string)GetValue(FullPathProperty);
            set => SetValue(FullPathProperty, value);
        }


        private static readonly DependencyPropertyKey DisplayPathPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(DisplayPath),
                typeof(string),
                typeof(SmartPathTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DisplayPathProperty =
            DisplayPathPropertyKey.DependencyProperty;

        public string DisplayPath {
            get => (string)GetValue(DisplayPathProperty);
            private set => SetValue(DisplayPathPropertyKey, value);
        }


        private static readonly DependencyPropertyKey ExistsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Exists),
                typeof(bool),
                typeof(SmartPathTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ExistsProperty =
            ExistsPropertyKey.DependencyProperty;

        public bool Exists {
            get => (bool)GetValue(ExistsProperty);
            private set => SetValue(ExistsPropertyKey, value);
        }


        static SmartPathTextBox() {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SmartPathTextBox),
                new FrameworkPropertyMetadata(
                    typeof(SmartPathTextBox)));
        }


        public SmartPathTextBox() {
            Loaded += (_, _) =>
                CalculateDisplayPath();

            SizeChanged += (_, e) => {
                Debug.WriteLine(
                    $"[SmartPath-{_controlId}] " +
                    $"Width {e.PreviousSize.Width:N1} -> {e.NewSize.Width:N1}");

                CalculateDisplayPath();
            };
        }


        private static void OnFullPathChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e) {

            if (d is not SmartPathTextBox box)
                return;

            string path =
                e.NewValue as string ?? string.Empty;

            box.EvaluatePathExistence(path);

            if (!box._isSyncing) {
                box._isSyncing = true;

                box.Text = path;

                box._isSyncing = false;
            }

            box.CalculateDisplayPath();
        }


        protected override void OnTextChanged(
            TextChangedEventArgs e) {

            base.OnTextChanged(e);

            if (_isSyncing)
                return;

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
                Exists =
                    Directory.Exists(path) ||
                    File.Exists(path);
            }
            catch {
                Exists = false;
            }
        }


        private void CalculateDisplayPath() {
            string path =
                FullPath ?? string.Empty;

            if (string.IsNullOrEmpty(path)) {
                DisplayPath = string.Empty;
                return;
            }

            double availableWidth =
                ActualWidth
                - Padding.Left
                - Padding.Right
                - BorderThickness.Left
                - BorderThickness.Right;

            /*
             * The first WPF layout pass can legitimately have zero width.
             * Do nothing and wait for Loaded/SizeChanged.
             */
            if (availableWidth <= 0)
                return;

            if (MeasureTextWidth(path) <= availableWidth) {
                DisplayPath = path;
                return;
            }

            DisplayPath =
                BuildShortenedPath(
                    path,
                    availableWidth);

            Debug.WriteLine(
                $"[SmartPath-{_controlId}] " +
                $"{ActualWidth:N0}px -> {DisplayPath}");
        }


        private string BuildShortenedPath(
            string fullPath,
            double availableWidth) {

            /*
             * Preserve the separator style of the supplied path.
             */
            char separator =
                fullPath.Contains('\\')
                    ? '\\'
                    : '/';

            string sep =
                separator.ToString();

            string[] parts =
                fullPath.Split(
                    new[] { '\\', '/' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return fullPath;

            /*
             * Always preserve the right-most component first.
             */
            string tail = parts[^1];

            if (parts.Length == 1)
                return tail;

            string result =
                "..." + sep + tail;

            /*
             * Add directories from right to left for as long as they fit.
             *
             * Example:
             *
             * ...\AutoGenerated
             * ...\Models\AutoGenerated
             * ...\TBQuiz09.Models\Models\AutoGenerated
             */
            for (int i = parts.Length - 2; i >= 0; i--) {
                string candidateTail =
                    parts[i] +
                    sep +
                    tail;

                string candidate =
                    "..." +
                    sep +
                    candidateTail;

                if (MeasureTextWidth(candidate) >
                    availableWidth) {

                    break;
                }

                tail = candidateTail;
                result = candidate;
            }

            return result;
        }


        private double MeasureTextWidth(string text) {
            var formattedText =
                new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(
                        FontFamily,
                        FontStyle,
                        FontWeight,
                        FontStretch),
                    FontSize,
                    Foreground,
                    VisualTreeHelper
                        .GetDpi(this)
                        .PixelsPerDip);

            return
                formattedText.WidthIncludingTrailingWhitespace;
        }


        /*
         * Keep this for compatibility if something else is currently
         * calling it. Card sizing should no longer depend on it.
         */
        public double MeasureFullTextWidth() {
            if (string.IsNullOrEmpty(FullPath))
                return 0;

            return MeasureTextWidth(FullPath);
        }
    }
}