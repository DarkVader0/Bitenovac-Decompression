using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace Bitenovac.RemoteBuildTool.Integration.Tests;

/// <summary>
/// Provides a module initializer that registers the installed SDK's MSBuild before any
/// <c>Microsoft.Build.*</c> type is loaded.
/// </summary>
internal static class MsBuildRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
    }
}
