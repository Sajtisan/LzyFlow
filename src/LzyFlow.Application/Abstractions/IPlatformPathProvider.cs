namespace LzyFlow.Application.Abstractions;

/// <summary>
/// Resolves the user-scoped directories used by LzyFlow on the current platform.
/// </summary>
public interface IPlatformPathProvider
{
    /// <summary>
    /// Gets the directory that contains LzyFlow configuration files.
    /// </summary>
    /// <returns>The absolute configuration directory path.</returns>
    string GetConfigDirectory();

    /// <summary>
    /// Gets the directory that contains LzyFlow persistent application data.
    /// </summary>
    /// <returns>The absolute data directory path.</returns>
    string GetDataDirectory();

    /// <summary>
    /// Gets the directory that contains LzyFlow non-essential cached data.
    /// </summary>
    /// <returns>The absolute cache directory path.</returns>
    string GetCacheDirectory();

    /// <summary>
    /// Gets the user's Downloads directory.
    /// </summary>
    /// <returns>The absolute Downloads directory path.</returns>
    string GetDownloadsDirectory();
}
