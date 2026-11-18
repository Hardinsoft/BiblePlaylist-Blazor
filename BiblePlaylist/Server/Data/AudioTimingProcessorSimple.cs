using BiblePlaylist.Server.Data;
using BiblePlaylist.Shared.Audio;
using BiblePlaylist;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;


public class AudioTimingProcessorSimple : IAudioTimingProcessor
{
    private readonly ILogger<AudioTimingProcessorSimple> _logger;

    public AudioTimingProcessorSimple(ILogger<AudioTimingProcessorSimple> logger)
    {
        _logger = logger;
    }

    // Clean a single word by splitting on hyphens and removing non-alphanumeric characters
    private string[] CleanWord(string word)
    {
        return Regex.Split(word, @"[\s-]").Select(w => Regex.Replace(w, @"[^a-zA-Z0-9]", "").ToLower()).Where(w => !string.IsNullOrEmpty(w)).ToArray();
    }

    // Clean and split text into words, removing HTML tags and handling hyphens
    private string[] CleanAndSplitText(string text)
    {
        string noHtml = Regex.Replace(text, @"<[^>]+>", string.Empty);
        string[] words = Regex.Split(noHtml, @"\s+").SelectMany(w => CleanWord(w)).ToArray();
        return words;
    }

    // Check if a string is a number
    private bool IsNumber(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Aligns one verse's NET words against a window of the transcript with a
    /// longest-common-subsequence (LCS) alignment, and returns the aligned
    /// pairs as (transcript token index, verse token index), in order, plus
    /// the fraction of the verse's words that matched. The caller searches
    /// forward from the previous verse's end, so a repeated phrase resolves
    /// to its next occurrence and a transcript word cannot be stolen by a
    /// later verse. A low ratio means the verse's words are not in the
    /// window (e.g. a verse missing from the transcription).
    /// </summary>
    private (List<(int TransIdx, int VerseIdx)> Pairs, double Ratio) AlignVerse(
        string[] transTokens, int windowStart, int windowEnd, string[] verseTokens)
    {
        var pairs = new List<(int, int)>();
        if (verseTokens.Length == 0 || windowStart >= windowEnd)
            return (pairs, 0.0);

        int n = windowEnd - windowStart; // transcript window length
        int m = verseTokens.Length;
        var dp = new int[n + 1, m + 1];
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (transTokens[windowStart + i - 1].Equals(verseTokens[j - 1], StringComparison.Ordinal))
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                else
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        int a = n, b = m;
        while (a > 0 && b > 0)
        {
            // Take a match only when it is forced: skipping either word
            // would shrink the LCS. Otherwise an equally good alignment
            // exists without this occurrence, so skip it to prefer the
            // earliest occurrence of a repeated word.
            if (transTokens[windowStart + a - 1].Equals(verseTokens[b - 1], StringComparison.Ordinal)
                && dp[a - 1, b] < dp[a, b] && dp[a, b - 1] < dp[a, b])
            {
                pairs.Add((windowStart + a - 1, b - 1));
                a--;
                b--;
            }
            else if (dp[a - 1, b] >= dp[a, b - 1])
            {
                a--;
            }
            else
            {
                b--;
            }
        }
        pairs.Reverse();
        double ratio = verseTokens.Length > 0 ? (double)pairs.Count / verseTokens.Length : 0.0;
        return (pairs, ratio);
    }

    public BiblePlaylist.Shared.Bible.Version SetVerseTimings(BiblePlaylist.Shared.Bible.Version partialVersion, Transcription transcription)
    {
        decimal startBuffer = 0.1m;
        decimal endBuffer = 0.3m;
        decimal avgWordDuration = 0.4m;
        var book = partialVersion.Books.FirstOrDefault();
        var chapter = book?.Chapters.FirstOrDefault();
        var logName = $"{book?.Name} {chapter?.Number.ToString()}";
        var verses = chapter?.Verses;

        if (verses == null || !verses.Any() || transcription?.Words == null || !transcription.Words.Any())
        {
            throw new ArgumentException("Invalid input: verses or transcription words cannot be null or empty.");
        }

        var words = transcription.Words;

        // Flatten the transcript into cleaned tokens, keeping the WordSegment
        // index for each token so timings can be read back off the original.
        var transTokens = new List<string>();
        var transTokenWordIdx = new List<int>();
        for (int w = 0; w < words.Count; w++)
        {
            foreach (var token in CleanWord(words[w].Word))
            {
                transTokens.Add(token);
                transTokenWordIdx.Add(w);
            }
        }
        string[] transArray = transTokens.ToArray();

        decimal totalDuration = words.Last().End;
        decimal lastEnd = 0m;
        int currentIdx = 0;
        const int maxSearch = 150;
        const double minRatio = 0.5;

        for (int i = 0; i < verses.Count; i++)
        {
            string[] verseTokens = CleanAndSplitText(verses[i].Html);

            // Search forward from the previous verse's end so a repeated
            // phrase resolves to its next occurrence.
            int windowEnd = Math.Min(currentIdx + maxSearch, transArray.Length);
            var (pairs, ratio) = AlignVerse(transArray, currentIdx, windowEnd, verseTokens);

            if (pairs.Count > 0 && ratio >= minRatio)
            {
                int firstWordIdx = transTokenWordIdx[pairs.Min(p => p.TransIdx)];
                int lastWordIdx = transTokenWordIdx[pairs.Max(p => p.TransIdx)];
                decimal audioStart = words[firstWordIdx].Start - startBuffer;
                decimal audioEnd = words[lastWordIdx].End + endBuffer;
                verses[i].AudioStart = Math.Max(audioStart, lastEnd);
                verses[i].AudioEnd = Math.Min(audioEnd, totalDuration);
                lastEnd = verses[i].AudioEnd;
                currentIdx = pairs.Max(p => p.TransIdx) + 1;
            }
            else
            {
                // No transcript words aligned to this verse: estimate from the
                // previous verse's end, as before.
                decimal estimatedStart = lastEnd;
                decimal estimatedDuration = verseTokens.Length * avgWordDuration;
                decimal estimatedEnd = estimatedStart + estimatedDuration;
                if (i + 1 < verses.Count && verses[i + 1].AudioStart > 0)
                {
                    estimatedEnd = Math.Min(estimatedEnd, verses[i + 1].AudioStart - startBuffer);
                }
                verses[i].AudioStart = estimatedStart;
                verses[i].AudioEnd = Math.Min(estimatedEnd, totalDuration);
                lastEnd = verses[i].AudioEnd;
                _logger.LogWarning($"Verse {verses[i].Number} in {logName} estimated timings: {estimatedStart} to {estimatedEnd}");
            }
        }

        var serializedVerses = JsonConvert.SerializeObject(verses, Formatting.Indented);
        chapter.Verses = verses;
        return partialVersion;
    }
}
