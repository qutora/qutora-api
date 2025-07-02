namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Wraps a Stream class and performs a specific action when disposed.
/// This class ensures proper cleanup of streams returned by MinioProvider or other providers.
/// </summary>
public class DisposableStreamWrapper : Stream
{
    private readonly Stream _innerStream;
    private readonly Action? _onDispose;
    private bool _disposed = false;

    /// <summary>
    /// Creates a DisposableStreamWrapper instance.
    /// </summary>
    /// <param name="innerStream">The wrapped stream</param>
    /// <param name="onDispose">Action to be called when stream is disposed (optional)</param>
    public DisposableStreamWrapper(Stream innerStream, Action? onDispose = null)
    {
        _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
        _onDispose = onDispose;
    }

    #region Stream Properties

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    #endregion

    #region Stream Methods

    public override void Flush()
    {
        _innerStream.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _innerStream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _innerStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _innerStream.Write(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.ReadAsync(buffer, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _innerStream.WriteAsync(buffer, cancellationToken);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        _innerStream.CopyTo(destination, bufferSize);
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
    }

    #endregion

    #region Dispose

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _onDispose?.Invoke();
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _innerStream.DisposeAsync();
            _onDispose?.Invoke();
            _disposed = true;
        }

        await base.DisposeAsync();
    }

    #endregion
}
