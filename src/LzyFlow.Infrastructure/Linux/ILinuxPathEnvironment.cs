namespace LzyFlow.Infrastructure.Linux;

internal interface ILinuxPathEnvironment
{
    string? GetEnvironmentVariable(string variable);

    string? GetUserProfileDirectory();

    bool FileExists(string path);

    IEnumerable<string> ReadLines(string path);
}
