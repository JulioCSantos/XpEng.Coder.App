using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.ViewModels;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder03.Views {
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window {

        #region MainViewModel
        private MainViewModel? _mainViewModel;
        public MainViewModel MainViewModel {
            get { return _mainViewModel ??= DIExtensions.ServiceProvider.GetRequiredService<MainViewModel>(); }
            protected set => _mainViewModel = value;
        }
        #endregion MainViewModel

        public MainView() {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e) {
            // If the DataContext implements IDisposable (which our ViewModel now does), dispose it
            if (DataContext is IDisposable disposableViewModel) {
                disposableViewModel.Dispose();
            }

            base.OnClosed(e);
        }
    }
}
