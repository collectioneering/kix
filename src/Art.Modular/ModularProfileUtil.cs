﻿using Art.Common;

namespace Art.Modular;

public static class ModularProfileUtil
{
    public static ArtifactToolProfile[] DeserializeProfiles(Stream utf8Stream)
    {
        return ArtifactToolProfileUtil.DeserializeProfiles(utf8Stream, JsonOpt.Options);
    }

    public static ArtifactToolProfile[] DeserializeProfilesFromFile(string path)
    {
        return ArtifactToolProfileUtil.DeserializeProfilesFromFile(path, JsonOpt.Options);
    }
}