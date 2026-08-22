using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using XpEng.Coder03.Views.Views;
using XpEng.Coder06.ViewModels;
using XpEng.Coder12.Services.T4Pipeline;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder03.Views;

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

        DIExtensions.ServiceCollection.AddViews();
        DIExtensions.Build();
        _ = WarmUpT4HostAsync();
        var mainView = DIExtensions.ServiceProvider.GetRequiredService<MainView>();
        mainView.Show();
        base.OnStartup(e);
    }

    private static async Task WarmUpT4HostAsync() {
        try { await T4HostServer.Instance.EnsureStartedAsync(CancellationToken.None).ConfigureAwait(false); }
        catch (Exception ex) { DIExtensions.ServiceProvider.GetRequiredService<IEngineLogger>().Log($"T4 host warm-up failed: {ex.Message}"); }
    }

    protected override void OnExit(ExitEventArgs e) {
        if (T4HostServer.HasInstance) T4HostServer.Instance.Dispose();
        base.OnExit(e);
    }
}
