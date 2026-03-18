using System.Text;
using ConFormat;

namespace Art.Tesler;

public struct TimedDownloadPrefabContentFiller : IContentFiller
{
    public static TimedDownloadPrefabContentFiller Create(string initialName)
    {
        return new TimedDownloadPrefabContentFiller(initialName);
    }

    public StringContentFiller NameTextContent;
    public StringContentFiller DurationTextContent;
    public ProgressContentFiller ProgressContent;
    public StringContentFiller ProgressTextContent;

    public IContentFiller Content;

    public TimedDownloadPrefabContentFiller(string initialName)
    {
        NameTextContent = StringContentFiller.Create(initialName, ContentAlignment.Left);
        DurationTextContent = StringContentFiller.Create("0.0s", ContentAlignment.Right);
        ProgressContent = ProgressContentFiller.Create();
        ProgressTextContent = StringContentFiller.Create("0.0%", ContentAlignment.Right);
        Content = BorderContentFiller.Create("[", "]",
            SplitContentFiller.Create("|", 0.25f, 0.75f,
                ScrollingContentFiller.Create(TimeSpan.FromSeconds(0.4f),
                    NameTextContent),
                FixedSplitContentFiller.Create("|", 7, 0,
                    DurationTextContent,
                    FixedSplitContentFiller.Create("|", 6, 1,
                        ColorContentFiller.Create(ConsoleColor.Green, ProgressContent), ProgressTextContent))));
    }

    public void SetName(string name)
    {
        NameTextContent.Content = name;
    }

    public void SetDuration(TimeSpan duration)
    {
        DurationTextContent.Content = $"{duration.TotalSeconds:F1}s";
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
