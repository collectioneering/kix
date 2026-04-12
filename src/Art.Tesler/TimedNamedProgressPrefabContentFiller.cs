using System.Text;
using ConFormat;

namespace Art.Tesler;

public struct TimedNamedProgressPrefabContentFiller : IContentFiller
{
    public static TimedNamedProgressPrefabContentFiller Create(string initialName)
    {
        return new TimedNamedProgressPrefabContentFiller(initialName);
    }

    public StringContentFiller NameTextContent;
    public StringContentFiller DurationTextContent;
    public ProgressContentFiller ProgressContent;
    public StringContentFiller ProgressTextContent;

    public TimeSpan DurationCache;
    public int ProgressCache;

    public IContentFiller Content;

    public TimedNamedProgressPrefabContentFiller(string initialName)
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
        DurationCache = TimeSpan.Zero;
        ProgressCache = 0;
    }

    public void SetName(string name)
    {
        NameTextContent.Content = name;
    }

    public void SetDuration(TimeSpan duration)
    {
        if (DurationCache == duration)
        {
            return;
        }
        DurationTextContent.Content = $"{duration.TotalSeconds:F1}s";
        DurationCache = duration;
    }

    public void SetProgress(float progress)
    {
        progress = Math.Clamp(progress, 0.0f, 1.0f);
        int processedProgress = (int)(1000.0f * progress);
        if (ProgressCache == processedProgress)
        {
            return;
        }
        ProgressContent.Progress = progress;
        ProgressTextContent.Content = $"{100.0f * progress:F1}%";
        ProgressCache = processedProgress;
    }

    public void Fill(StringBuilder stringBuilder, int width, int scrollOffset = 0)
    {
        Content.Fill(stringBuilder, width, scrollOffset);
    }
}
