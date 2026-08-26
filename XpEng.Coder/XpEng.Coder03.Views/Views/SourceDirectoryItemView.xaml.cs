using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views
{
    /// <summary>
    /// Interaction logic for SourceDirectoryItemView.xaml
    /// </summary>
    public partial class SourceDirectoryItemView : UserControl
    {
        public SourceDirectoryItemView()
        {
            InitializeComponent();
        }


        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not SourceDirectoryPoco poco) return;

            SelectDirectory(
                "Select Source Directory (Monitored)",
                poco.SourceDirectory,
                path => poco.SourceDirectory = path);
        }

        private void SelectDirectory(string title, string currentPath, Action<string> onFolderSelected) {
            var dialog = new OpenFolderDialog { Title = title };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath)) {
                dialog.InitialDirectory = currentPath;
            }

            if (dialog.ShowDialog() == true) onFolderSelected(dialog.FolderName);
        }
    }
}
