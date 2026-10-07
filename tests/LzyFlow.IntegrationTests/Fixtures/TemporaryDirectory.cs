namespace LzyFlow.IntegrationTests.Fixtures;

public sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; }

    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "lzyflow-tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    public string GetPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (System.IO.Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException(
                "The path must be relative to the temporary directory.",
                nameof(relativePath));
        }

        var root = System.IO.Path.GetFullPath(Path);
        var candidate = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(root, relativePath));

        var rootWithSeparator = root.EndsWith(System.IO.Path.DirectorySeparatorChar)
            ? root
            : root + System.IO.Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The path must remain inside the temporary directory.",
                nameof(relativePath));
        }

        return candidate;
    }

    public string CreateFile(string relativePath, string content = "")
    {
        var filePath = GetPath(relativePath);
        var parentDirectory = System.IO.Path.GetDirectoryName(filePath);

        if (parentDirectory is not null)
        {
            Directory.CreateDirectory(parentDirectory);
        }

        File.WriteAllText(filePath, content);

        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}