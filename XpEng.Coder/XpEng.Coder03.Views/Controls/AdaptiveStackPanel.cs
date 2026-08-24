using System.Windows;
using System.Windows.Controls;

namespace XpEng.Coder03.Views.Controls;

public class AdaptiveStackPanel : Panel {

    protected override Size MeasureOverride(Size availableSize) {
        var children = GetVisibleChildren();
        if (children.Count == 0) return new Size(0, 0);

        var (gmw, sumMinWidths, effectiveMaxWidth) = ComputeMetrics(children, availableSize.Width);
        bool useHorizontal = sumMinWidths <= effectiveMaxWidth;

        System.Diagnostics.Debug.WriteLine($"[AdaptiveStackPanel] Measure: available={availableSize.Width:F1}, gmw={gmw:F1}, sumMin={sumMinWidths:F1}, effMax={effectiveMaxWidth:F1}, horizontal={useHorizontal}");

        return useHorizontal
            ? MeasureHorizontal(children, effectiveMaxWidth, availableSize.Height)
            : MeasureVertical(children, gmw, availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize) {
        var children = GetVisibleChildren();
        if (children.Count == 0) return finalSize;

        var (gmw, sumMinWidths, _) = ComputeMetrics(children, finalSize.Width);
        bool useHorizontal = sumMinWidths <= finalSize.Width;

        System.Diagnostics.Debug.WriteLine($"[AdaptiveStackPanel] Arrange: final={finalSize.Width:F1}, gmw={gmw:F1}, sumMin={sumMinWidths:F1}, horizontal={useHorizontal}");

        return useHorizontal
            ? ArrangeHorizontal(children, finalSize)
            : ArrangeVertical(children, finalSize);
    }

    private (double Gmw, double SumMinWidths, double EffectiveMaxWidth) ComputeMetrics(List<UIElement> children, double availableWidth) {
        double gmw = 0, sumMinWidths = 0, greatestMaxWidth = 0;
        foreach (var child in children) {
            double minWidth = GetEffectiveMinWidth(child);
            double maxWidth = GetEffectiveMaxWidth(child);
            double declaredMinWidth = (child as FrameworkElement)?.MinWidth ?? -1;
            System.Diagnostics.Debug.WriteLine($"[AdaptiveStackPanel]   child={child.GetType().Name}, declaredMinWidth={declaredMinWidth:F1}, probedMinWidth={minWidth:F1}, maxWidth={maxWidth:F1}");
            gmw = Math.Max(gmw, minWidth);
            sumMinWidths += minWidth;
            greatestMaxWidth = Math.Max(greatestMaxWidth, double.IsPositiveInfinity(maxWidth) ? 0 : maxWidth);
        }
        if (greatestMaxWidth <= 0) greatestMaxWidth = double.PositiveInfinity;

        double effectiveMaxWidth = double.IsPositiveInfinity(availableWidth)
            ? greatestMaxWidth
            : Math.Min(availableWidth, greatestMaxWidth);

        return (gmw, sumMinWidths, effectiveMaxWidth);
    }

    // ==================== Horizontal mode ====================

    private Size MeasureHorizontal(List<UIElement> children, double effectiveMaxWidth, double availableHeight) {
        var (fixedChildren, elasticChildren) = SplitFixedElastic(children);

        double fixedWidthTotal = 0, maxHeight = 0;
        foreach (var child in fixedChildren) {
            child.Measure(new Size(double.PositiveInfinity, availableHeight));
            fixedWidthTotal += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        double remainingWidth = Math.Max(0, effectiveMaxWidth - fixedWidthTotal);
        double perElasticWidth = elasticChildren.Count > 0 ? remainingWidth / elasticChildren.Count : 0;

        double totalWidth = fixedWidthTotal;
        foreach (var child in elasticChildren) {
            child.Measure(new Size(perElasticWidth, availableHeight));
            totalWidth += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        return new Size(totalWidth, maxHeight);
    }
    private Size ArrangeHorizontal(List<UIElement> children, Size finalSize) {
        var (fixedChildren, elasticChildren) = SplitFixedElastic(children);

        double fixedWidthTotal = 0;
        foreach (var child in fixedChildren) {
            child.Measure(new Size(double.PositiveInfinity, finalSize.Height));
            fixedWidthTotal += child.DesiredSize.Width;
        }

        double remainingWidth = Math.Max(0, finalSize.Width - fixedWidthTotal);
        double perElasticWidth = elasticChildren.Count > 0 ? remainingWidth / elasticChildren.Count : 0;

        // Re-measure elastic children at their actual final width BEFORE arranging them —
        // arranging without this step can leave a child's internal layout (e.g. a TextBox's
        // rendered content) stale relative to the width it's actually being squeezed into.
        foreach (var child in elasticChildren) {
            child.Measure(new Size(perElasticWidth, finalSize.Height));
        }

        double x = 0;
        foreach (var child in children) {
            bool isElastic = elasticChildren.Contains(child);
            double width = isElastic ? perElasticWidth : child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }

    // ==================== Vertical mode ====================

    private Size MeasureVertical(List<UIElement> children, double gmw, double availableHeight) {
        double totalHeight = 0;
        foreach (var child in children) {
            child.Measure(new Size(gmw, double.PositiveInfinity));
            totalHeight += child.DesiredSize.Height;
        }
        return new Size(gmw, totalHeight);
    }

    private Size ArrangeVertical(List<UIElement> children, Size finalSize) {
        double y = 0;
        foreach (var child in children) {
            child.Measure(new Size(finalSize.Width, double.PositiveInfinity));
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }

    // ==================== Shared helpers ====================

    private List<UIElement> GetVisibleChildren() =>
        InternalChildren.Cast<UIElement>().Where(c => c.Visibility != Visibility.Collapsed).ToList();

    private static (List<UIElement> Fixed, List<UIElement> Elastic) SplitFixedElastic(List<UIElement> children) {
        var fixedList = new List<UIElement>();
        var elasticList = new List<UIElement>();
        foreach (var child in children) {
            if (IsFixedSize(child)) fixedList.Add(child); else elasticList.Add(child);
        }
        return (fixedList, elasticList);
    }

    private static bool IsFixedSize(UIElement element) {
        if (element is not FrameworkElement fe) return true;
        if (!double.IsNaN(fe.Width)) return true;
        if (fe.MinWidth > 0 && fe.MinWidth == fe.MaxWidth) return true;
        return false;
    }

    private static double GetEffectiveMinWidth(UIElement element) {
        element.Measure(new Size(0, double.PositiveInfinity));
        return element.DesiredSize.Width;
    }

    private static double GetEffectiveMaxWidth(UIElement element) =>
        element is FrameworkElement fe ? fe.MaxWidth : double.PositiveInfinity;
}