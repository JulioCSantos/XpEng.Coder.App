using System.Windows;
using System.Windows.Controls;

namespace XpEng.Coder03.Views.Controls {
    public class ResponsivePanel : Panel {
        protected override Size MeasureOverride(Size availableSize) {
            double spacing = 5;
            double currentX = 0;
            double currentY = 0;
            double maxRowHeight = 0;
            double totalWidth = 0;

            foreach (UIElement child in InternalChildren) {
                child.Measure(new Size(availableSize.Width, availableSize.Height));
                Size childSize = child.DesiredSize;

                if (currentX > 0 && currentX + childSize.Width > availableSize.Width) {
                    totalWidth = Math.Max(totalWidth, currentX - spacing);
                    currentY += maxRowHeight + spacing;
                    currentX = 0;
                    maxRowHeight = 0;
                }

                currentX += childSize.Width + spacing;
                maxRowHeight = Math.Max(maxRowHeight, childSize.Height);
            }

            totalWidth = Math.Max(totalWidth, currentX > 0 ? currentX - spacing : 0);
            double totalHeight = currentY + maxRowHeight;

            return new Size(Math.Min(availableSize.Width, totalWidth), totalHeight);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            double spacing = 5;
            double currentX = 0;
            double currentY = 0;
            double maxRowHeight = 0;

            List<UIElement> currentRowElements = new List<UIElement>();
            List<Size> currentRowSizes = new List<Size>();

            void ArrangeCurrentRow() {
                if (currentRowElements.Count == 0) return;

                double x = 0;
                for (int i = 0; i < currentRowElements.Count; i++) {
                    var child = currentRowElements[i];
                    var size = currentRowSizes[i];

                    // Respect FrameworkElement MaxWidth constraints during layout arrangement
                    double finalChildWidth = size.Width;
                    if (child is FrameworkElement fe && !double.IsNaN(fe.MaxWidth)) {
                        finalChildWidth = Math.Min(finalChildWidth, fe.MaxWidth);
                    }

                    // If it's the last item on a single line and we have extra space, 
                    // ensure we don't force a Grid/FrameworkElement past its MaxWidth if it's set.
                    if (currentRowElements.Count == 2 && i == 1) {
                        if (child is FrameworkElement innerFe && !double.IsNaN(innerFe.MaxWidth)) {
                            finalChildWidth = Math.Min(finalSize.Width - x, innerFe.MaxWidth);
                        }
                    }

                    double y = currentY + (maxRowHeight - size.Height) / 2;
                    child.Arrange(new Rect(x, y, finalChildWidth, size.Height));

                    x += finalChildWidth + spacing;
                }

                currentY += maxRowHeight + spacing;
                currentX = 0;
                maxRowHeight = 0;
                currentRowElements.Clear();
                currentRowSizes.Clear();
            }

            foreach (UIElement child in InternalChildren) {
                Size childSize = child.DesiredSize;

                if (currentX > 0 && currentX + childSize.Width > finalSize.Width) {
                    ArrangeCurrentRow();
                }

                currentRowElements.Add(child);
                currentRowSizes.Add(childSize);

                currentX += childSize.Width + spacing;
                maxRowHeight = Math.Max(maxRowHeight, childSize.Height);
            }

            ArrangeCurrentRow();

            return finalSize;
        }
    }
}