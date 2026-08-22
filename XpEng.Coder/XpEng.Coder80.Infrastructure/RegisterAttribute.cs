using Microsoft.Extensions.DependencyInjection;

namespace XpEng.Coder80.Infrastructure;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RegisterAttribute : Attribute {
    public Type? ServiceType { get; }
    public ServiceLifetime Lifetime { get; }

    public RegisterAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton) {
        Lifetime = lifetime;
    }

    public RegisterAttribute(Type serviceType, ServiceLifetime lifetime = ServiceLifetime.Singleton) {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
}