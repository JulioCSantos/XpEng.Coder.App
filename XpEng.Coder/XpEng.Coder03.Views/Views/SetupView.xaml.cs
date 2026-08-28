using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class SetupView : UserControl {
        #region constructors
        public SetupView() {
            InitializeComponent();
        }
        #endregion constructors

        #region methods
        private void BrowseSetupSolutionFile_Click(object sender, RoutedEventArgs e) {
            if (DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Solution File",
                Filter = "Visual Studio Solution (*.sln;*.slnx)|*.sln;*.slnx|All Files (*.*)|*.*"
            };

            SetInitialFile(dialog, poco.SetupSolutionFile);
            if (dialog.ShowDialog() == true) poco.SetupSolutionFile = dialog.FileName;
        }

        private void BrowseSetupTemplate_Click(object sender, RoutedEventArgs e) {
            if (DataContext is not PlanOrchestratorPoco poco) return;

            var dialog = new OpenFileDialog {
                Title = "Select Setup Template File",
                Filter = "T4 Templates (*.tt)|*.tt|All Files (*.*)|*.*"
            };

            SetInitialFile(dialog, poco.SetupTemplatePath);
            if (dialog.ShowDialog() == true) poco.SetupTemplatePath = dialog.FileName;
        }

        private static void SetInitialFile(OpenFileDialog dialog, string? filePath) {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

            dialog.InitialDirectory = directory;
            dialog.FileName = Path.GetFileName(filePath);
        }
        #endregion methods
    }
}