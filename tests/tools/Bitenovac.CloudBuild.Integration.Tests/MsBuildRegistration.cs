using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace Bitenovac.CloudBuild.Integration.Tests;

/// <summary>
/// Does for this test host what <c>Program.cs</c> does for the tool: points the process at the
/// installed SDK's MSBuild before anything loads a <c>Microsoft.Build.*</c> type.
/// </summary>
/// <remarks>
/// A module initializer rather than a fixture, because a fixture runs after xUnit has already
/// reflected over every test class — and reflecting over a class that mentions
/// <c>MsBuildProjectEvaluator</c> is enough to trigger the load this has to precede.
/// </remarks>
internal static class MsBuildRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
    }
}
