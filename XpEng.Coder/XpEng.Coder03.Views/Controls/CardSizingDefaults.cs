namespace XpEng.Coder03.Views.Controls {
    /// <summary>
    /// Centralized tuning values for the responsive Target/Template card layout.
    /// These values express UI preferences rather than path-specific requirements.
    /// </summary>
    public static class CardSizingDefaults {
        // A card may shrink this far when the window requires it.
        public const double MinCardWidth = 250;

        // Prefer this much room per card before adding another column.
        // Increasing this favors fewer/wider cards; decreasing it favors more columns.
        public const double PreferredCardWidth = 320;

        // Maximum density even on very wide displays.
        public const int MaxColumns = 2;

        //// Minimum useful editor area before AdaptivePanel moves it below its label.
        //public const double MinEditorWidth = 320;
    }
}