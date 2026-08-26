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
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views
{
    /// <summary>
    /// Interaction logic for PlanItemView.xaml
    /// </summary>
    public partial class PlanItemView : UserControl
    {
        public PlanItemView()
        {
            InitializeComponent();
        }

        private void BrowseSetupSolutionFile_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Solution File",
                Filter = "Visual Studio Solution (*.sln;*.slnx)|*.sln;*.slnx|All Files (*.*)|*.*"
            };

            if (!string.IsNullOrWhiteSpace(poco.SetupSolutionFile)) {
                var directory = Path.GetDirectoryName(poco.SetupSolutionFile);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                    dialog.InitialDirectory = directory;
                    dialog.FileName = Path.GetFileName(poco.SetupSolutionFile);
                }
            }

            if (dialog.ShowDialog() == true) poco.SetupSolutionFile = dialog.FileName;
        }

        private void BrowseSetupTemplate_Click(object sender, RoutedEventArgs e) {
            if ((sender as FrameworkElement)?.DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Setup Template File",
                Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
            };

            if (!string.IsNullOrWhiteSpace(poco.SetupTemplatePath)) {
                var directory = Path.GetDirectoryName(poco.SetupTemplatePath);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory)) {
                    dialog.InitialDirectory = directory;
                    dialog.FileName = Path.GetFileName(poco.SetupTemplatePath);
                }
            }

            if (dialog.ShowDialog() == true) poco.SetupTemplatePath = dialog.FileName;
        }
    }
}
