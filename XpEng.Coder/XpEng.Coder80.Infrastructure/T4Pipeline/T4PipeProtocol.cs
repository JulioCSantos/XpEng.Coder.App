using System.Buffers.Binary;

namespace XpEng.Coder80.Infrastructure.T4Pipeline; 
public static class T4PipeProtocol {
    public const string PipeName = "XpEngCoder.T4Host";
    public static async Task WriteFrameAsync(Stream stream, ReadOnlyMemory<byte> payload, CancellationToken ct) {
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, ct).ConfigureAwait(false);
        await stream.WriteAsync(payload, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }
    public static async Task<byte[]> ReadFrameAsync(Stream stream, CancellationToken ct) {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, ct).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length < 0 || length > 64 * 1024 * 1024) throw new InvalidDataException($"Invalid frame length {length}");
        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, ct).ConfigureAwait(false);
        return payload;
    }
}