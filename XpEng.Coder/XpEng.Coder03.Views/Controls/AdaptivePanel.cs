using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace XpEng.Coder03.Views.Controls {
    public class AdaptivePanel : Panel {
        private const double Spacing = 5;

        protected override Size MeasureOverride(Size availableSize) {
            double availableWidth = availableSize.Width;

            double rowWidth = 0;
            double rowHeight = 0;
            double totalWidth = 0;
            double totalHeight = 0;

            foreach (UIElement child in InternalChildren) {
                child.Measure(new Size(
                    double.IsInfinity(availableWidth)
                        ? double.PositiveInfinity
                        : availableWidth,
                    availableSize.Height));

                Size desired = child.DesiredSize;

                bool wrap =
                    rowWidth > 0 &&
                    !double.IsInfinity(availableWidth) &&
                    rowWidth + Spacing + desired.Width > availableWidth;

                if (wrap) {
                    totalWidth = Math.Max(totalWidth, rowWidth);
                    totalHeight += rowHeight + Spacing;

                    rowWidth = 0;
                    rowHeight = 0;
                }

                if (rowWidth > 0)
                    rowWidth += Spacing;

                rowWidth += desired.Width;
                rowHeight = Math.Max(rowHeight, desired.Height);
            }

            totalWidth = Math.Max(totalWidth, rowWidth);
            totalHeight += rowHeight;

            if (!double.IsInfinity(availableWidth))
                totalWidth = Math.Min(totalWidth, availableWidth);

            return new Size(totalWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            double currentY = 0;

            var row = new List<UIElement>();
            double rowDesiredWidth = 0;
            double rowHeight = 0;

            void ArrangeRow() {
                if (row.Count == 0)
                    return;

                double x = 0;

                foreach (UIElement child in row) {
                    if (x > 0)
                        x += Spacing;

                    double availableWidth =
                        Math.Max(0, finalSize.Width - x);

                    double width =
                        Math.Min(child.DesiredSize.Width, availableWidth);

                    if (child is FrameworkElement fe) {
                        width = Math.Min(width, fe.MaxWidth);

                        if (availableWidth >= fe.MinWidth)
                            width = Math.Max(width, fe.MinWidth);
                    }

                    double height = child.DesiredSize.Height;

                    double y =
                        currentY +
                        Math.Max(0, (rowHeight - height) / 2);

                    child.Arrange(
                        new Rect(
                            x,
                            y,
                            width,
                            height));

                    x += width;
                }

                currentY += rowHeight + Spacing;

                row.Clear();
                rowDesiredWidth = 0;
                rowHeight = 0;
            }

            foreach (UIElement child in InternalChildren) {
                double childWidth = child.DesiredSize.Width;

                double proposedWidth =
                    row.Count == 0
                        ? childWidth
                        : rowDesiredWidth + Spacing + childWidth;

                if (row.Count > 0 &&
                    proposedWidth > finalSize.Width) {

                    ArrangeRow();
                }

                if (row.Count > 0)
                    rowDesiredWidth += Spacing;

                row.Add(child);
                rowDesiredWidth += childWidth;

                rowHeight =
                    Math.Max(
                        rowHeight,
                        child.DesiredSize.Height);
            }

            ArrangeRow();

            return finalSize;
        }
    }
}