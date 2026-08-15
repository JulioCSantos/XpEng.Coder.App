using System.Windows.Controls;
using XpEng.Coder03.Views.Controls;

namespace XpEng.Coder03.Views.Views;

public partial class TargetTemplateItemView : UserControl {
    public TargetTemplateItemView() { InitializeComponent(); }

    // Card's own required width: whichever of its two paths needs more room, plus a fixed
    // allowance for chrome (border padding, the "..." button column) that measurement can't
    // capture directly. Approximate by design — good enough to avoid truncating real content
    // without needing to hand-measure every pixel of the card's layout.
    public double MeasureRequiredCardWidth() {
        double textWidth = Math.Max(TargetPicker.MeasureDisplayTextWidth(), TemplatePicker.MeasureDisplayTextWidth());
        return Math.Max(textWidth + CardSizingDefaults.CardChromeOverhead, CardSizingDefaults.MinCardWidth);
    }
}