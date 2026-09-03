// Entry point for the Bitenovac CI tool. MSBuildLocator must run before anything in this process
// touches a Microsoft.Build.* type — including, transitively, any command class below — so it is
// the very first statement, ahead of every other using or call.

using Microsoft.Build.Locator;

if (!MSBuildLocator.IsRegistered)
    MSBuildLocator.RegisterDefaults();

return Bitenovac.Ci.CommandLine.Run(args);
