using System;
using System.Collections.Generic;
using System.Text;

namespace XpEng.Coder03.Views.Controls {
    public static class ResponsiveGridCalculator {
        public static double CalculateItemWidth(double availableWidth, double minItemWidth, int maxColumns) {
            if (availableWidth <= 0 || minItemWidth <= 0) return minItemWidth;
            int columns = Math.Max(1, Math.Min(maxColumns, (int)(availableWidth / minItemWidth)));
            return Math.Max(minItemWidth, Math.Floor(availableWidth / columns) - 1);
        }
    }
}
