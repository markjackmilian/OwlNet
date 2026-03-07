namespace OwlNet.Application.Common.Interfaces;

/// <summary>
/// Abstraction over filesystem operations to enable testability.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The absolute path to check.</param>
    /// <returns><see langword="true"/> if the directory exists; otherwise <see langword="false"/>.</returns>
    bool DirectoryExists(string path);
}
