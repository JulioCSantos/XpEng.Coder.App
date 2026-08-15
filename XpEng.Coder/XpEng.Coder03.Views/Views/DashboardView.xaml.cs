using Microsoft.Win32;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using XpEng.Coder03.Views.Controls;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {

        private const int MaxCardColumns = 3;

        #region Properties
        private readonly DashboardViewModel VM;
        #endregion Properties

        #region Constructors
        public DashboardView() {
            InitializeComponent();
            VM = (this.DataContext as DashboardViewModel)!;

            // Listen for the ViewModel's request to touch the clipboard
            VM.CopyToClipboardRequested += OnCopyToClipboardRequested!;
        }
        #endregion Constructors

        #region Event Handlers & Methods

        // SourceDirectory (top of the tree): no ancestor default, browse from wherever it currently points.
        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is SourceDirectoryPoco poco) {
                SelectDirectory("Select Source Directory (Monitored)", poco.SourceDirectory, path => {
                    poco.SourceDirectory = path;
                });
            }
        }

        private void SelectDirectory(string title, string currentPath, System.Action<string> onFolderSelected) {
            var dialog = new OpenFolderDialog {
                Title = title
            };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) {
                onFolderSelected(dialog.FolderName);
            }
        }

        // Wires up reactivity for a TargetTemplates ListBox on first load: reacts to items being
        // added/removed, and to each item's own display text changing (typing, or picking a new
        // path via the dialog on an existing card) — not just window resize.
        private void TargetTemplatesListBox_Loaded(object sender, RoutedEventArgs e) {
            if (sender is not ListBox listBox) return;

            if (listBox.ItemsSource is INotifyCollectionChanged incc) {
                incc.CollectionChanged += (_, args) => {
                    if (args.NewItems != null) {
                        foreach (TargetTemplatePoco poco in args.NewItems) SubscribeToPocoWidthChanges(poco, listBox);
                    }
                    RefreshCardWidth(listBox);
                };
            }
            if (listBox.ItemsSource is System.Collections.Generic.IEnumerable<TargetTemplatePoco> items) {
                foreach (var poco in items) SubscribeToPocoWidthChanges(poco, listBox);
            }

            RefreshCardWidth(listBox);
        }

        private void SubscribeToPocoWidthChanges(TargetTemplatePoco poco, ListBox listBox) {
            poco.PropertyChanged += (_, args) => {
                if (args.PropertyName is nameof(TargetTemplatePoco.TargetDirectoryDisplay) or nameof(TargetTemplatePoco.TemplatePathDisplay)) {
                    RefreshCardWidth(listBox);
                }
            };
        }

        private void TargetTemplatesListBox_SizeChanged(object sender, SizeChangedEventArgs e) {
            RefreshCardWidth(sender as ListBox);
        }

        private void RefreshCardWidth(ListBox? listBox) {
            if (listBox == null) return;
            Dispatcher.BeginInvoke(new Action(() => 
                RefreshCardWidthCore(listBox)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void RefreshCardWidthCore(ListBox listBox) {
            if (listBox.Tag is not WrapPanel wrapPanel) {
                wrapPanel = FindVisualChild<WrapPanel>(listBox)!;
                listBox.Tag = wrapPanel;
            }
            double minWidth = MeasureMaxRequiredCardWidth(listBox);
            wrapPanel.ItemWidth = ResponsiveGridCalculator.CalculateItemWidth(listBox.ActualWidth, minWidth, MaxCardColumns);
        }
        // Every card in a WrapPanel with ItemWidth set shares one uniform width, so the value
        // used has to be the widest real requirement across all currently-rendered cards —
        // not an arbitrary constant.
        private double MeasureMaxRequiredCardWidth(ListBox listBox) {
            double max = 0;
            foreach (var item in listBox.Items) {
                if (listBox.ItemContainerGenerator.ContainerFromItem(item) is not DependencyObject container) continue;
                var cardView = FindVisualChild<TargetTemplateItemView>(container);
                if (cardView != null) max = Math.Max(max, cardView.MeasureRequiredCardWidth());
            }
            return max;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}