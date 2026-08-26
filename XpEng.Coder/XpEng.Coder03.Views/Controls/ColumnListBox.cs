using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace XpEng.Coder03.Views.Controls {
    /// <summary>
    /// ListBox that always lays out its items through a WrapPanel and manages
    /// item width to produce a configurable number of responsive columns.
    ///
    /// Columns is the preferred starting column count.
    /// MinColumns and MaxColumns define the allowed range.
    /// MinItemWidth is the actual minimum item width. If the viewport becomes
    /// narrower, normal ScrollViewer behavior determines whether the content scrolls.
    ///
    /// The WrapPanel is intrinsic to this control and is intentionally created internally.
    /// </summary>
    public class ColumnListBox : ListBox {
        #region properties and fields
        private double _viewportWidth;

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns), typeof(int), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
            );

        public int Columns {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        public static readonly DependencyProperty MinColumnsProperty =
            DependencyProperty.Register(
                nameof(MinColumns), typeof(int), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
            );

        public int MinColumns {
            get => (int)GetValue(MinColumnsProperty);
            set => SetValue(MinColumnsProperty, value);
        }

        public static readonly DependencyProperty MaxColumnsProperty =
            DependencyProperty.Register(
                nameof(MaxColumns), typeof(int), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(int.MaxValue, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
            );

        public int MaxColumns {
            get => (int)GetValue(MaxColumnsProperty);
            set => SetValue(MaxColumnsProperty, value);
        }

        public static readonly DependencyProperty MinItemWidthProperty =
            DependencyProperty.Register(
                nameof(MinItemWidth), typeof(double), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(250.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
            );

        public double MinItemWidth {
            get => (double)GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        private static readonly DependencyPropertyKey ItemWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ItemWidth), typeof(double), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure)
            );

        public static readonly DependencyProperty PreferredItemWidthProperty =
            DependencyProperty.Register(
                nameof(PreferredItemWidth), typeof(double), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(320.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutPropertyChanged)
            );

        public double PreferredItemWidth {
            get => (double)GetValue(PreferredItemWidthProperty);
            set => SetValue(PreferredItemWidthProperty, value);
        }

        public static readonly DependencyProperty ItemWidthProperty = ItemWidthPropertyKey.DependencyProperty;
        public double ItemWidth => (double)GetValue(ItemWidthProperty);

        private static readonly DependencyPropertyKey PanelWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(PanelWidth), typeof(double), typeof(ColumnListBox),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure)
            );

        public static readonly DependencyProperty PanelWidthProperty = PanelWidthPropertyKey.DependencyProperty;
        public double PanelWidth => (double)GetValue(PanelWidthProperty);
        #endregion properties and fields

        #region constructors
        public ColumnListBox() {
            ItemsPanel = CreateItemsPanel();
            HorizontalContentAlignment = HorizontalAlignment.Stretch;

            // ScrollChanged bubbles from the ListBox's internal ScrollViewer.
            // It gives us the real viewport width rather than an approximation from ActualWidth.
            AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrollChanged));

            Loaded += (_, _) => UpdateLayoutWidths();
            SizeChanged += (_, _) => UpdateLayoutWidths();
        }
        #endregion constructors

        #region methods
        private static ItemsPanelTemplate CreateItemsPanel() {
            var panel = new FrameworkElementFactory(typeof(WrapPanel));

            panel.SetBinding(WrapPanel.ItemWidthProperty, new Binding(nameof(ItemWidth)) {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ColumnListBox), 1)
            });

            panel.SetBinding(FrameworkElement.WidthProperty, new Binding(nameof(PanelWidth)) {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ColumnListBox), 1)
            });

            return new ItemsPanelTemplate(panel);
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ColumnListBox listBox) listBox.UpdateLayoutWidths();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e) {
            if (e.OriginalSource is not ScrollViewer scrollViewer ||
                !ReferenceEquals(scrollViewer.TemplatedParent, this) ||
                scrollViewer.ViewportWidth <= 0) return;

            if (Math.Abs(_viewportWidth - scrollViewer.ViewportWidth) < .5) return;

            _viewportWidth = scrollViewer.ViewportWidth;
            UpdateLayoutWidths();
        }

        private void UpdateLayoutWidths() {
            double availableWidth = GetAvailableWidth();
            if (availableWidth <= 0) return;

            int minColumns = Math.Max(1, MinColumns);
            int maxColumns = Math.Max(minColumns, MaxColumns);
            int columns = Math.Clamp(Columns, minColumns, maxColumns);

            // Reduce until the configured minimum item width can be satisfied.
            while (columns > minColumns && availableWidth / columns < MinItemWidth) columns--;

            // Add columns for as long as every item can still satisfy MinItemWidth.
            while (columns < maxColumns && availableWidth / (columns + 1) >= PreferredItemWidth) columns++;

            // MinItemWidth is a true minimum. If even MinColumns do not fit,
            // the panel becomes wider than the viewport and the ScrollViewer may scroll.
            double minimumPanelWidth = columns * MinItemWidth;
            double panelWidth = Math.Max(availableWidth, minimumPanelWidth);

            // One pixel protects WrapPanel from rounding an item onto the next row.
            double itemWidth = Math.Floor(panelWidth / columns) - 1;

            SetValue(PanelWidthPropertyKey, panelWidth);
            SetValue(ItemWidthPropertyKey, Math.Max(MinItemWidth, itemWidth));
        }

        private double GetAvailableWidth() {
            if (_viewportWidth > 0) return _viewportWidth;

            // Initial fallback until the ScrollViewer reports its actual viewport.
            return ActualWidth - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right;
        }
        #endregion methods

    }
}