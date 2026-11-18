using BiblePlaylist.Shared.Audio;
using BiblePlaylist.Shared.Bible;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BiblePlaylist.Tests;

/// <summary>
/// Tests for AudioTimingProcessorSimple.SetVerseTimings.
/// The processor aligns the whole transcript word sequence against the whole
/// NET chapter word sequence (global LCS alignment) and derives each verse's
/// AudioStart/AudioEnd from the transcript words aligned to that verse.
/// </summary>
public class AudioTimingProcessorSimpleTests
{
    private static AudioTimingProcessorSimple Processor =>
        new(NullLogger<AudioTimingProcessorSimple>.Instance);

    private static BiblePlaylist.Shared.Bible.Version MakeVersion(params (int Number, string Html)[] verses)
    {
        return new BiblePlaylist.Shared.Bible.Version
        {
            Code = "NET",
            Books = new List<Book>
            {
                new Book
                {
                    Number = 45,
                    Name = "Romans",
                    Chapters = new List<Chapter>
                    {
                        new Chapter
                        {
                            Number = 5,
                            AudioUrl = "45-Romans-05.mp3",
                            Verses = verses.Select(v => new Verse { Number = v.Number, Html = v.Html }).ToList()
                        }
                    }
                }
            }
        };
    }

    private static Transcription MakeTranscription(params (string Word, decimal Start, decimal End)[] words)
    {
        return new Transcription
        {
            Words = words.Select((w, i) => new WordSegment { SegentID = 0, Word = w.Word, Start = w.Start, End = w.End }).ToList()
        };
    }

    [Fact]
    public void SetVerseTimings_RepeatedPhrase_AlignsEachVerseToItsOwnOccurrence()
    {
        // Romans 3:10-12 shape: "there is no one" repeats across the verses.
        // The old 3-word marker matcher anchored v11 onto v10's occurrence
        // (v10 end 83.80, v11 start 84.00). The alignment must give each
        // verse its own occurrence, in order.
        var version = MakeVersion(
            (10, "just as it is written: \u201cThere is no one righteous, not even one;\u201d"),
            (11, "there is no one who understands; there is no one who seeks God."),
            (12, "All have turned away; together they have become worthless; there is no one who does good."));
        var transcription = MakeTranscription(
            ("Romans", 4.08m, 4.72m), ("3", 4.72m, 5.36m),
            ("just", 82.00m, 82.30m), ("as", 82.30m, 82.50m), ("it", 82.50m, 82.60m), ("is", 82.60m, 82.80m),
            ("written", 82.80m, 83.10m),
            ("There", 83.10m, 83.50m), ("is", 83.50m, 83.80m), ("no", 83.80m, 84.10m), ("one", 84.10m, 84.50m),
            ("righteous", 84.50m, 85.00m), ("not", 85.00m, 85.30m), ("even", 85.30m, 85.60m), ("one", 85.60m, 86.00m),
            ("There", 86.94m, 87.20m), ("is", 87.20m, 87.50m), ("no", 87.50m, 87.80m), ("one", 87.80m, 88.10m),
            ("who", 88.10m, 88.40m), ("understands", 88.40m, 89.00m),
            ("there", 89.00m, 89.30m), ("is", 89.30m, 89.60m), ("no", 89.60m, 89.90m), ("one", 89.90m, 90.20m),
            ("who", 90.20m, 90.50m), ("seeks", 90.50m, 90.90m), ("God", 90.90m, 91.22m),
            ("All", 91.68m, 92.00m), ("have", 92.00m, 92.30m), ("turned", 92.30m, 92.70m), ("away", 92.70m, 93.00m),
            ("together", 93.00m, 93.40m), ("they", 93.40m, 93.70m), ("have", 93.70m, 94.00m), ("become", 94.00m, 94.40m),
            ("worthless", 94.40m, 94.90m),
            ("there", 94.90m, 95.20m), ("is", 95.20m, 95.50m), ("no", 95.50m, 95.80m), ("one", 95.80m, 96.10m),
            ("who", 96.10m, 96.40m), ("does", 96.40m, 96.70m), ("good", 96.70m, 97.00m));

        var result = Processor.SetVerseTimings(version, transcription);
        var verses = result.Books.First().Chapters.First().Verses;
        var v10 = verses[0];
        var v11 = verses[1];
        var v12 = verses[2];

        // v10 spans its own words (82.00 "just" through 86.00 "one"), not
        // cut off before "There" as the old matcher did (end 83.80).
        Assert.InRange(v10.AudioStart, 81.8m, 82.1m);
        Assert.InRange(v10.AudioEnd, 85.9m, 86.4m);
        // v11 starts at its own "There" (86.94), not mid-v10 (84.00).
        Assert.InRange(v11.AudioStart, 86.7m, 87.1m);
        Assert.InRange(v11.AudioEnd, 91.1m, 91.6m);
        Assert.InRange(v12.AudioStart, 91.5m, 91.8m);
        Assert.InRange(v12.AudioEnd, 96.9m, 97.4m);
        // Monotonic, non-overlapping.
        Assert.True(v10.AudioEnd <= v11.AudioStart);
        Assert.True(v11.AudioEnd <= v12.AudioStart);
    }

    [Fact]
    public void SetVerseTimings_FirstVerse_StartsNearChapterStart_NotShiftedLate()
    {
        // Romans 5 shape: verse 1 is the first spoken verse after the intro.
        // The old matcher put v1 at 80.78; the alignment must put it at the
        // start of the narration (~5.8 s).
        var version = MakeVersion(
            (1, "Therefore, since we have been declared righteous by faith, we have peace with God through our Lord Jesus Christ,"),
            (2, "through whom we have also obtained access into this grace in which we stand, and we rejoice in the hope of God\u2019s glory."),
            (3, "Not only this, but we also rejoice in sufferings, knowing that suffering produces endurance,"));
        var transcription = MakeTranscription(
            ("Romans", 2.46m, 3.86m), ("5", 3.86m, 4.74m),
            ("Therefore", 5.80m, 6.52m), ("since", 7.04m, 7.40m), ("we", 7.40m, 7.64m), ("have", 7.64m, 7.80m),
            ("been", 7.80m, 7.98m), ("declared", 7.98m, 8.40m), ("righteous", 8.40m, 8.90m), ("by", 8.90m, 9.10m),
            ("faith", 9.10m, 9.50m), ("we", 9.50m, 9.70m), ("have", 9.70m, 9.90m), ("peace", 9.90m, 10.30m),
            ("with", 10.30m, 10.60m), ("God", 10.60m, 10.90m), ("through", 10.90m, 11.30m), ("our", 11.30m, 11.60m),
            ("Lord", 11.60m, 12.00m), ("Jesus", 12.00m, 12.40m), ("Christ", 12.40m, 13.98m),
            ("through", 14.32m, 14.70m), ("whom", 14.70m, 15.00m), ("we", 15.00m, 15.30m), ("have", 15.30m, 15.60m),
            ("also", 15.60m, 15.90m), ("obtained", 15.90m, 16.40m), ("access", 16.40m, 16.90m), ("into", 16.90m, 17.20m),
            ("this", 17.20m, 17.50m), ("grace", 17.50m, 17.90m), ("in", 17.90m, 18.10m), ("which", 18.10m, 18.40m),
            ("we", 18.40m, 18.70m), ("stand", 18.70m, 19.10m), ("and", 19.10m, 19.40m), ("we", 19.40m, 19.70m),
            ("rejoice", 19.70m, 20.20m), ("in", 20.20m, 20.40m), ("the", 20.40m, 20.60m), ("hope", 20.60m, 21.00m),
            ("of", 21.00m, 21.20m), ("God", 21.20m, 21.60m), ("s", 21.60m, 21.80m), ("glory", 21.80m, 22.92m),
            ("Not", 23.62m, 24.00m), ("only", 24.00m, 24.30m), ("this", 24.30m, 24.60m), ("but", 24.60m, 24.90m),
            ("we", 24.90m, 25.20m), ("also", 25.20m, 25.50m), ("rejoice", 25.50m, 26.00m), ("in", 26.00m, 26.20m),
            ("sufferings", 26.20m, 26.80m), ("knowing", 26.80m, 27.20m), ("that", 27.20m, 27.50m), ("suffering", 27.50m, 28.00m),
            ("produces", 28.00m, 28.50m), ("endurance", 28.50m, 30.02m));

        var result = Processor.SetVerseTimings(version, transcription);
        var verses = result.Books.First().Chapters.First().Verses;

        // v1 starts at the narration start (~5.8), not shifted to ~80 s.
        Assert.InRange(verses[0].AudioStart, 5.5m, 6.0m);
        Assert.InRange(verses[0].AudioEnd, 13.9m, 14.4m);
        Assert.InRange(verses[1].AudioStart, 14.1m, 14.5m);
        Assert.InRange(verses[1].AudioEnd, 22.8m, 23.3m);
        Assert.InRange(verses[2].AudioStart, 23.4m, 23.8m);
        Assert.InRange(verses[2].AudioEnd, 29.9m, 30.4m);
        Assert.True(verses[0].AudioEnd <= verses[1].AudioStart);
        Assert.True(verses[1].AudioEnd <= verses[2].AudioStart);
    }

    [Fact]
    public void SetVerseTimings_VerseWithNoTranscriptWords_GetsEstimatedTimings()
    {
        // A verse whose words never appear in the transcript gets an
        // estimate (not 0, not an exception) so playback still has bounds.
        var version = MakeVersion(
            (1, "In the beginning God created the heavens and the earth."),
            (2, "Now the earth was without shape and empty, and darkness was over the surface of the watery deep, but the Spirit of God was moving over the surface of the water."),
            (3, "God said, \u201cLet there be light.\u201d And there was light!"));
        var transcription = MakeTranscription(
            ("Genesis", 1.00m, 1.50m), ("1", 1.50m, 2.00m),
            ("In", 2.00m, 2.30m), ("the", 2.30m, 2.50m), ("beginning", 2.50m, 3.00m), ("God", 3.00m, 3.30m),
            ("created", 3.30m, 3.70m), ("the", 3.70m, 3.90m), ("heavens", 3.90m, 4.30m), ("and", 4.30m, 4.50m),
            ("the", 4.50m, 4.70m), ("earth", 4.70m, 5.10m),
            // Verse 2 words are absent from the transcript entirely.
            ("God", 20.00m, 20.30m), ("said", 20.30m, 20.60m), ("Let", 20.60m, 20.90m), ("there", 20.90m, 21.20m),
            ("be", 21.20m, 21.40m), ("light", 21.40m, 21.80m), ("And", 21.80m, 22.10m), ("there", 22.10m, 22.40m),
            ("was", 22.40m, 22.70m), ("light", 22.70m, 23.10m));

        var result = Processor.SetVerseTimings(version, transcription);
        var verses = result.Books.First().Chapters.First().Verses;

        Assert.InRange(verses[0].AudioStart, 1.8m, 2.1m);
        Assert.InRange(verses[0].AudioEnd, 5.0m, 5.5m);
        // Verse 2 has no transcript words: estimated from v1's end onward.
        Assert.True(verses[1].AudioStart >= verses[0].AudioEnd);
        Assert.True(verses[1].AudioEnd > verses[1].AudioStart);
        // Verse 3 is aligned to its words.
        Assert.InRange(verses[2].AudioStart, 19.8m, 20.2m);
        Assert.InRange(verses[2].AudioEnd, 23.0m, 23.5m);
    }

    [Fact]
    public void SetVerseTimings_EmptyVersesOrTranscription_Throws()
    {
        var emptyVersion = MakeVersion();
        var transcription = MakeTranscription(("word", 1.00m, 1.50m));
        Assert.Throws<System.ArgumentException>(() => Processor.SetVerseTimings(emptyVersion, transcription));

        var version = MakeVersion((1, "In the beginning."));
        var emptyTranscription = new Transcription { Words = new List<WordSegment>() };
        Assert.Throws<System.ArgumentException>(() => Processor.SetVerseTimings(version, emptyTranscription));
    }
}
