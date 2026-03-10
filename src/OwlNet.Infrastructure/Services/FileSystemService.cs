using OwlNet.Application.Common.Interfaces;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IFileSystem"/> that delegates to
/// <see cref="Directory"/> and <see cref="File"/> for filesystem operations.
/// </summary>
public sealed class FileSystemService : IFileSystem
{
    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public string[] GetFiles(string path, string searchPattern) =>
        Directory.GetFiles(path, searchPattern);

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default) =>
        File.WriteAllTextAsync(path, content, cancellationToken);

    /// <inheritdoc />
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    /// <inheritdoc />
    public void DeleteFile(string path) => File.Delete(path);
}
