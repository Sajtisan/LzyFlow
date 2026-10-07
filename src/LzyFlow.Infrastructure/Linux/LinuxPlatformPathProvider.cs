using LzyFlow.Application.Abstractions;

namespace LzyFlow.Infrastructure.Linux;

/// <summary>
/// Resolves LzyFlow paths according to the XDG Base Directory and user-directory conventions.
/// </summary>
public sealed class LinuxPlatformPathProvider : IPlatformPathProvider
{
    private const string ApplicationDirectoryName = "lzyflow";
    private const string DownloadDirectoryKey = "XDG_DOWNLOAD_DIR=";

    private readonly ILinuxPathEnvironment _environment;

    /// <summary>
    /// Initializes a provider that reads the current process environment and user-directory file.
    /// </summary>
    public LinuxPlatformPathProvider()
        : this(new SystemLinuxPathEnvironment())
    {
    }

    internal LinuxPlatformPathProvider(ILinuxPathEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _environment = environment;
    }

    /// <inheritdoc />
    public string GetConfigDirectory() =>
        AppendApplicationDirectory(GetXdgBaseDirectory("XDG_CONFIG_HOME", ".config"));

    /// <inheritdoc />
    public string GetDataDirectory() =>
        AppendApplicationDirectory(GetXdgBaseDirectory("XDG_DATA_HOME", ".local/share"));

    /// <inheritdoc />
    public string GetCacheDirectory() =>
        AppendApplicationDirectory(GetXdgBaseDirectory("XDG_CACHE_HOME", ".cache"));

    /// <inheritdoc />
    public string GetDownloadsDirectory()
    {
        var homeDirectory = GetHomeDirectory();
        var userDirectoriesPath = Path.Combine(
            GetXdgBaseDirectory("XDG_CONFIG_HOME", ".config"),
            "user-dirs.dirs");

        try
        {
            if (_environment.FileExists(userDirectoriesPath))
            {
                foreach (var line in _environment.ReadLines(userDirectoriesPath))
                {
                    if (TryResolveDownloadDirectory(line, homeDirectory, out var downloadDirectory))
                    {
                        return downloadDirectory;
                    }
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A missing or unreadable optional user-dirs file uses the conventional fallback.
        }

        return Path.Combine(homeDirectory, "Downloads");
    }

    private string GetXdgBaseDirectory(string variable, string fallbackRelativePath)
    {
        var configuredPath = _environment.GetEnvironmentVariable(variable);

        if (TryNormalizeAbsolutePath(configuredPath, out var absolutePath))
        {
            return absolutePath;
        }

        return Path.Combine(GetHomeDirectory(), fallbackRelativePath);
    }

    private string GetHomeDirectory()
    {
        if (TryNormalizeAbsolutePath(
                _environment.GetEnvironmentVariable("HOME"),
                out var homeDirectory))
        {
            return homeDirectory;
        }

        if (TryNormalizeAbsolutePath(
                _environment.GetUserProfileDirectory(),
                out var userProfileDirectory))
        {
            return userProfileDirectory;
        }

        throw new InvalidOperationException(
            "Unable to resolve an absolute home directory from HOME or the user profile.");
    }

    private static bool TryResolveDownloadDirectory(
        string line,
        string homeDirectory,
        out string downloadDirectory)
    {
        downloadDirectory = string.Empty;
        var trimmedLine = line.Trim();

        if (!trimmedLine.StartsWith(DownloadDirectoryKey, StringComparison.Ordinal))
        {
            return false;
        }

        var configuredPath = trimmedLine[DownloadDirectoryKey.Length..].Trim();

        if (configuredPath.Length < 2 || configuredPath[0] != '"' || configuredPath[^1] != '"')
        {
            return false;
        }

        configuredPath = configuredPath[1..^1];

        if (configuredPath.Equals("$HOME", StringComparison.Ordinal) ||
            configuredPath.Equals("${HOME}", StringComparison.Ordinal))
        {
            downloadDirectory = homeDirectory;
            return true;
        }

        const string homePrefix = "$HOME/";
        const string bracedHomePrefix = "${HOME}/";

        if (configuredPath.StartsWith(homePrefix, StringComparison.Ordinal))
        {
            configuredPath = Path.Combine(homeDirectory, configuredPath[homePrefix.Length..]);
        }
        else if (configuredPath.StartsWith(bracedHomePrefix, StringComparison.Ordinal))
        {
            configuredPath = Path.Combine(homeDirectory, configuredPath[bracedHomePrefix.Length..]);
        }

        return TryNormalizeAbsolutePath(configuredPath, out downloadDirectory);
    }

    private static bool TryNormalizeAbsolutePath(string? path, out string absolutePath)
    {
        absolutePath = string.Empty;

        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
        {
            return false;
        }

        try
        {
            absolutePath = Path.GetFullPath(path);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private static string AppendApplicationDirectory(string baseDirectory) =>
        Path.Combine(baseDirectory, ApplicationDirectoryName);
}
