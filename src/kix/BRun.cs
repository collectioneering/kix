using CommandLine;

namespace Kix;

public class BRun : IRunOutput
{
    [Option('v', "verbose", HelpText = "Enable verbose output.")]
    public bool Verbose { get; set; }

    //[Option("ignore-shared-api-version", HelpText = "Ignore Art library version validation.")]
    public bool IgnoreSharedAssemblyVersion { get; set; }
}
