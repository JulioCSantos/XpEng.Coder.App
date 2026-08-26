using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Media;
//using XpEng.Coder03.Views.Controls;
using XpEng.Coder06.ViewModels;
using XpEng.Coder09.Models.Transport;

namespace XpEng.Coder03.Views.Views {
    public partial class DashboardView : UserControl {
        //private const int MaxCardColumns = 3;

        #region Properties
        private readonly DashboardViewModel VM;
        #endregion Properties

        #region Constructors
        public DashboardView() {
            InitializeComponent();

            VM = (DataContext as DashboardViewModel)!;
            VM.CopyToClipboardRequested += OnCopyToClipboardRequested!;
        }
        #endregion Constructors

        #region Event Handlers & Methods
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

        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}