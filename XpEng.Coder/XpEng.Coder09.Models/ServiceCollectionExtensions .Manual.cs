using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder80.Infrastructure;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;

namespace XpEng.Coder09.Models;

public static partial class ServiceCollectionExtensions {

    // MainViewModel's constructor is protected internal, so the reflection-based [Register]
    // scan cannot construct it. A factory can: same-assembly compiled code reaches a
    // non-public constructor, reflection does not.
    static partial void AddManualRegistrations(IServiceCollection services) {
        services.AddSingleton<MainModel>(_ => new MainModel());

    }
}
