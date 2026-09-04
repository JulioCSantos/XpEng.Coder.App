using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure.Services;

public static class DesignTimeSingleton<T> where T : class {
    private static readonly Lock _lock = new();
    private static T? _designTimeFallback;

    public static T Resolve(Func<T> constructFallback) {
        try {
            return ServiceLocator.CurrentProvider.GetRequiredService<T>();
        }
        catch {
            if (!DesignTimeDetector.IsInDesignMode) throw;
            lock (_lock) {
                return _designTimeFallback ??= constructFallback();
            }
        }
    }
}