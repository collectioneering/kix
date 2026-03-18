using System.CommandLine;
using Art.Common.Management;

namespace Art.Tesler;

public class DiskTeslerDataProvider : ITeslerDataProvider
{
    protected Option<string> OutputOption;

    public DiskTeslerDataProvider()
    {
        OutputOption = new Option<string>("-o", "--output") { HelpName = "directory", Description = "Output directory", DefaultValueFactory = static _ => Directory.GetCurrentDirectory() };
    }

    public DiskTeslerDataProvider(Option<string> outputOption)
    {
        OutputOption = outputOption;
    }

    public void Initialize(Command command)
    {
        command.Add(OutputOption);
    }

    public IArtifactDataManager CreateArtifactDataManager(ParseResult parseResult)
    {
        return new DiskArtifactDataManager(parseResult.GetRequiredValue(OutputOption));
    }
}
