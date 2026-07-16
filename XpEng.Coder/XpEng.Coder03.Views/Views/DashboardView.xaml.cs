using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using XpEng.Coder06.ViewModels;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {
        public DashboardView() {
            InitializeComponent();
        }

        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFolderDialog {
                Title = "Select Source Project Directory"
            };

            // Explicitly cast to bool? to handle the object return type safely
            bool? result = dialog.ShowDialog() as bool?;

            if (result == true) {
                // Correct C# pattern matching (no more 'auto')
                if (DataContext is DashboardViewModel vm) {
                    vm.SourceDirectory = dialog.FolderName;
                }
            }
        }

        private void BrowseTarget_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFolderDialog {
                Title = "Select Target Output Directory"
            };

            bool? result = dialog.ShowDialog() as bool?;

            if (result == true) {
                if (DataContext is DashboardViewModel vm) {
                    vm.TargetDirectory = dialog.FolderName;
                }
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