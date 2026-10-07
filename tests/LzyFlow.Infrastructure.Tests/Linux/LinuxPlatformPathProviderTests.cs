using LzyFlow.Infrastructure.Linux;

namespace LzyFlow.Infrastructure.Tests.Linux;

public sealed class LinuxPlatformPathProviderTests
{
    [Theory]
    [InlineData("XDG_CONFIG_HOME", "/xdg/config", "/xdg/config/lzyflow")]
    [InlineData("XDG_DATA_HOME", "/xdg/data", "/xdg/data/lzyflow")]
    [InlineData("XDG_CACHE_HOME", "/xdg/cache", "/xdg/cache/lzyflow")]
    public void XdgDirectory_WhenOverrideIsAbsolute_UsesOverride(
        string variable,
        string configuredPath,
        string expectedPath)
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.SetVariable(variable, configuredPath);
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = ResolveXdgDirectory(provider, variable);

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }

    [Theory]
    [InlineData("XDG_CONFIG_HOME", null, "/home/test/.config/lzyflow")]
    [InlineData("XDG_CONFIG_HOME", "relative/config", "/home/test/.config/lzyflow")]
    [InlineData("XDG_DATA_HOME", null, "/home/test/.local/share/lzyflow")]
    [InlineData("XDG_DATA_HOME", "relative/data", "/home/test/.local/share/lzyflow")]
    [InlineData("XDG_CACHE_HOME", null, "/home/test/.cache/lzyflow")]
    [InlineData("XDG_CACHE_HOME", "relative/cache", "/home/test/.cache/lzyflow")]
    public void XdgDirectory_WhenOverrideIsMissingOrRelative_UsesHomeFallback(
        string variable,
        string? configuredPath,
        string expectedPath)
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.SetVariable(variable, configuredPath);
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = ResolveXdgDirectory(provider, variable);

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenUserDirsUsesHome_ExpandsHome()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.AddFile(
            "/home/test/.config/user-dirs.dirs",
            "XDG_DOCUMENTS_DIR=\"$HOME/Documents\"",
            "XDG_DOWNLOAD_DIR=\"$HOME/My Downloads\"");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/My Downloads", actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenUserDirsUsesBracedHome_ExpandsHome()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.AddFile(
            "/home/test/.config/user-dirs.dirs",
            "XDG_DOWNLOAD_DIR=\"${HOME}/Downloads from XDG\"");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/Downloads from XDG", actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenUserDirsUsesAbsolutePath_ReturnsAbsolutePath()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.AddFile(
            "/home/test/.config/user-dirs.dirs",
            "XDG_DOWNLOAD_DIR=\"/mnt/downloads\"");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/mnt/downloads", actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenConfigHomeIsAbsolute_ReadsUserDirsFromOverride()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.SetVariable("XDG_CONFIG_HOME", "/xdg/config");
        environment.AddFile(
            "/xdg/config/user-dirs.dirs",
            "XDG_DOWNLOAD_DIR=\"$HOME/Incoming\"");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/Incoming", actualPath);
    }

    [Theory]
    [InlineData("XDG_DOWNLOAD_DIR=\"Downloads\"")]
    [InlineData("XDG_DOWNLOAD_DIR=\"$OTHER/Downloads\"")]
    [InlineData("XDG_DOWNLOAD_DIR=$HOME/Downloads")]
    [InlineData("# XDG_DOWNLOAD_DIR=\"$HOME/Downloads\"")]
    public void GetDownloadsDirectory_WhenEntryIsInvalid_UsesConventionalFallback(string entry)
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment();
        environment.AddFile("/home/test/.config/user-dirs.dirs", entry);
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/Downloads", actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenUserDirsFileIsMissing_UsesConventionalFallback()
    {
        // Arrange
        var provider = new LinuxPlatformPathProvider(new FakeLinuxPathEnvironment());

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/Downloads", actualPath);
    }

    [Fact]
    public void GetDownloadsDirectory_WhenUserDirsFileCannotBeRead_UsesConventionalFallback()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment
        {
            ReadLinesException = new IOException("Test read failure."),
        };
        environment.AddFile(
            "/home/test/.config/user-dirs.dirs",
            "XDG_DOWNLOAD_DIR=\"$HOME/Incoming\"");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var actualPath = provider.GetDownloadsDirectory();

        // Assert
        Assert.Equal("/home/test/Downloads", actualPath);
    }

    [Fact]
    public void XdgDirectory_WhenHomeIsUnavailable_ThrowsDescriptiveException()
    {
        // Arrange
        var environment = new FakeLinuxPathEnvironment
        {
            UserProfileDirectory = null,
        };
        environment.SetVariable("HOME", "relative/home");
        var provider = new LinuxPlatformPathProvider(environment);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(provider.GetConfigDirectory);

        // Assert
        Assert.Contains("absolute home directory", exception.Message, StringComparison.Ordinal);
    }

    private static string ResolveXdgDirectory(
        LinuxPlatformPathProvider provider,
        string variable) => variable switch
        {
            "XDG_CONFIG_HOME" => provider.GetConfigDirectory(),
            "XDG_DATA_HOME" => provider.GetDataDirectory(),
            "XDG_CACHE_HOME" => provider.GetCacheDirectory(),
            _ => throw new ArgumentOutOfRangeException(nameof(variable), variable, null),
        };

    private sealed class FakeLinuxPathEnvironment : ILinuxPathEnvironment
    {
        private readonly Dictionary<string, string?> _variables = new(StringComparer.Ordinal)
        {
            ["HOME"] = "/home/test",
        };

        private readonly Dictionary<string, IReadOnlyList<string>> _files =
            new(StringComparer.Ordinal);

        public string? UserProfileDirectory { get; init; } = "/profile/test";

        public Exception? ReadLinesException { get; init; }

        public string? GetEnvironmentVariable(string variable) =>
            _variables.GetValueOrDefault(variable);

        public string? GetUserProfileDirectory() => UserProfileDirectory;

        public bool FileExists(string path) => _files.ContainsKey(path);

        public IEnumerable<string> ReadLines(string path)
        {
            if (ReadLinesException is not null)
            {
                throw ReadLinesException;
            }

            return _files[path];
        }

        public void SetVariable(string variable, string? value) => _variables[variable] = value;

        public void AddFile(string path, params string[] lines) => _files[path] = lines;
    }
}
