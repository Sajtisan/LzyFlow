using LzyFlow.IntegrationTests.Fixtures;

namespace LzyFlow.IntegrationTests;

public sealed class TemporaryDirectoryTests
{
    [Fact]
    public void Constructor_CreatesUniqueDirectory()
    {
        using var first = new TemporaryDirectory();
        using var second = new TemporaryDirectory();

        Assert.True(Directory.Exists(first.Path));
        Assert.True(Directory.Exists(second.Path));
        Assert.NotEqual(first.Path, second.Path);
    }

    [Fact]
    public void CreateFile_CreatesFileInsideTemporaryDirectory()
    {
        using var directory = new TemporaryDirectory();

        var filePath = directory.CreateFile(
            "downloads/example.txt",
            "hello");

        Assert.True(File.Exists(filePath));
        Assert.Equal("hello", File.ReadAllText(filePath));
    }

    [Fact]
    public void GetPath_RejectsTraversalOutsideTemporaryDirectory()
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<ArgumentException>(
            () => directory.GetPath("../outside.txt"));
    }

    [Fact]
    public void Dispose_RemovesTemporaryDirectory()
    {
        var directory = new TemporaryDirectory();
        var path = directory.Path;

        directory.CreateFile("example.txt");
        directory.Dispose();

        Assert.False(Directory.Exists(path));
    }
}