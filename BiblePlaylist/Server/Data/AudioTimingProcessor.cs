using BiblePlaylist.Server.Data;
using BiblePlaylist.Shared.Audio;
using BiblePlaylist;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class AudioTimingProcessor : IAudioTimingProcessor
{
    private readonly ILogger<AudioTimingProcessor> _logger;

    public AudioTimingProcessor(ILogger<AudioTimingProcessor> logger)
    {
        _logger = logger;
    }

    // Clean a single word by removing punctuation and converting to lowercase
    public BiblePlaylist.Shared.Bible.Version SetVerseTimings(BiblePlaylist.Shared.Bible.Version partialVersion, Transcription transcription)
    {
        decimal startBuffer = 0.1m; // Buffer before audio start
        var book = partialVersion.Books.FirstOrDefault();
        var chapter = book?.Chapters.FirstOrDefault();
        var logName = $"{book?.Name} {chapter?.Number.ToString()}";
        var verses = chapter?.Verses;

        if (verses == null || !verses.Any() || transcription?.Words == null || !transcription.Words.Any())
        {
            throw new ArgumentException("Invalid input: verses or transcription words cannot be null or empty.");
        }

        var words = transcription.Words;
        string[] transWords = words.Select(w => CleanWord(w.Word)).ToArray();

        // Start after prelude (e.g., "Genesis chapter 1")
        int transIdx = words.FindIndex(w => IsNumber(w.Word));
        if (transIdx == -1)
            transIdx = 0;
        else
            transIdx++;

        // Process each verse
        for (int i = 0; i < verses.Count; i++)
        {
            var verse = verses[i];
            string[] verseWords = CleanAndSplitText(verse.Html);

            // Find approximate start index based on AudioStart
            int approxStartIdx = FindClosestWordIndex(words, verse.AudioStart);

            // Define search window
            int windowStart = Math.Max(0, approxStartIdx - 20);
            int windowEnd = Math.Min(transWords.Length, approxStartIdx + 50);

            // Find best match in window
            var (bestStartIdx, bestEndIdx, matchRatio) = FindBestMatchSubsequence(transWords, windowStart, windowEnd, verseWords);

            if (matchRatio >= 0.5) // At least 50% match
            {
                verse.AudioStart = words[bestStartIdx].Start - startBuffer;
                verse.AudioEnd = words[bestEndIdx].End;
            }
            else
            {
                _logger.LogWarning($"Verse {verse.Number} in {logName} has no valid match; retaining approximate timings.");
            }
        }

        // Adjust AudioEnd based on next verse's AudioStart
        for (int i = 0; i < verses.Count - 1; i++)
        {
            if (verses[i].AudioEnd > verses[i + 1].AudioStart)
            {
                verses[i].AudioEnd = verses[i + 1].AudioStart;
            }
        }
        verses.Last().AudioEnd = words.Last().End;

        chapter.Verses = verses;
        return partialVersion;
    }

    // Find word index closest to target time
    private int FindClosestWordIndex(List<WordSegment> words, decimal targetTime)
    {
        int left = 0;
        int right = words.Count - 1;
        while (left < right)
        {
            int mid = left + (right - left) / 2;
            if (words[mid].Start < targetTime)
                left = mid + 1;
            else
                right = mid;
        }
        return left;
    }

    // Find best matching subsequence in window
    private (int, int, double) FindBestMatchSubsequence(string[] transWords, int windowStart, int windowEnd, string[] verseWords)
    {
        int bestStart = -1;
        int bestEnd = -1;
        int maxMatches = -1;

        for (int i = windowStart; i < windowEnd; i++)
        {
            for (int j = i; j < windowEnd; j++)
            {
                int matches = 0;
                for (int k = 0; k < verseWords.Length && i + k < transWords.Length; k++)
                {
                    if (i + k <= j && transWords[i + k] == verseWords[k])
                    {
                        matches++;
                    }
                }
                if (matches > maxMatches)
                {
                    maxMatches = matches;
                    bestStart = i;
                    bestEnd = j;
                }
            }
        }

        double matchRatio = verseWords.Length > 0 ? (double)maxMatches / verseWords.Length : 0;
        return (bestStart, bestEnd, matchRatio);
    }

    // Clean word by removing non-alphanumeric characters
    private string CleanWord(string word)
    {
        return Regex.Replace(word, @"[^a-zA-Z0-9]", " ").ToLower();
    }

    // Clean and split verse text
    private string[] CleanAndSplitText(string text)
    {
        string noHtml = Regex.Replace(text, @"<[^>]+>", string.Empty);
        string[] words = Regex.Split(noHtml, @"\s+").Where(w => !string.IsNullOrEmpty(w)).ToArray();
        return words.Select(w => CleanWord(w)).ToArray();
    }

    // Check if word is a number
    private bool IsNumber(string word)
    {
        return Regex.IsMatch(word, @"^\d+$");
    }
}