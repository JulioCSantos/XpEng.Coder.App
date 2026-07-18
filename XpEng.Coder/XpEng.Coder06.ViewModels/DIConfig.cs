using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder06.ViewModels {
    public class DIConfig {

        public static IServiceCollection Config(IServiceCollection serviceCollection) {
            serviceCollection = Coder09.Models.DIConfig.Config(serviceCollection);
            serviceCollection.AddSingleton(provider => MainViewModel.Instance);

            return serviceCollection;
        }
    }
}
