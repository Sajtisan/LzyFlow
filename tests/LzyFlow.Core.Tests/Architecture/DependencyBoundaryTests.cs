using System.Reflection;

namespace LzyFlow.Core.Tests.Architecture;

public sealed class DependencyBoundaryTests
{
    [Theory]
    [InlineData("LzyFlow.Application")]
    [InlineData("LzyFlow.Infrastructure")]
    [InlineData("LzyFlow.Daemon")]
    [InlineData("LzyFlow.Cli")]
    public void Core_DoesNotReferenceOtherLzyFlowLayers(string forbiddenAssembly)
    {
        var coreAssembly = Assembly.Load("LzyFlow.Core");

        var referencedAssemblies = coreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name);

        Assert.DoesNotContain(forbiddenAssembly, referencedAssemblies);
    }
}