using XpEng.Coder03.Views.Views;
using XpEng.Coder06.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder03.Views
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Configuration
        private IConfiguration? _configuration;
        public IConfiguration Configuration {
            get {
                if (_configuration != null) { return _configuration; }

                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appSettings.json", false, true);
                _configuration = builder.Build();

                return _configuration;
            }
            protected set => _configuration = value;
        }
        #endregion Configuration

        protected override void OnStartup(StartupEventArgs e) {
            //Set DI factories
            IServiceCollection servColl = DIExtensions.ServiceCollection;
            servColl = DIConfig.Config(servColl);
            
            // Startup MainWindow
            var mainView = DIExtensions.ServiceProvider.GetRequiredService<MainView>();
            mainView.Show();
            base.OnStartup(e);

        }
    }

}
