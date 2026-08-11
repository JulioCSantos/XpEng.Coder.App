using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {

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

        // Target Directory: defaults to the owning Source Directory's calculated SolutionsDirectory
        // when the field is still empty, so the dialog doesn't open at a meaningless location.
        private void BrowseTarget_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) {
                var parentSource = FindAncestorDataContext<SourceDirectoryPoco>(sender as DependencyObject);
                string initialPath = !string.IsNullOrWhiteSpace(poco.TargetDirectory) ? poco.TargetDirectory : parentSource?.SolutionsDirectory ?? string.Empty;

                SelectDirectory("Select Target Output Directory", initialPath, path => {
                    poco.TargetDirectory = path;
                });
            }
        }

        // Template: same ancestor-default behavior as the target directory browse above.
        private void BrowseTemplate_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) {
                var parentSource = FindAncestorDataContext<SourceDirectoryPoco>(sender as DependencyObject);

                var dialog = new OpenFileDialog {
                    Title = "Select T4 Template File",
                    Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
                };

                string currentPath = poco.TemplatePath;
                if (!string.IsNullOrWhiteSpace(currentPath)) {
                    var directory = Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                        dialog.InitialDirectory = directory;
                        dialog.FileName = Path.GetFileName(currentPath);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(parentSource?.SolutionsDirectory) && Directory.Exists(parentSource.SolutionsDirectory)) {
                    dialog.InitialDirectory = parentSource.SolutionsDirectory;
                }

                if (dialog.ShowDialog() == true) {
                    poco.TemplatePath = dialog.FileName;
                }
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

        // Walks up the visual tree looking for the nearest ancestor whose DataContext is of type T.
        // Used to find a TargetTemplatePoco's owning SourceDirectoryPoco for browse-dialog defaults,
        // since the Poco tree does not hold parent back-references.
        private static T? FindAncestorDataContext<T>(DependencyObject? child) where T : class {
            var parent = child != null ? VisualTreeHelper.GetParent(child) : null;
            while (parent != null) {
                if (parent is FrameworkElement fe && fe.DataContext is T match) return match;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // Clicking the shortened-path overlay forwards focus to the real, full-path TextBox
        // underneath it. GotFocus/LostFocus below set the poco's IsEditing* flag.
        private void DisplayOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe && fe.Tag is TextBox editBox) {
                editBox.Focus();
                Keyboard.Focus(editBox);
                editBox.CaretIndex = editBox.Text?.Length ?? 0;
            }
        }

        private void TargetDirectoryEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) poco.IsEditingTargetDirectory = true;
        }

        private void TargetDirectoryEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) poco.IsEditingTargetDirectory = false;
        }

        private void TemplatePathEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) poco.IsEditingTemplatePath = true;
        }

        private void TemplatePathEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TargetTemplatePoco poco) poco.IsEditingTemplatePath = false;
        }

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}