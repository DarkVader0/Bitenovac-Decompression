using Microsoft.Build.Locator;

if (!MSBuildLocator.IsRegistered)
    MSBuildLocator.RegisterDefaults();

return Bitenovac.RemoteBuildTool.CommandLine.Run(args, Bitenovac.RemoteBuildTool.PipelineOutput.Console);
