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

    // Find potential start indices based on the marker sequence
    private List<int> FindPotentialStarts(string[] transWords, int startFrom, string[] marker)
    {
        List<int> starts = new List<int>();
        int markerLen = marker.Length;
        for (int i = startFrom; i <= transWords.Length - markerLen; i++)
        {
            bool match = true;
            for (int j = 0; j < markerLen; j++)
            {
                if (!transWords[i + j].Equals(marker[j], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                starts.Add(i);
            }
        }
        return starts;
    }

    // Find the best matching segment for a verse based on word overlap ratio
    private (int startIdx, int endIdx, double ratio) FindBestMatch(string[] transWords, int startFrom, string[] verseWords, int maxSearch)
    {
        int bestStart = -1;
        int bestEnd = -1;
        double bestRatio = 0.0;
        int verseLen = verseWords.Length;
        int windowSize = Math.Max(verseLen, 10);

        for (int i = startFrom; i <= transWords.Length - verseLen && i < startFrom + maxSearch; i++)
        {
            int matches = 0;
            int windowEnd = Math.Min(i + windowSize, transWords.Length);
            for (int j = 0; j < verseLen && i + j < windowEnd; j++)
            {
                if (i + j < transWords.Length && transWords[i + j].Equals(verseWords[j], StringComparison.OrdinalIgnoreCase))
                {
                    matches++;
                }
            }
            double ratio = (double)matches / verseLen;
            if (ratio > bestRatio)
            {
                bestRatio = ratio;
                bestStart = i;
                bestEnd = Math.Min(i + verseLen - 1, transWords.Length - 1);
            }
        }
        return (bestStart, bestEnd, bestRatio);
    }

    private int ConfirmEndIndex(int startIndx, int endIdx, string[] transWords, string[] verseWords, string[] nextVerseWords)
    {
        var lastIdx = verseWords.ToList().LastIndexOf(transWords[endIdx]);
        var verseWordsLastIdx = verseWords.Length - 1;

        if (lastIdx == -1) return endIdx;

        if (verseWordsLastIdx - lastIdx > 0)
        {
            var diffInx = verseWordsLastIdx - lastIdx;
            // If we have a mismatch, we need to adjust the end index
            if (verseWords[verseWordsLastIdx].Equals(transWords[endIdx + diffInx], StringComparison.OrdinalIgnoreCase)) 
            { 
                return endIdx + diffInx; 
            }
            else
            {
                //var newEndIdx = FindSubsetEndIndex(verseWords, GetSubset(transWords, startIndx, endIdx - startIndx + 1));

                //if (newEndIdx != -1)
                //    return newEndIdx;    

                var (nextStartIdx, nextEndIdx, ratio) = FindBestMatch(transWords, endIdx - 10, nextVerseWords, nextVerseWords.Length - 1 + 20);
                if (nextStartIdx != -1 && ratio >= 0.5)
                {
                    endIdx = nextStartIdx - 1; // Adjust end index to the start of the next verse
                }
            }           
        }        

        return endIdx;
    }
    private int CountMatchingValues(string[] array1, string[] array2)
    {
        // Handle null or empty arrays
        if (array1 == null || array2 == null || array1.Length == 0 || array2.Length == 0)
            return 0;

        // Convert arrays to HashSet for efficient comparison
        var set1 = new HashSet<string>(array1, StringComparer.OrdinalIgnoreCase);
        var set2 = new HashSet<string>(array2, StringComparer.OrdinalIgnoreCase);

        // Count common elements
        return set1.Intersect(set2).Count();
    }
    public string[] GetSubset(string[] source, int startIndex, int length)
    {
        if (source == null || startIndex < 0 || length <= 0 || startIndex + length > source.Length)
            return new string[0];

        return source.Skip(startIndex).Take(length).ToArray();
    }
    private int FindSubsetEndIndex(string[] source, string[] subset)
    {
        // Input validation
        if (source == null || subset == null || source.Length < 3 || subset.Length < 3)
            return -1;

        // Get the last three elements of the source array
        var lastThreeSource = source.TakeLast(3).ToArray();
        if (lastThreeSource.Length < 3)
            return -1;

        // Iterate through possible ending positions in subset
        for (int i = subset.Length - 1; i >= 2; i--)
        {
            // Get last three elements of subset ending at index i
            var lastThreeSubset = subset.Skip(i - 2).Take(3).ToArray();

            // Check if last element matches exactly
            if (lastThreeSubset[2] != lastThreeSource[2])
                continue;

            // Create sets for first two elements to ignore order
            var sourceFirstTwo = new HashSet<string>(lastThreeSource.Take(2), StringComparer.OrdinalIgnoreCase);
            var subsetFirstTwo = new HashSet<string>(lastThreeSubset.Take(2), StringComparer.OrdinalIgnoreCase);

            // Check if first two elements match (order doesn't matter)
            if (sourceFirstTwo.Intersect(subsetFirstTwo).Count() == 2)
                return i;
        }

        return -1; // No match found
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
        string[] transWords = words.SelectMany(w => CleanWord(w.Word)).ToArray();

        int transIdx = Array.FindIndex(transWords, w => IsNumber(w));
        //if (transIdx == -1) transIdx = 3;
        //else transIdx++;

        transIdx = 3;

        int currentIdx = transIdx;
        int maxSearch = 150;
        decimal totalDuration = words.Last().End;
        decimal lastEnd = 0m;

        for (int i = 0; i < verses.Count; i++)
        {
            if(i == 16)
                Debug.WriteLine("Debugging verse 17");

            string[] verseWords = CleanAndSplitText(verses[i].Html);
            string[] marker = verseWords.Take(3).ToArray();
            List<int> potentialStarts = FindPotentialStarts(transWords, currentIdx, marker);

            int bestStart = -1;
            int bestEnd = -1;
            double bestRatio = 0.0;

            if (potentialStarts.Any())
            {
                foreach (int startIdx in potentialStarts)
                {
                    int endIdx = Math.Min(startIdx + verseWords.Length - 1, transWords.Length - 1);
                    // Confirm that endIdx is really end of the verse
                    endIdx = ConfirmEndIndex(startIdx, endIdx, transWords, verseWords, i + 1 < verses.Count ? CleanAndSplitText(verses[i + 1].Html) : new string[0]);
                    int matches = 0;
                    //for (int j = 0; j < verseWords.Length && startIdx + j <= endIdx; j++)
                    //{
                    //    if (transWords[startIdx + j].Equals(verseWords[j], StringComparison.OrdinalIgnoreCase))
                    //    {
                    //        matches++;
                    //    }
                    //}
                    var verseWordsEst = GetSubset(transWords, startIdx, endIdx - startIdx +1);
                    matches = CountMatchingValues(verseWordsEst, verseWords);

                    double ratio = (double)matches / verseWords.Length;
                    if (ratio >= 0.5 && (bestStart == -1 || ratio > bestRatio))
                    {
                        bestStart = startIdx;
                        bestEnd = endIdx;
                        bestRatio = ratio;
                    }
                }
            }

            if (bestStart == -1)
            {
                var (startIdx, endIdx, ratio) = FindBestMatch(transWords, currentIdx, verseWords, maxSearch);
                if (startIdx != -1 && ratio >= 0.5)
                {                    
                    bestStart = Math.Min(startIdx, currentIdx);
                    bestEnd = endIdx;
                    bestRatio = ratio;
                }
                

            }

            if (bestStart != -1)
            {
                decimal audioStart = words[bestStart].Start - startBuffer;
                decimal audioEnd = words[bestEnd].End + endBuffer;
                verses[i].AudioStart = Math.Max(audioStart, lastEnd);
                verses[i].AudioEnd = Math.Min(audioEnd, totalDuration);
                lastEnd = verses[i].AudioEnd;
                currentIdx = bestEnd + 1;
            }
            else
            {
                
                decimal estimatedStart = lastEnd;
                decimal estimatedDuration = verseWords.Length * avgWordDuration;
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