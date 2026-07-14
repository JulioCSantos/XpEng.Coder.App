using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
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
    }
}