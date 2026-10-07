namespace LzyFlow.Infrastructure.Linux;

internal sealed class SystemLinuxPathEnvironment : ILinuxPathEnvironment
{
    public string? GetEnvironmentVariable(string variable) =>
        Environment.GetEnvironmentVariable(variable);

    public string? GetUserProfileDirectory() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> ReadLines(string path) => File.ReadLines(path);
}
