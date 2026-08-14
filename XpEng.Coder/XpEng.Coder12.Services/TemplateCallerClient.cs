using System.Collections.Concurrent;
using System.IO.Pipes;
using MessagePack;
using XpEng.Coder80.Infrastructure;
namespace XpEng.Coder12.Services {
    public sealed class TemplateCallerClient : IAsyncDisposable {
        private readonly ConcurrentDictionary<int, TaskCompletionSource<T4GenerationResponse>> _pending = new();
        private NamedPipeClientStream? _pipe;
        private Task? _readLoop;
        private T4HostServer? _connectedHost;
        private int _nextRequestId;
        private readonly SemaphoreSlim _connectGate = new(1, 1);
        public async Task<T4GenerationResponse> GenerateAsync(string templatePath, string templateContent, string templateHash, Dictionary<string, string> sessionParameters, string outputFilePath, CancellationToken ct) {
            await EnsureConnectedAsync(ct).ConfigureAwait(false);
            var requestId = Interlocked.Increment(ref _nextRequestId);
            var request = new T4GenerationRequest { RequestId = requestId, TemplatePath = templatePath, TemplateContent = templateContent, TemplateContentHash = templateHash, SessionParameters = sessionParameters, OutputFilePath = outputFilePath };
            var tcs = new TaskCompletionSource<T4GenerationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;
            var bytes = MessagePackSerializer.Serialize(request, T4WireOptions.Options);
            await T4PipeProtocol.WriteFrameAsync(_pipe!, bytes, ct).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }
        private async Task EnsureConnectedAsync(CancellationToken ct) {
            var host = T4HostServer.Instance;
            if (_pipe is { IsConnected: true } && ReferenceEquals(_connectedHost, host)) return;
            await _connectGate.WaitAsync(ct).ConfigureAwait(false);
            try {
                host = T4HostServer.Instance;
                if (_pipe is { IsConnected: true } && ReferenceEquals(_connectedHost, host)) return;
                await host.EnsureStartedAsync(ct).ConfigureAwait(false);
                _pipe?.Dispose();
                _pipe = new NamedPipeClientStream(".", T4PipeProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await _pipe.ConnectAsync(5000, ct).ConfigureAwait(false);
                _connectedHost = host;
                _readLoop = Task.Run(() => ReadLoopAsync(_pipe, ct), ct);
            }
            finally { _connectGate.Release(); }
        }
        private async Task ReadLoopAsync(NamedPipeClientStream pipe, CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested && pipe.IsConnected) {
                    var bytes = await T4PipeProtocol.ReadFrameAsync(pipe, ct).ConfigureAwait(false);
                    var response = MessagePackSerializer.Deserialize<T4GenerationResponse>(bytes, T4WireOptions.Options);
                    if (_pending.TryRemove(response.RequestId, out var tcs)) tcs.TrySetResult(response);
                }
            }
            catch (Exception ex) { foreach (var kvp in _pending) kvp.Value.TrySetException(ex); }
        }
        public async ValueTask DisposeAsync() {
            if (_pipe is not null) _pipe.Dispose();
            if (_readLoop is not null) { try { await _readLoop.ConfigureAwait(false); } catch { } }
            _connectGate.Dispose();
        }
    }
}