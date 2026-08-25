using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace XpEng.Coder03.Views.Views {
    public partial class TargetTemplateItemView : UserControl {
        public TargetTemplateItemView() {
            InitializeComponent();
        }


        private void BrowseTargetDirectory_Click(
            object sender,
            RoutedEventArgs e) {

            var dialog =
                new OpenFolderDialog {
                    Title = "Select Target Output Directory"
                };

            var vm = DataContext as dynamic;

            if (vm != null &&
                !string.IsNullOrWhiteSpace(vm.TargetDirectory) &&
                Directory.Exists(vm.TargetDirectory)) {

                dialog.InitialDirectory =
                    vm.TargetDirectory;
            }

            if (dialog.ShowDialog() == true &&
                vm != null) {

                vm.TargetDirectory =
                    dialog.FolderName;
            }
        }


        private void BrowseTemplate_Click(
            object sender,
            RoutedEventArgs e) {

            var dialog =
                new OpenFileDialog {
                    Title = "Select T4 Template File",
                    Filter =
                        "T4 Templates (*.tt)|*.tt|" +
                        "All Files (*.*)|*.*"
                };

            var vm = DataContext as dynamic;

            if (vm != null &&
                !string.IsNullOrWhiteSpace(vm.TemplatePath)) {

                string? directory =
                    Path.GetDirectoryName(
                        (string)vm.TemplatePath);

                if (!string.IsNullOrWhiteSpace(directory) &&
                    Directory.Exists(directory)) {

                    dialog.InitialDirectory =
                        directory;

                    dialog.FileName =
                        Path.GetFileName(
                            (string)vm.TemplatePath);
                }
            }

            if (dialog.ShowDialog() == true &&
                vm != null) {

                vm.TemplatePath =
                    dialog.FileName;
            }
        }
    }
}