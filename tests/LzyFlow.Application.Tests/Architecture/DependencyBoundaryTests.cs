using LzyFlow.Application.Abstractions;

namespace LzyFlow.Application.Tests.Architecture;

public sealed class DependencyBoundaryTests
{
    [Theory]
    [InlineData("LzyFlow.Infrastructure")]
    [InlineData("LzyFlow.Daemon")]
    [InlineData("LzyFlow.Cli")]
    public void Application_DoesNotReferenceOuterLayers(string forbiddenAssembly)
    {
        var applicationAssembly = typeof(IPlatformPathProvider).Assembly;

        var referencedAssemblies = applicationAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
    }
}