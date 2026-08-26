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
        private void OnCopyToClipboardRequested(object sender, string textToCopy) {
            Clipboard.SetText(textToCopy);
        }
        #endregion Event Handlers & Methods
    }
}