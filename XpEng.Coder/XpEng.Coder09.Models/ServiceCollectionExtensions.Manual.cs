// Hand-written. Created once by the DISetup template and never regenerated —
// edit freely.

using Microsoft.Extensions.DependencyInjection;
using XpEng.Coder80.Infrastructure;

namespace XpEng.Coder09.Models;

public static partial class ServiceCollectionExtensions {

    /// <summary>
    /// Registrations the attribute scan cannot express. Called by the generated
    /// AddRegistrations after this project's dependencies and its own scan have run.
    ///
    /// THREE WAYS TO REGISTER, and which to reach for:
    ///
    ///   [Register] on the class — the default. Works when you own the type and it has
    ///   a PUBLIC constructor, because the scan activates it by reflection:
    ///
    ///       [Register]                                    // as itself, Singleton
    ///       [Register(typeof(IThing))]                    // against an interface
    ///       [Register(typeof(IThing), ServiceLifetime.Transient)]
    ///
    ///   HERE — you own the type, but reflection cannot build it: a non-public
    ///   constructor, an options lambda, a lifetime decided at runtime. A factory
    ///   written here is compiled code in this assembly, so it can reach an internal
    ///   or protected constructor that reflection cannot:
    ///
    ///       services.AddSingleton<MainViewModel>(_ => new MainViewModel());
    ///       services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
    ///
    ///   IAddRegistrations — you do NOT own the type and cannot decorate it: a
    ///   scaffolded entity, generated code, something from a package. Implement the
    ///   interface in its own class under DI/; the scan finds every implementation, so
    ///   any number can coexist. This is the one most easily missed.
    ///
    /// To REPLACE a registration rather than add one — resolving a conflict, or
    /// substituting in a test — see IOverrideRegistrations. See also
    /// CoderFiles/README-DependencyInjection.md.
    /// </summary>
    static partial void AddManualRegistrations(IServiceCollection services) {
        // Example — delete or replace:
        // services.AddSingleton<SomeType>(_ => new SomeType());
        services.AddSingleton<MainModel>(_ => new MainModel());
    }
}
