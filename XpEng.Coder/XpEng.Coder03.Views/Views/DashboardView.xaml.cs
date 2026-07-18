using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {

        private readonly DashboardViewModel VM;

        public DashboardView() {
            InitializeComponent();
            VM = (this.DataContext as DashboardViewModel)!;

            // Listen for the ViewModel's request to touch the clipboard
            VM.CopyToClipboardRequested += OnCopyToClipboardRequested!;
        }

        // Fired from inside the Expander Header (DataContext is PlanOrchestratorViewModel)
        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is PlanOrchestratorViewModel planVm) {
                SelectDirectory("Select Source Project Directory", planVm.Model.SourceDirectory.FullName, path => {
                    planVm.Model.SourceDirectory = new DirectoryInfo(path);
                });
            }
        }

        // Fired from inside the TemplateTarget list (DataContext is TemplateTarget)
        private void BrowseTarget_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTarget target) {
                SelectDirectory("Select Target Output Directory", target.TargetDirectory.FullName, path => {
                    target.TargetDirectory = new DirectoryInfo(path);
                });
            }
        }

        // Fired from inside the TemplateTarget list (DataContext is TemplateTarget)
        private void BrowseTemplate_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTarget target) {
                var dialog = new Microsoft.Win32.OpenFileDialog {
                    Title = "Select T4 Template File",
                    Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
                };

                string currentPath = target.TemplatePath.FullName;
                if (!string.IsNullOrWhiteSpace(currentPath)) {
                    var directory = Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                        dialog.InitialDirectory = directory;
                        dialog.FileName = Path.GetFileName(currentPath);
                    }
                }

                if (dialog.ShowDialog() == true) {
                    target.TemplatePath = new FileInfo(dialog.FileName);
                }
            }
        }

        private void SelectDirectory(string title, string currentPath, System.Action<string> onFolderSelected) {
            var dialog = new Microsoft.Win32.OpenFolderDialog {
                Title = title
            };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) {
                onFolderSelected(dialog.FolderName);
            }
        }

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
    }
}