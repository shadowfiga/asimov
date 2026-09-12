using System.Collections.Concurrent;

namespace Graphite.Engine.Persistence;

/// <summary>File transactions use a per-directory process gate and an OS file lock.</summary>
public class AtomicFileStorage(string directory)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    public const int MaximumFileBytes = 64 * 1024 * 1024;

    public string PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name.Contains('\\') || name is "." or "..")
        {
            throw new ArgumentException("A storage name must be a filename, not a path.", nameof(name));
        }

        return Path.Combine(DirectoryPath, name);
    }

    internal async Task<IDisposable> LockAsync(string name, CancellationToken cancellationToken)
    {
        var path = PathFor(name);
        var gate = Gates.GetOrAdd(DirectoryPath, _ => new SemaphoreSlim(1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var file = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return new Lease(file, gate);
                }
                catch (IOException) when (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(25, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            gate.Release();
            throw;
        }
    }

    internal async Task<byte[]?> ReadAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(PathFor(name), FileMode.Open, FileAccess.Read, FileShare.Read,
                8192, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length > MaximumFileBytes)
            {
                throw new InvalidDataException("The persistence file exceeds the size limit.");
            }

            var bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            return bytes;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    internal async Task WriteAsync(string name, byte[] bytes, bool backupCurrent, CancellationToken cancellationToken)
    {
        if (bytes.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("The persistence file exceeds the size limit.");
        }

        var path = PathFor(name);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var backupTemporary = temporary + ".bak";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (backupCurrent)
            {
                File.Copy(path, backupTemporary);
                using (var backup = new FileStream(backupTemporary, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    backup.Flush(flushToDisk: true);
                }

                File.Move(backupTemporary, path + ".bak", overwrite: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Commit(temporary, path);
        }
        finally
        {
            File.Delete(temporary);
            File.Delete(backupTemporary);
        }
    }

    /// <summary>Replace on the same filesystem. Override in storage tests to simulate an interrupted commit.</summary>
    protected virtual void Commit(string temporaryPath, string destinationPath)
        => File.Move(temporaryPath, destinationPath, overwrite: true);

    internal void Delete(string name) => File.Delete(PathFor(name));

    private sealed class Lease(FileStream stream, SemaphoreSlim gate) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try { stream.Dispose(); }
            finally { gate.Release(); }
        }
    }
}
