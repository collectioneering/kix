using Art.Common;

namespace Art.Tesler.Profiles;

public class FileResolvedProfiles : IWritableResolvedProfiles
{
    public IReadOnlyList<ArtifactToolProfile> Values { get; }
    public string FilePath { get; }
    public bool SingleObject { get; }

    public FileResolvedProfiles(IReadOnlyList<ArtifactToolProfile> values, string filePath, bool singleObject)
    {
        Values = values;
        FilePath = filePath;
        SingleObject = singleObject;
    }

    public void WriteProfiles(IReadOnlyList<ArtifactToolProfile> artifactToolProfiles)
    {
        if (artifactToolProfiles.Count == 1 && SingleObject)
        {
            ArtifactToolProfileUtil.SerializeProfileToFile(FilePath, artifactToolProfiles[0]);
        }
        else
        {
            ArtifactToolProfileUtil.SerializeProfilesToFile(FilePath, artifactToolProfiles);
        }
    }
}
