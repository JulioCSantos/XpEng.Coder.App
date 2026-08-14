using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Pipes;
using XpEng.Coder80.Infrastructure.Interfaces;
using XpEng.Coder80.Infrastructure.Services;
using XpEng.Coder80.Infrastructure.T4Pipeline;
namespace XpEng.Coder12.Services.T4Pipeline {
    public enum T4HostStatus { NotStarted, Active, Disposed }
    public sealed class T4HostServer : IDisposable {
        private static readonly Lock InstanceLock = new();
        private static T4HostServer? _instance;
        public static bool HasInstance => _instance is { Status: not T4HostStatus.Disposed };
        public static T4HostServer Instance {
            get {
                lock (InstanceLock) {
                    if (_instance is { Status: T4HostStatus.Disposed }) _instance = null;
                    return _instance ??= new T4HostServer(DIExtensions.ServiceProvider.GetRequiredService<TemplateCompilerService>());
                }
            }
        }
        private IEngineLogger Logger => DIExtensions.ServiceProvider.GetRequiredService<IEngineLogger>();
        private readonly TemplateCompilerService _compiler;
        private readonly CancellationTokenSource _lifetimeCts = new();
        private readonly SemaphoreSlim _startGate = new(1, 1);
        private Task? _acceptLoop;
        public T4HostStatus Status { get; private set; } = T4HostStatus.NotStarted;
        private T4HostServer(TemplateCompilerService compiler) { _compiler = compiler; }
        public async Task EnsureStartedAsync(CancellationToken ct) {
            if (Status == T4HostStatus.Active) return;
            Logger.Log("[T4Host] EnsureStartedAsync waiting for start gate...");
            await _startGate.WaitAsync(ct).ConfigureAwait(false);
            try {
                if (Status == T4HostStatus.Active) return;
                Logger.Log("[T4Host] Starting prewarm...");
                await _compiler.PrewarmAsync(_lifetimeCts.Token).ConfigureAwait(false);
                Logger.Log("[T4Host] Prewarm complete. Starting accept loop.");
                _acceptLoop = Task.Run(() => AcceptLoopAsync(_lifetimeCts.Token), _lifetimeCts.Token);
                Status = T4HostStatus.Active;
                Logger.Log("[T4Host] Host is Active.");
            }
            finally { _startGate.Release(); }
        }
        private async Task AcceptLoopAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                var server = new NamedPipeServerStream(T4PipeProtocol.PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
                Logger.Log("[T4Host] Client connected.");
                _ = Task.Run(() => HandleClientAsync(server, ct), ct);
            }
        }
        private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken appCt) {
            var writeLock = new SemaphoreSlim(1, 1);
            try {
                while (!appCt.IsCancellationRequested && pipe.IsConnected) {
                    byte[] reqBytes;
                    try { reqBytes = await T4PipeProtocol.ReadFrameAsync(pipe, appCt).ConfigureAwait(false); }
                    catch (EndOfStreamException) { break; }
                    catch (IOException) { break; }
                    catch (OperationCanceledException) { break; }
                    _ = Task.Run(() => ProcessRequestAsync(pipe, writeLock, reqBytes, appCt), appCt);
                }
            }
            finally { pipe.Dispose(); writeLock.Dispose(); Logger.Log("[T4Host] Client connection closed."); }
        }
        private async Task ProcessRequestAsync(NamedPipeServerStream pipe, SemaphoreSlim writeLock, byte[] reqBytes, CancellationToken ct) {
            T4GenerationResponse response;
            try {
                var request = MessagePackSerializer.Deserialize<T4GenerationRequest>(reqBytes, T4WireOptions.Options);
                Logger.Log($"[T4Host] Request {request.RequestId} received for '{request.TemplatePath}'");
                var started = DateTime.UtcNow;
                response = new T4GenerationResponse { RequestId = request.RequestId };
                try {
                    Logger.Log($"[T4Host] Request {request.RequestId}: compiling...");
                    await _compiler.GetOrCompileAsync(request.TemplateContentHash, request.TemplatePath, request.TemplateContent, ct).ConfigureAwait(false);
                    Logger.Log($"[T4Host] Request {request.RequestId}: compiled at {(DateTime.UtcNow - started).TotalMilliseconds}ms. Executing Process()...");
                    response.GeneratedContent = _compiler.Process(request.TemplateContentHash, request.SessionParameters);
                    Logger.Log($"[T4Host] Request {request.RequestId}: Process() done at {(DateTime.UtcNow - started).TotalMilliseconds}ms.");
                    response.Success = true;
                }
                catch (Exception ex) { Logger.Log($"[T4Host] Request {request.RequestId} FAILED: {ex}"); response.Success = false; response.ErrorMessage = ex.Message; }
                response.ElapsedMilliseconds = (long)(DateTime.UtcNow - started).TotalMilliseconds;
            }
            catch (Exception ex) { Logger.Log($"[T4Host] Deserialize failed: {ex}"); response = new T4GenerationResponse { Success = false, ErrorMessage = $"Deserialization failed: {ex.Message}" }; }
            try {
                var respBytes = MessagePackSerializer.Serialize(response, T4WireOptions.Options);
                await writeLock.WaitAsync(ct).ConfigureAwait(false);
                try { await T4PipeProtocol.WriteFrameAsync(pipe, respBytes, ct).ConfigureAwait(false); Logger.Log($"[T4Host] Response sent for request {response.RequestId}."); }
                finally { writeLock.Release(); }
            }
            catch (Exception ex) { Logger.Log($"[T4Host] Failed to send response for request {response.RequestId}: {ex.Message}"); }
        }
        public void Dispose() { _lifetimeCts.Cancel(); _lifetimeCts.Dispose(); Status = T4HostStatus.Disposed; Logger.Log("[T4Host] Disposed."); }
    }
}