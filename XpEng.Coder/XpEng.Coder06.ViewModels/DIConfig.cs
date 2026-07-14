using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder06.ViewModels {
    public class DIConfig {

        public static IServiceCollection Config(IServiceCollection serviceCollection) {
            serviceCollection.AddSingleton(provider => MainViewModel.Instance);
            serviceCollection = XpEng.Coder09.Models.DIConfig.Config(serviceCollection);

            return serviceCollection;
        }
    }
}
