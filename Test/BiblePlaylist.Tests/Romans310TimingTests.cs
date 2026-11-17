using BiblePlaylist.Shared.Bible;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace BiblePlaylist.Tests;

public class Romans310TimingTests
{
    private static (Verse Verse10, Verse Verse11) LoadRomans3Verses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "NET.json");
        var json = File.ReadAllText(path);
        var version = JsonConvert.DeserializeObject<BiblePlaylist.Shared.Bible.Version>(json)!;
        var romans = version.Books.First(b => b.Name == "Romans");
        var chapter3 = romans.Chapters.First(c => c.Number == 3);
        var verse10 = chapter3.Verses.First(v => v.Number == 10);
        var verse11 = chapter3.Verses.First(v => v.Number == 11);
        return (verse10, verse11);
    }

    private static int CountWords(string html)
    {
        var text = Regex.Replace(html, "<.*?>", " ");
        // Count only tokens containing a letter, so stray punctuation
        // (curly quotes, a trailing comma) is not counted as a word.
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(token => token.Any(char.IsLetter));
    }

    [Fact]
    public void Romans3_10_DurationCoversFullVerseText()
    {
        var (verse10, _) = LoadRomans3Verses();
        var duration = verse10.AudioEnd - verse10.AudioStart;
        var words = CountWords(verse10.Html);

        // Romans 3:10 has 13 words; at narration pace it needs ~4 s of audio.
        // The stored 1.42 s cut the verse off after "written,".
        Assert.True(duration >= 3.0m,
            $"Romans 3:10 lasts {duration}s for {words} words; the verse is cut short.");
        Assert.True(duration / words >= 0.25m,
            $"Romans 3:10 allows {duration / words:0.00}s per word; speech is unintelligible that fast.");
        // Pilot re-transcription (faster-whisper, word timestamps): the final word
        // of v10 ("one.") ends at 86.52 s, so the stored end must reach at least 86.5 s.
        Assert.True(verse10.AudioEnd >= 86.5m,
            $"Romans 3:10 ends at {verse10.AudioEnd}s, before its final word finishes at 86.52s.");
    }

    [Fact]
    public void Romans3_11_StartsAfterVerse10Ends()
    {
        var (verse10, verse11) = LoadRomans3Verses();
        Assert.True(verse11.AudioStart >= verse10.AudioEnd,
            $"Romans 3:11 starts at {verse11.AudioStart}s, before verse 10 ends at {verse10.AudioEnd}s.");
        // Pilot re-transcription: v10's last word ends at 86.52 s, so v11 must not
        // start before then or it begins mid-phrase of verse 10.
        Assert.True(verse11.AudioStart >= 86.5m,
            $"Romans 3:11 starts at {verse11.AudioStart}s, while verse 10 is still speaking.");
    }
}
