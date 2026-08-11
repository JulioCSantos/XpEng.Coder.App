using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        // Clicking the shortened-path overlay forwards focus to the real, full-path TextBox
        // underneath it. GotFocus/LostFocus below (not this handler) set the poco's IsEditing*
        // flag, so the flag reflects actual keyboard focus regardless of how it was reached.
        private void DisplayOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe && fe.Tag is TextBox editBox) {
                editBox.Focus();
                Keyboard.Focus(editBox);
                editBox.CaretIndex = editBox.Text?.Length ?? 0;
            }
        }

        private void SourceDirectoryEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is PlanOrchestratorPoco poco) poco.IsEditingSourceDirectory = true;
        }

        private void SourceDirectoryEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is PlanOrchestratorPoco poco) poco.IsEditingSourceDirectory = false;
        }

        private void TargetDirectoryEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) poco.IsEditingTargetDirectory = true;
        }

        private void TargetDirectoryEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) poco.IsEditingTargetDirectory = false;
        }

        private void TemplatePathEditBox_GotFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) poco.IsEditingTemplatePath = true;
        }

        private void TemplatePathEditBox_LostFocus(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is TemplateTargetPoco poco) poco.IsEditingTemplatePath = false;
        }

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}