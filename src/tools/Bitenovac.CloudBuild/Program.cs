using Microsoft.Build.Locator;

if (!MSBuildLocator.IsRegistered)
    MSBuildLocator.RegisterDefaults();

return Bitenovac.CloudBuild.CommandLine.Run(args, Bitenovac.CloudBuild.PipelineOutput.Console);
