using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Media;
//using XpEng.Coder03.Views.Controls;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {
        //private const int MaxCardColumns = 3;

        #region Properties
        private readonly DashboardViewModel VM;
        #endregion Properties

        #region Constructors
        public DashboardView() {
            InitializeComponent();

            VM = (DataContext as DashboardViewModel)!;
            VM.CopyToClipboardRequested += OnCopyToClipboardRequested!;
        }
        #endregion Constructors

        #region Event Handlers & Methods
        private void BrowseSetupSolutionFile_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Solution File",
                Filter = "Visual Studio Solution (*.sln;*.slnx)|*.sln;*.slnx|All Files (*.*)|*.*"
            };

            if (!string.IsNullOrWhiteSpace(poco.SetupSolutionFile)) {
                var directory = Path.GetDirectoryName(poco.SetupSolutionFile);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                    dialog.InitialDirectory = directory;
                    dialog.FileName = Path.GetFileName(poco.SetupSolutionFile);
                }
            }

            if (dialog.ShowDialog() == true) poco.SetupSolutionFile = dialog.FileName;
        }

        private void BrowseSetupTemplate_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Setup Template File",
                Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
            };

            if (!string.IsNullOrWhiteSpace(poco.SetupTemplatePath)) {
                var directory = Path.GetDirectoryName(poco.SetupTemplatePath);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                    dialog.InitialDirectory = directory;
                    dialog.FileName = Path.GetFileName(poco.SetupTemplatePath);
                }
            }

            if (dialog.ShowDialog() == true) poco.SetupTemplatePath = dialog.FileName;
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not SourceDirectoryPoco poco) return;

            SelectDirectory(
                "Select Source Directory (Monitored)",
                poco.SourceDirectory,
                path => poco.SourceDirectory = path);
        }

        private void SelectDirectory(string title, string currentPath, Action<string> onFolderSelected) {
            var dialog = new OpenFolderDialog { Title = title };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) onFolderSelected(dialog.FolderName);
        }

        //// Card width depends on available space, not on the length of the paths inside the card.
        //private void TargetTemplatesListBox_Loaded(object sender, RoutedEventArgs e) {
        //    RefreshCardWidth(sender as ListBox);
        //}

        //private void TargetTemplatesListBox_SizeChanged(object sender, SizeChangedEventArgs e) {
        //    RefreshCardWidth(sender as ListBox);
        //}

        //private void RefreshCardWidth(ListBox? listBox) {
        //    if (listBox == null) return;

        //    Dispatcher.BeginInvoke(
        //        new Action(() => RefreshCardWidthCore(listBox)),
        //        System.Windows.Threading.DispatcherPriority.Background);
        //}

        //private void RefreshCardWidthCore(ListBox listBox) {
        //    if (listBox.ActualWidth <= 0) return;

        //    WrapPanel? wrapPanel = listBox.Tag as WrapPanel;

        //    if (wrapPanel == null) {
        //        wrapPanel = FindVisualChild<WrapPanel>(listBox);
        //        if (wrapPanel == null) return;

        //        listBox.Tag = wrapPanel;
        //    }

        //    double itemWidth = ResponsiveGridCalculator.CalculateItemWidth(
        //        listBox.ActualWidth,
        //        CardSizingDefaults.MinCardWidth,
        //        CardSizingDefaults.PreferredCardWidth,
        //        CardSizingDefaults.MaxColumns);

        //    if (double.IsNaN(itemWidth) || double.IsInfinity(itemWidth) || itemWidth <= 0) return;

        //    wrapPanel.ItemWidth = itemWidth;
        //}

        //private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
        //    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
        //        DependencyObject child = VisualTreeHelper.GetChild(parent, i);

        //        if (child is T match) return match;

        //        T? found = FindVisualChild<T>(child);
        //        if (found != null) return found;
        //    }

        //    return null;
        //}

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}