using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Art.Modular;
using Art.Tesler;

namespace kix;

internal static class Program
{
    [RequiresUnreferencedCode("Loading artifact tools might require types that cannot be statically analyzed.")]
    private static async Task<int> Main(string[] args)
    {
        return await TeslerRootCommand<PluginStore>.Create(new PluginStore()).InvokeAsync(args);
    }
}
