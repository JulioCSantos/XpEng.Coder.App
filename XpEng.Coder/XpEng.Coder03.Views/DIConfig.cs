using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder03.Views.Views;

namespace XpEng.Coder03.Views;

public class DIConfig {
    public static IServiceCollection Config(IServiceCollection serviceCollection) {
        serviceCollection.AddSingleton(typeof(MainView));

        serviceCollection = XpEng.Coder06.ViewModels.DIConfig.Config(serviceCollection);

        // Call the Data project config WITHOUT passing a string.
        // It will automatically use the default connection string hidden inside XpEng.Coder06.Data.
        serviceCollection = XpEng.Coder06.Data.DIConfig.Config(serviceCollection);

        return serviceCollection;
    }
}