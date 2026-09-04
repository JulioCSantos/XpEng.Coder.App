using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder06.ViewModels;

public static partial class ServiceCollectionExtensions {

    // MainViewModel's constructor is protected internal, so the reflection-based [Register]
    // scan cannot construct it. A factory can: same-assembly compiled code reaches a
    // non-public constructor, reflection does not.
    static partial void AddManualRegistrations(IServiceCollection services) {
        services.AddSingleton<MainViewModel>(_ => new MainViewModel());
    }
}