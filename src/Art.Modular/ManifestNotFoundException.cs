namespace Art.Modular;

public class ManifestNotFoundException : ArtUserException
{
    public string AssemblyName { get; }

    public ManifestNotFoundException(string assemblyName)
    {
        AssemblyName = assemblyName;
    }

    public override string Message => $"No applicable manifest for the assembly {AssemblyName} could be found.";
}
