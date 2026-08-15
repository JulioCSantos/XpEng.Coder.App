using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XpEng.Coder03.Views.Controls;

public partial class PathPickerControl : UserControl {
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(PathPickerControl));
    public static readonly DependencyProperty PathValueProperty = DependencyProperty.Register(nameof(PathValue), typeof(string), typeof(PathPickerControl), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty DisplayValueProperty = DependencyProperty.Register(nameof(DisplayValue), typeof(string), typeof(PathPickerControl));
    public static readonly DependencyProperty DefaultPathProperty = DependencyProperty.Register(nameof(DefaultPath), typeof(string), typeof(PathPickerControl));
    public static readonly DependencyProperty DialogKindProperty = DependencyProperty.Register(nameof(DialogKind), typeof(PickerDialogKind), typeof(PathPickerControl), new PropertyMetadata(PickerDialogKind.Folder));
    public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register(nameof(FileFilter), typeof(string), typeof(PathPickerControl), new PropertyMetadata("All Files (*.*)|*.*"));
    public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register(nameof(DialogTitle), typeof(string), typeof(PathPickerControl), new PropertyMetadata("Select Path"));

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string PathValue { get => (string)GetValue(PathValueProperty); set => SetValue(PathValueProperty, value); }
    public string DisplayValue { get => (string)GetValue(DisplayValueProperty); set => SetValue(DisplayValueProperty, value); }
    public string DefaultPath { get => (string)GetValue(DefaultPathProperty); set => SetValue(DefaultPathProperty, value); }
    public PickerDialogKind DialogKind { get => (PickerDialogKind)GetValue(DialogKindProperty); set => SetValue(DialogKindProperty, value); }
    public string FileFilter { get => (string)GetValue(FileFilterProperty); set => SetValue(FileFilterProperty, value); }
    public string DialogTitle { get => (string)GetValue(DialogTitleProperty); set => SetValue(DialogTitleProperty, value); }

    public PathPickerControl() { InitializeComponent(); }

    // Natural (unwrapped) width of the current display text, e.g. "how wide would this
    // shortened path be if nothing constrained it." Used by TargetTemplateItemView to size
    // the whole card to its real content instead of a guessed constant.
    public double MeasureDisplayTextWidth() {
        PathDisplayText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Max(PathDisplayText.DesiredSize.Width, CardSizingDefaults.MinTextWidthWhenEmpty);
    }

    private void PickButton_Click(object sender, RoutedEventArgs e) {
        string initial = !string.IsNullOrWhiteSpace(PathValue) ? PathValue : DefaultPath;

        if (DialogKind == PickerDialogKind.Folder) {
            var dialog = new OpenFolderDialog { Title = DialogTitle };
            if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial)) dialog.InitialDirectory = initial;
            if (dialog.ShowDialog() == true) PathValue = dialog.FolderName;
        }
        else {
            var dialog = new OpenFileDialog { Title = DialogTitle, Filter = FileFilter };
            if (!string.IsNullOrWhiteSpace(PathValue)) {
                var dir = Path.GetDirectoryName(PathValue);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) { dialog.InitialDirectory = dir; dialog.FileName = Path.GetFileName(PathValue); }
            }
            else if (!string.IsNullOrWhiteSpace(DefaultPath) && Directory.Exists(DefaultPath)) dialog.InitialDirectory = DefaultPath;
            if (dialog.ShowDialog() == true) PathValue = dialog.FileName;
        }
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        PathEditBox.Focus();
        Keyboard.Focus(PathEditBox);
        PathEditBox.CaretIndex = PathEditBox.Text?.Length ?? 0;
    }
}