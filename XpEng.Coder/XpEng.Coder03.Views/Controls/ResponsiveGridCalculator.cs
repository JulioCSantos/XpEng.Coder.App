using System;

namespace XpEng.Coder03.Views.Controls {
    public static class ResponsiveGridCalculator {
        public static double CalculateItemWidth(
            double availableWidth, double minItemWidth, double preferredItemWidth, int maxColumns) {

            if (availableWidth <= 0 || minItemWidth <= 0 || preferredItemWidth <= 0) return minItemWidth;

            // Preferred width controls when we choose to add another column.
            int columns = Math.Max(1, Math.Min(maxColumns, (int)(availableWidth / preferredItemWidth)));

            // Never allow the preference to produce cards below the real minimum.
            while (columns > 1 && availableWidth / columns < minItemWidth) columns--;

            return Math.Max(minItemWidth, Math.Floor(availableWidth / columns) - 1);
        }
    }
}