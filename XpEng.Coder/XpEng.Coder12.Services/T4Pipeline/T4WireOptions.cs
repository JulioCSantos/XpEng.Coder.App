using MessagePack;
using MessagePack.Resolvers;
namespace XpEng.Coder12.Services.T4Pipeline {
    internal static class T4WireOptions {
        public static readonly MessagePackSerializerOptions Options 
            = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
    }
}