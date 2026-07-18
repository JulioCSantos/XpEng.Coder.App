using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is PlanOrchestratorPoco poco) {
                SelectDirectory("Select Source Project Directory", poco.SourceDirectory, path => {
                    poco.SourceDirectory = path;
                });
            }
        }

        private void BrowseTarget_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) {
                SelectDirectory("Select Target Output Directory", poco.TargetDirectory, path => {
                    poco.TargetDirectory = path;
                });
            }
        }

        private void BrowseTemplate_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) {
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

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}