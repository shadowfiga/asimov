namespace Graphite.Engine.Persistence;

internal sealed class AtomicFileStorage(string directory)
{
    public string DirectoryPath { get; } = Path.GetFullPath(directory);
    private const int MaximumFileBytes = 64 * 1024 * 1024;

    public string PathFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name.Contains('\\') || name is "." or "..")
        {
            throw new ArgumentException("A storage name must be a filename, not a path.", nameof(name));
        }

        return Path.Combine(DirectoryPath, name);
    }

    public byte[]? Read(string name)
    {
        try
        {
            using var stream = File.OpenRead(PathFor(name));
            if (stream.Length > MaximumFileBytes)
            {
                throw new InvalidDataException("The persistence file exceeds the size limit.");
            }

            var bytes = new byte[(int)stream.Length];
            stream.ReadExactly(bytes);
            return bytes;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    public void Write(string name, byte[] bytes, bool backupCurrent)
    {
        if (bytes.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("The persistence file exceeds the size limit.");
        }

        Directory.CreateDirectory(DirectoryPath);
        var path = PathFor(name);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var backupTemporary = temporary + ".bak";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            if (backupCurrent)
            {
                File.Copy(path, backupTemporary);
                using (var backup = File.OpenWrite(backupTemporary))
                {
                    backup.Flush(flushToDisk: true);
                }

                File.Move(backupTemporary, path + ".bak", overwrite: true);
            }

            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
            File.Delete(backupTemporary);
        }
    }

    public void Delete(string name) => File.Delete(PathFor(name));
}
