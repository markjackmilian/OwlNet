using OwlNet.Application.Common.Interfaces;

namespace OwlNet.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IFileSystem"/> that delegates to <see cref="Directory"/>.
/// </summary>
public sealed class FileSystemService : IFileSystem
{
    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);
}
