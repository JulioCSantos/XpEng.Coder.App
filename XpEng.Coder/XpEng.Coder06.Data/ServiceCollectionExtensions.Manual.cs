using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder06.Data;

public static partial class ServiceCollectionExtensions {

    // The Data project owns its default. Previously an optional parameter on AddData;
    // AddRegistrations has a fixed signature, so an override now goes through
    // IOverrideRegistrations or services.Replace(...) rather than an argument.
    private const string DefaultConnection =
        "Data Source=asus-strange2;Initial Catalog=XpEng.CoderDb;Integrated Security=True;Encrypt=True;Trust Server Certificate=True";

    static partial void AddManualRegistrations(IServiceCollection services) {
        // Nothing consumes DefaultConnection yet — no DbContext is registered in Coder.App.
        // When one is added it belongs here, since options-lambda construction cannot be
        // expressed as a [Register] attribute:
        //
        //     services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(DefaultConnection));
    }
}