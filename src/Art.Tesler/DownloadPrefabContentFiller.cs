using System.Text;
using ConFormat;

namespace Art.Tesler;

public struct DownloadPrefabContentFiller : IContentFiller
{
    public static DownloadPrefabContentFiller Create(string initialName)
    {
        return new DownloadPrefabContentFiller(initialName);
    }

    public StringContentFiller NameTextContent;
    public ProgressContentFiller ProgressContent;
    public StringContentFiller ProgressTextContent;

    public IContentFiller Content;

    public DownloadPrefabContentFiller(string initialName)
    {
        NameTextContent = StringContentFiller.Create(initialName, ContentAlignment.Left);
        ProgressContent = ProgressContentFiller.Create();
        ProgressTextContent = StringContentFiller.Create("0.0%", ContentAlignment.Right);
        Content = BorderContentFiller.Create("[", "]",
            SplitContentFiller.Create("|", 0.25f, 0.75f,
                NameTextContent,
                FixedSplitContentFiller.Create("|", 6, 1,
                    ColorContentFiller.Create(ConsoleColor.Green, ProgressContent),
                    ProgressTextContent)));
    }

    public void SetName(string name)
    {
        NameTextContent.Content = name;
    }

    public void SetProgress(float progress)
    {
        progress = Math.Clamp(progress, 0.0f, 1.0f);
        ProgressContent.Progress = progress;
        ProgressTextContent.Content = $"{100.0f * progress:F1}%";
    }

    public void Fill(StringBuilder stringBuilder, int width, int scrollOffset = 0)
    {
        Content.Fill(stringBuilder, width, scrollOffset);
    }
}
