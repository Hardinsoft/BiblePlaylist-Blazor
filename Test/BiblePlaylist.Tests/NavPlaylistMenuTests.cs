using BiblePlaylist.Client.Config;
using BiblePlaylist.Client.Events;
using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.Data;
using BiblePlaylist.Shared.Playlist;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace BiblePlaylist.Tests
{
    /// <summary>
    /// Tests for #30 (Play after return from the segment editor) and #31
    /// (double-click to jump; single-click only within the playing segment).
    /// Renders NavPlaylistMenu on a return URL:
    /// /playlists?playlist=test-key&library=...&book=43&chapter=1
    ///   &segmentStart=3&segmentEnd=4
    /// so the pending return segment is the second segment (verses 3-4) of
    /// John chapter 1, and the other segment is verses 1-2.
    /// </summary>
    public class NavPlaylistMenuTests : Bunit.TestContext
    {
        public RecordingJSRuntime JSRuntime { get; private set; } = null!;
        public RecordingHttpMessageHandler HttpHandler { get; private set; } = null!;

        public NavPlaylistMenuTests()
        {
            Services.AddMudBlazorDialog();
            Services.AddMudServices();

            JSRuntime = new RecordingJSRuntime();
            Services.AddSingleton<IJSRuntime>(JSRuntime);

            Services.AddSingleton(new Endpoints());
            Services.AddScoped<IDelegateLibrary>(sp => new Mock<IDelegateLibrary>().Object);
            Services.AddBlazoredLocalStorage();

            HttpHandler = new RecordingHttpMessageHandler();
            var httpClient = new HttpClient(HttpHandler) { BaseAddress = new Uri("http://localhost/") };
            Services.AddSingleton(httpClient);
        }

        private static Segment SegmentWithVerses(int start, int end)
        {
            return new Segment
            {
                VerseStart = start,
                VerseEnd = end,
                Verses = Enumerable.Range(start, end - start + 1)
                    .Select(n => new Verse
                    {
                        Number = n,
                        Html = $"Verse {n} text.",
                        AudioStart = (decimal)(n - 1) * 10,
                        AudioEnd = (decimal)n * 10
                    })
                    .ToList()
            };
        }

        private static BookChapter ChapterWithSegments(bool inflated)
        {
            var segments = inflated
                ? new List<Segment> { SegmentWithVerses(1, 2), SegmentWithVerses(3, 4) }
                : new List<Segment>
                {
                    new Segment { VerseStart = 1, VerseEnd = 2 },
                    new Segment { VerseStart = 3, VerseEnd = 4 }
                };
            return new BookChapter
            {
                BookNumber = 43,
                ChapterNumber = 1,
                BookName = "John",
                ChapterAudioUrl = "43-John-01.mp3",
                Segments = segments
            };
        }

        private IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> RenderReturnMenu()
        {
            var library = new Library
            {
                Code = "library_greg_hardin_live_com",
                Playlists = new List<Playlist>
                {
                    new Playlist
                    {
                        Key = "test-key",
                        Name = "Test Playlist",
                        Description = "Test playlist",
                        BookChapters = new List<BookChapter> { ChapterWithSegments(inflated: false) }
                    }
                }
            };
            HttpHandler.SetupGet("library?key=library_greg_hardin_live_com", JsonConvert.SerializeObject(library));
            // Inflate must be serialized with System.Text.Json: Newtonsoft
            // strips Segment.Verses via [JsonIgnore], while the real server
            // serializes inflate with System.Text.Json, which does not.
            HttpHandler.SetupPost("playlist/inflate",
                System.Text.Json.JsonSerializer.Serialize(new List<BookChapter> { ChapterWithSegments(inflated: true) }));

            Services.GetRequiredService<NavigationManager>().NavigateTo(
                "http://localhost/playlists?playlist=test-key&library=library_greg_hardin_live_com&book=43&chapter=1&segmentStart=3&segmentEnd=4");

            var cut = RenderComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu>();
            // Return handling is async (library GET, then inflate POST).
            cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_pendingSegmentIndex")),
                TimeSpan.FromSeconds(5));
            return cut;
        }

        private static int GetInt(IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, string field)
        {
            var f = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (int)f.GetValue(cut.Instance)!;
        }

        private static void StopTimer(IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut)
        {
            var m = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu)
                .GetMethod("StopPlaybackTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(cut.Instance, null);
        }

        private bool HasPlayAudioSegment(decimal start, decimal end)
        {
            return JSRuntime.Invocations.Any(i =>
                i.Method == "PlayAudioSegment" && i.Args.Length >= 5 &&
                Convert.ToDecimal(i.Args[3]) == start && Convert.ToDecimal(i.Args[4]) == end);
        }

        [Fact]
        public void Return_PlayButton_PlaysPendingSegment()
        {
            var cut = RenderReturnMenu();
            try
            {
                Assert.Equal(-1, GetInt(cut, "_currentSegmentIndex"));

                // The AudioPlayer Play button (shown while !HasPlayed).
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));
                Assert.Equal(-1, GetInt(cut, "_pendingSegmentIndex"));
                // Second segment: verses 3-4, audio 20s-40s.
                cut.WaitForAssertion(() => Assert.True(HasPlayAudioSegment(20m, 40m)),
                    TimeSpan.FromSeconds(5));
            }
            finally { StopTimer(cut); }
        }

        [Fact]
        public void Return_ForwardButton_PlaysPendingSegmentNotNext()
        {
            var cut = RenderReturnMenu();
            try
            {
                cut.Find("[aria-label='Next segment']").Click();
                cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));
                Assert.Equal(-1, GetInt(cut, "_pendingSegmentIndex"));
                cut.WaitForAssertion(() => Assert.True(HasPlayAudioSegment(20m, 40m)),
                    TimeSpan.FromSeconds(5));
            }
            finally { StopTimer(cut); }
        }

        [Fact]
        public void SingleClick_DoesNotStartNonPlayingSegment()
        {
            var cut = RenderReturnMenu();
            try
            {
                // Click the first segment's card (not a verse row): with #31
                // there is no single-click handler on the card any more, so
                // bUnit itself reports the missing handler — that absence is
                // the assertion, plus the unchanged state below.
                var papers = cut.FindAll("div[class*='pa-2']");
                Assert.NotEmpty(papers);
                Assert.Throws<MissingEventHandlerException>(() => papers.First().Click());

                Assert.Equal(-1, GetInt(cut, "_currentSegmentIndex"));
                Assert.Equal(1, GetInt(cut, "_pendingSegmentIndex"));
                Assert.DoesNotContain(JSRuntime.Invocations, i => i.Method == "PlayAudioSegment");
            }
            finally { StopTimer(cut); }
        }

        [Fact]
        public void DoubleClick_JumpsToOtherSegmentAndPlaysFromFirstVerse()
        {
            var cut = RenderReturnMenu();
            try
            {
                // Start the pending segment (verses 3-4) via the Play button.
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));

                // Double-click a verse row of the other segment (verses 1-2).
                // Verse element ids are verse-{chapter}-{segmentStart}-{verse}.
                cut.Find("#verse-1-1-1").DoubleClick();
                cut.WaitForAssertion(() => Assert.Equal(0, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));
                cut.WaitForAssertion(() => Assert.True(HasPlayAudioSegment(0m, 20m)),
                    TimeSpan.FromSeconds(5));
            }
            finally { StopTimer(cut); }
        }

        [Fact]
        public void DoubleClick_OnPlayingSegment_IsNoOp()
        {
            var cut = RenderReturnMenu();
            try
            {
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));
                var playCalls = JSRuntime.Invocations.Count(i => i.Method == "PlayAudioSegment");

                // bUnit DoubleClick dispatches only the dblclick event (no
                // single clicks), so this isolates the dblclick handler:
                // double-clicking the playing segment must not restart it.
                cut.Find("#verse-1-3-3").DoubleClick();
                Assert.Equal(1, GetInt(cut, "_currentSegmentIndex"));
                Assert.Equal(playCalls, JSRuntime.Invocations.Count(i => i.Method == "PlayAudioSegment"));
            }
            finally { StopTimer(cut); }
        }

        [Fact]
        public void SingleClick_VerseInPlayingSegment_SeeksToThatVerse()
        {
            var cut = RenderReturnMenu();
            try
            {
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(1, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));

                // Verse 4 of the playing segment (audio start 30s).
                cut.Find("#verse-1-3-4").Click();
                cut.WaitForAssertion(() => Assert.Contains(JSRuntime.Invocations,
                    i => i.Method == "seekAudioPlayer" && i.Args.Length >= 2 &&
                         Convert.ToDecimal(i.Args[1]) == 30m),
                    TimeSpan.FromSeconds(5));
            }
            finally { StopTimer(cut); }
        }
    }
}
