using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XpEng.Coder06.ViewModels;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {

        // ReSharper disable once InconsistentNaming
        private DashboardViewModel VM => MainViewModel.Instance.DashboardViewModel;

        public DashboardView() {
            InitializeComponent();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            // Pass the current SourceDirectory to the helper
            SelectDirectory("Select Source Project Directory", VM.SourceDirectory, path => {
                VM.SourceDirectory = path;
            });
        }

        private void BrowseTarget_Click(object sender, RoutedEventArgs e) {
            // Pass the current TargetDirectory to the helper
            SelectDirectory("Select Target Output Directory", VM.TargetDirectory, path => {
                VM.TargetDirectory = path;
            });
        }

        private void BrowseTemplate_Click(object sender, RoutedEventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Title = "Select T4 Template File",
                Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
            };

            // If we already have a path, set the dialog to open there
            if (!string.IsNullOrWhiteSpace(VM.TemplatePath)) {
                var directory = Path.GetDirectoryName(VM.TemplatePath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                    dialog.InitialDirectory = directory;
                    // Optionally, pre-fill the file name in the dialog box
                    dialog.FileName = Path.GetFileName(VM.TemplatePath);
                }
            }

            if (dialog.ShowDialog() == true) {
                VM.TemplatePath = dialog.FileName;
            }
        }

        // Shared Helper Method updated to accept and evaluate the current path
        private void SelectDirectory(string title, string currentPath, System.Action<string> onFolderSelected) {
            var dialog = new Microsoft.Win32.OpenFolderDialog {
                Title = title
            };

            // If the path isn't empty and actually exists on the drive, set it as the starting point
            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) {
                onFolderSelected(dialog.FolderName);
            }
        }

        private void LogsListBox_KeyDown(object sender, KeyEventArgs e) {
            // Check if the user pressed Ctrl + C
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) {
                var listBox = sender as ListBox;

                if (listBox != null && listBox.SelectedItems.Count > 0) {
                    // Cast the selected items back to strings and join them with a line break
                    var selectedLogs = listBox.SelectedItems.Cast<string>();
                    string textToCopy = string.Join(Environment.NewLine, selectedLogs);

                    // Push to the Windows Clipboard
                    Clipboard.SetText(textToCopy);
                }
            }
        }
    }
}