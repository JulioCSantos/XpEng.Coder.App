using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder06.ViewModels;
using XpEng.Coder88.Infrastructure.Services;

namespace XpEng.Coder03.Views
{
    public class ViewModelLocator {

        #region singleton
        public static ViewModelLocator Instance { get; } = new ViewModelLocator();

        public static ViewModelLocator GetInstance() { return Instance; }

        private ViewModelLocator() { }
        #endregion singleton

        public MainViewModel MainViewModel => DIExtensions.ServiceProvider.GetRequiredService<MainViewModel>();
        public CounterViewModel CounterViewModel => DIExtensions.ServiceProvider.GetRequiredService<CounterViewModel>();

        //private bool IsInDesignMode() {
        //    return DesignerProperties.GetIsInDesignMode(dummy);
        //}
    }
    //if (IsInDesignMode()) {
    //    return new MockMainViewModel();
    //}
}
