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

    /// <summary>
    /// Returns the file paths that match the specified search pattern in the given directory.
    /// </summary>
    /// <param name="path">The absolute path of the directory to search.</param>
    /// <param name="searchPattern">
    /// The search string to match against the names of files in <paramref name="path"/>
    /// (e.g. <c>"*.json"</c>).
    /// </param>
    /// <returns>An array of full file paths that match the pattern.</returns>
    string[] GetFiles(string path, string searchPattern);

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The absolute path to the file to check.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Asynchronously reads all text from the specified file.
    /// </summary>
    /// <param name="path">The absolute path of the file to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous read operation and contains the file contents.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously writes the specified text to a file, creating the file if it does not exist
    /// or overwriting it if it does.
    /// </summary>
    /// <param name="path">The absolute path of the file to write.</param>
    /// <param name="content">The text to write to the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the specified directory and all parent directories in its path.
    /// </summary>
    /// <param name="path">The absolute path of the directory to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The absolute path of the file to delete.</param>
    void DeleteFile(string path);
}
