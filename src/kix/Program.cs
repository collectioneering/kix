using System.CommandLine;
using Art.Modular;
using Art.Tesler;

return await TeslerRootCommand.Create(new DynamicRegistryStore()).InvokeAsync(args);
