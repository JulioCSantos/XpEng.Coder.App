using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class SourceDirectoryItemView : UserControl {
        #region constructors
        public SourceDirectoryItemView() {
            InitializeComponent();
        }
        #endregion constructors

        #region methods
        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not SourceDirectoryPoco poco) return;

            SelectDirectory(
                "Select Source Directory (Monitored)",
                poco.SourceDirectory,
                path => poco.SourceDirectory = path);
        }

        private static void SelectDirectory(string title, string currentPath, Action<string> onFolderSelected) {
            var dialog = new OpenFolderDialog { Title = title };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) onFolderSelected(dialog.FolderName);
        }
        #endregion methods
    }
}