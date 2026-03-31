using System.Collections.Frozen;
using Art.Common;

namespace Art.Tesler.Tests;

public class RefactoringsTests
{
    [Fact]
    public void Refactor_NoMatchingRules_Noop()
    {
        var refactorings = CreateRefactorings([], []);
        bool success = refactorings.TryGetRefactoredArtifactKey(CreateArtifactKey("x", "y"), out var remappedKey);
        Assert.False(success);
        Assert.Null(remappedKey);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Refactor_MatchingAssembly_AppliesRemapCorrectly(bool typeHasSameNamespace, bool updateNamespace)
    {
        const string recordedAssembly = "Assembly1";
        const string targetAssembly = "Assembly2";
        var refactorings = CreateRefactorings([], [
            new KeyValuePair<string, RefactoringsAssembly>(recordedAssembly, new RefactoringsAssembly(targetAssembly, updateNamespace))
        ]);
        string typeName = typeHasSameNamespace ? $"{recordedAssembly}.Type1" : "Type1";
        string expectedTypeName = updateNamespace
            ? typeHasSameNamespace ? $"{targetAssembly}.Type1" : "Type1"
            : typeName;
        var key = CreateArtifactKey(recordedAssembly, typeName);
        bool success = refactorings.TryGetRefactoredArtifactKey(key, out var remappedKey);
        Assert.True(success);
        Assert.NotNull(remappedKey);
        var resultToolId = ArtifactToolIDUtil.ParseID(remappedKey.Tool);
        Assert.Equal(targetAssembly, resultToolId.Assembly);
        Assert.Equal(expectedTypeName, resultToolId.Type);
        Assert.Equal(key.Group, remappedKey.Group);
        Assert.Equal(key.Id, remappedKey.Id);
    }

    [Fact]
    public void Refactor_MatchingType_AppliesRemapCorrectly()
    {
        var refactorings = CreateRefactorings([
            new KeyValuePair<string, string>(ArtifactToolIDUtil.CreateToolString("Assembly1", "Type1"), ArtifactToolIDUtil.CreateToolString("Assembly2", "Type2"))
        ], []);
        var key = CreateArtifactKey("Assembly1", "Type1");
        bool success = refactorings.TryGetRefactoredArtifactKey(key, out var remappedKey);
        Assert.True(success);
        Assert.NotNull(remappedKey);
        var resultToolId = ArtifactToolIDUtil.ParseID(remappedKey.Tool);
        Assert.Equal("Assembly2", resultToolId.Assembly);
        Assert.Equal("Type2", resultToolId.Type);
        Assert.Equal(key.Group, remappedKey.Group);
        Assert.Equal(key.Id, remappedKey.Id);
    }

    [Fact]
    public void Refactor_MatchingType_And_MatchingAssembly_AppliesRemapUsingType()
    {
        var refactorings = CreateRefactorings([
            new KeyValuePair<string, string>(ArtifactToolIDUtil.CreateToolString("Assembly1", "Type1"), ArtifactToolIDUtil.CreateToolString("Assembly2", "Type2"))
        ], [
            new KeyValuePair<string, RefactoringsAssembly>("Assembly1", new RefactoringsAssembly("Assembly3", false)),
        ]);
        var key = CreateArtifactKey("Assembly1", "Type1");
        bool success = refactorings.TryGetRefactoredArtifactKey(key, out var remappedKey);
        Assert.True(success);
        Assert.NotNull(remappedKey);
        var resultToolId = ArtifactToolIDUtil.ParseID(remappedKey.Tool);
        Assert.Equal("Assembly2", resultToolId.Assembly);
        Assert.Equal("Type2", resultToolId.Type);
        Assert.Equal(key.Group, remappedKey.Group);
        Assert.Equal(key.Id, remappedKey.Id);
    }

    private static ArtifactKey CreateArtifactKey(
        string toolAssembly,
        string toolType,
        string group = "group",
        string id = "id")
    {
        return new ArtifactKey(
            Tool: ArtifactToolIDUtil.CreateToolString(toolAssembly, toolType),
            Group: group,
            Id: id
        );
    }

    private static Refactorings CreateRefactorings(
        List<KeyValuePair<string, string>> types,
        List<KeyValuePair<string, RefactoringsAssembly>> assemblies
    )
    {
        return new Refactorings(types.ToFrozenDictionary(), assemblies.ToFrozenDictionary());
    }
}
