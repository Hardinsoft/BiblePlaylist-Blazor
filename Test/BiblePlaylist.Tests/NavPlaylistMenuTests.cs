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
using MudBlazor;
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
    /// Tests for #30 (Play after return from the segment editor), #31
    /// (double-click to jump; single-click only within the playing segment),
    /// #37 (Add segment entry points) and #42 (deleting the last segment of
    /// a chapter prompts to delete the chapter too).
    /// Renders NavPlaylistMenu on a return URL:
    /// /playlists?playlist=test-key&library=...&book=43&chapter=1
    ///   &segmentId=2
    /// so the pending return segment is the second segment (Id 2, verses 3-4)
    /// of John chapter 1, and the other segment is verses 1-2.
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

        private static Segment SegmentWithVerses(int id, int start, int end)
        {
            return new Segment
            {
                Id = id,
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
                ? new List<Segment> { SegmentWithVerses(1, 1, 2), SegmentWithVerses(2, 3, 4) }
                : new List<Segment>
                {
                    new Segment { Id = 1, VerseStart = 1, VerseEnd = 2 },
                    new Segment { Id = 2, VerseStart = 3, VerseEnd = 4 }
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

        // #42: chapters as the library GET returns them (segments without
        // verses; inflate fills those in).
        private static Segment PlainSegment(int id, int start, int end) =>
            new Segment { Id = id, VerseStart = start, VerseEnd = end };

        private static BookChapter JohnSingleSegmentChapter() => new BookChapter
        {
            BookNumber = 43,
            ChapterNumber = 1,
            BookName = "John",
            ChapterAudioUrl = "43-John-01.mp3",
            Segments = new List<Segment> { PlainSegment(1, 1, 2) }
        };

        private static BookChapter JohnEmptyChapter() => new BookChapter
        {
            BookNumber = 43,
            ChapterNumber = 1,
            BookName = "John",
            ChapterAudioUrl = "43-John-01.mp3",
            Segments = new List<Segment>()
        };

        private static BookChapter LukeTwoSegmentChapter() => new BookChapter
        {
            BookNumber = 42,
            ChapterNumber = 1,
            BookName = "Luke",
            ChapterAudioUrl = "42-Luke-01.mp3",
            Segments = new List<Segment> { PlainSegment(1, 1, 2), PlainSegment(2, 3, 4) }
        };

        private static BookChapter InflatedChapter(BookChapter chapter) => new BookChapter
        {
            BookNumber = chapter.BookNumber,
            ChapterNumber = chapter.ChapterNumber,
            BookName = chapter.BookName,
            ChapterAudioUrl = chapter.ChapterAudioUrl,
            Segments = chapter.Segments?
                .Select(s => SegmentWithVerses(s.Id, s.VerseStart, s.VerseEnd))
                .ToList() ?? new List<Segment>()
        };

        /// <summary>
        /// #42: render the menu for a playlist built from the given chapters,
        /// navigated to via the return URL so the given chapter is expanded.
        /// Waits until inflate has completed (verse rows only render then).
        /// </summary>
        private IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> RenderMenu(
            List<BookChapter> chapters, int book, int chapter, int segmentId = 0)
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
                        BookChapters = chapters
                    }
                }
            };
            HttpHandler.SetupGet("library?key=library_greg_hardin_live_com", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPost("playlist/inflate",
                System.Text.Json.JsonSerializer.Serialize(chapters.Select(InflatedChapter).ToList()));
            HttpHandler.SetupPut("library");

            var url = $"http://localhost/playlists?playlist=test-key&library=library_greg_hardin_live_com&book={book}&chapter={chapter}";
            if (segmentId > 0) url += $"&segmentId={segmentId}";
            Services.GetRequiredService<NavigationManager>().NavigateTo(url);

            var cut = RenderComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu>();
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#verse-1-1-1")),
                TimeSpan.FromSeconds(5));
            return cut;
        }

        /// <summary>
        /// #42: start the segment card's DeleteSegment handler for the given
        /// chapter and segment (by playlist order). The ⋮ menu is a MudMenu
        /// whose popover content is rendered through JS interop, so in bUnit
        /// the menu items never appear in the markup (the recording JS runtime
        /// returns defaults for mudPopover.connect); the click path itself is
        /// covered by the real-browser check. The menu item is wired
        /// declaratively to this same method. Returns the handler's task
        /// without awaiting it: the caller drives the dialog, then awaits the
        /// task once the handler has run to completion.
        /// </summary>
        private static Task StartDeleteSegment(
            IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, int chapterIndex, int segmentIndex)
        {
            var instance = cut.Instance;
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            var playlist = (Playlist)type
                .GetProperty("selectedPlaylist", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance)!;
            var chapter = playlist.BookChapters![chapterIndex];
            var segment = chapter.Segments![segmentIndex];
            var method = type.GetMethod("DeleteSegment", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return cut.InvokeAsync(() => (Task)method.Invoke(instance, new object[] { chapter, segment })!);
        }

        /// <summary>
        /// #42: start the chapter panel menu's DeleteChapterAsync handler for
        /// the given chapter (by playlist order). Same MudMenu popover
        /// limitation as StartDeleteSegment: the menu items never render in
        /// bUnit, so the handler the menu item is wired to is driven here
        /// and the click path is covered by the real-browser check.
        /// </summary>
        private static Task StartDeleteChapterAsync(
            IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, int chapterIndex)
        {
            var instance = cut.Instance;
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            var playlist = (Playlist)type
                .GetProperty("selectedPlaylist", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance)!;
            var chapter = playlist.BookChapters![chapterIndex];
            var method = type.GetMethod("DeleteChapterAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return cut.InvokeAsync(() => (Task)method.Invoke(instance, new object[] { chapter })!);
        }

        private static void ClickDialogButton(IRenderedComponent<MudDialogProvider> provider, string text)
        {
            provider.FindAll("button").First(b => b.TextContent.Contains(text)).Click();
        }

        private async Task<Library?> GetSavedLibrary()
        {
            var putRequest = HttpHandler.Requests.LastOrDefault(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri?.AbsoluteUri.Contains("library") == true);
            if (putRequest?.Content == null) return null;

            var putBody = await putRequest.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
            return JsonConvert.DeserializeObject<Library>(putBody);
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
                "http://localhost/playlists?playlist=test-key&library=library_greg_hardin_live_com&book=43&chapter=1&segmentId=2");

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

        /// <summary>
        /// #37: the per-chapter "Add segment" button opens the editor in
        /// Create mode for that chapter — no segmentId in the URL.
        /// </summary>
        [Fact]
        public void Chapter_AddSegment_NavigatesToEditorCreateMode()
        {
            var cut = RenderReturnMenu();
            try
            {
                var addButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("Add segment"));
                Assert.NotNull(addButton);
                addButton!.Click();

                var uri = Services.GetRequiredService<NavigationManager>().Uri;
                Assert.Contains("/edit-segment?", uri);
                Assert.Contains("book=43", uri);
                Assert.Contains("chapter=1", uri);
                Assert.Contains("playlist=test-key", uri);
                Assert.DoesNotContain("segmentId", uri);
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #37: the playlist-level "Add segment to a new chapter" button
        /// shows the one-time explainer dialog on first click.
        /// </summary>
        [Fact]
        public void Playlist_AddSegmentToNewChapter_FirstClick_ShowsExplainerDialog()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderReturnMenu();
            try
            {
                var addButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("Add segment to a new chapter"));
                Assert.NotNull(addButton);
                addButton!.Click();

                cut.WaitForAssertion(() =>
                    Assert.Contains("Pick the chapter on the Read page", provider.Markup),
                    TimeSpan.FromSeconds(5));
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #37: choosing "Go to Read page" in the explainer navigates to the
        /// Read page, remembers the explainer was used, and the next click
        /// on the button goes straight to the Read page without the dialog.
        /// </summary>
        [Fact]
        public void Playlist_AddSegmentToNewChapter_DialogGo_NavigatesAndSecondClickGoesDirect()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderReturnMenu();
            try
            {
                var addButton = cut.FindAll("button")
                    .First(b => b.TextContent.Contains("Add segment to a new chapter"));
                addButton.Click();

                cut.WaitForAssertion(() =>
                    Assert.Contains("Pick the chapter on the Read page", provider.Markup),
                    TimeSpan.FromSeconds(5));

                var goButton = provider.FindAll("button")
                    .First(b => b.TextContent.Contains("Go to Read page"));
                goButton.Click();

                cut.WaitForAssertion(() =>
                    Assert.Equal("http://localhost/",
                        Services.GetRequiredService<NavigationManager>().Uri),
                    TimeSpan.FromSeconds(5));
                cut.WaitForAssertion(() =>
                    Assert.DoesNotContain("Pick the chapter on the Read page", provider.Markup),
                    TimeSpan.FromSeconds(5));

                // The one-time flag was persisted.
                Assert.Contains(JSRuntime.Invocations, i =>
                    i.Method == "localStorage.setItem" &&
                    i.Args.Length >= 1 &&
                    i.Args[0]?.ToString() == "userName_addSegmentHelpSeen");

                // Second click: straight to the Read page, no dialog again.
                var navManager = Services.GetRequiredService<NavigationManager>();
                navManager.NavigateTo("/playlists?playlist=test-key&library=library_greg_hardin_live_com&book=43&chapter=1&segmentId=2");
                addButton = cut.FindAll("button")
                    .First(b => b.TextContent.Contains("Add segment to a new chapter"));
                addButton.Click();

                cut.WaitForAssertion(() =>
                    Assert.Equal("http://localhost/", navManager.Uri),
                    TimeSpan.FromSeconds(5));
                Assert.DoesNotContain("Pick the chapter on the Read page", provider.Markup);
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: deleting the last segment of a chapter prompts to delete the
        /// chapter too; confirming removes the chapter from the playlist and saves.
        /// </summary>
        [Fact]
        public async Task DeleteLastSegment_PromptConfirm_DeletesChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1, segmentId: 1);
            try
            {
                var deleteTask = StartDeleteSegment(cut, chapterIndex: 0, segmentIndex: 0);

                cut.WaitForAssertion(() =>
                    Assert.Contains("Delete the chapter from the playlist as well?", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Delete chapter");

                cut.WaitForAssertion(() =>
                    Assert.Contains(HttpHandler.Requests, r => r.Method == HttpMethod.Put),
                    TimeSpan.FromSeconds(5));
                var savedLibrary = await GetSavedLibrary();
                Assert.NotNull(savedLibrary);
                var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
                Assert.NotNull(chapters);
                Assert.Single(chapters!);
                Assert.Equal("Luke", chapters![0].BookName);
                Assert.Equal(2, chapters[0].Segments!.Count);

                cut.WaitForAssertion(() => Assert.DoesNotContain("John 1", cut.Markup),
                    TimeSpan.FromSeconds(5));
                Assert.Equal(-1, GetInt(cut, "_pendingSegmentIndex"));
                Assert.Equal(-1, GetInt(cut, "_currentSegmentIndex"));

                await deleteTask;
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: deleting the last segment of a chapter prompts to delete the
        /// chapter too; keeping it leaves the empty chapter, which shows the
        /// empty state with Add segment and Delete chapter actions.
        /// </summary>
        [Fact]
        public async Task DeleteLastSegment_PromptKeep_LeavesEmptyChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1, segmentId: 1);
            try
            {
                var deleteTask = StartDeleteSegment(cut, chapterIndex: 0, segmentIndex: 0);

                cut.WaitForAssertion(() =>
                    Assert.Contains("Delete the chapter from the playlist as well?", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Keep chapter");

                cut.WaitForAssertion(() =>
                    Assert.Contains(HttpHandler.Requests, r => r.Method == HttpMethod.Put),
                    TimeSpan.FromSeconds(5));
                var savedLibrary = await GetSavedLibrary();
                Assert.NotNull(savedLibrary);
                var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
                Assert.NotNull(chapters);
                Assert.Equal(2, chapters!.Count);
                Assert.Equal("John", chapters[0].BookName);
                Assert.NotNull(chapters[0].Segments);
                Assert.Empty(chapters[0].Segments!);
                Assert.Equal(2, chapters[1].Segments!.Count);

                cut.WaitForAssertion(() => Assert.Contains("No segments defined for this chapter.", cut.Markup),
                    TimeSpan.FromSeconds(5));
                Assert.Contains(cut.FindAll("button"), b => b.TextContent.Contains("Delete chapter"));
                Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Add segment");

                await deleteTask;
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: an empty chapter shows a Delete chapter button (chapters with
        /// segments do not); confirming the dialog removes the chapter and saves.
        /// </summary>
        [Fact]
        public async Task EmptyChapter_DeleteChapterButton_ConfirmRemovesChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnEmptyChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1);
            try
            {
                Assert.Single(cut.FindAll("button"), b => b.TextContent.Contains("Delete chapter"));

                cut.FindAll("button").First(b => b.TextContent.Contains("Delete chapter")).Click();

                cut.WaitForAssertion(() =>
                    Assert.Contains("This chapter has no segments.", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Delete chapter");

                cut.WaitForAssertion(() =>
                    Assert.Contains(HttpHandler.Requests, r => r.Method == HttpMethod.Put),
                    TimeSpan.FromSeconds(5));
                var savedLibrary = await GetSavedLibrary();
                Assert.NotNull(savedLibrary);
                var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
                Assert.NotNull(chapters);
                Assert.Single(chapters!);
                Assert.Equal("Luke", chapters![0].BookName);
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: the empty-chapter Delete chapter button asks first, but
        /// cancelling keeps the chapter and saves nothing.
        /// </summary>
        [Fact]
        public void EmptyChapter_DeleteChapterButton_CancelKeepsChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnEmptyChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1);
            try
            {
                cut.FindAll("button").First(b => b.TextContent.Contains("Delete chapter")).Click();

                cut.WaitForAssertion(() =>
                    Assert.Contains("This chapter has no segments.", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Keep chapter");

                cut.WaitForAssertion(() =>
                    Assert.DoesNotContain("This chapter has no segments.", provider.Markup),
                    TimeSpan.FromSeconds(5));
                Assert.DoesNotContain(HttpHandler.Requests, r => r.Method == HttpMethod.Put);
                Assert.Contains("John 1", cut.Markup);
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: deleting a segment that is not the last one never prompts;
        /// the chapter keeps its remaining segment.
        /// </summary>
        [Fact]
        public async Task DeleteNonLastSegment_DoesNotPrompt()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { LukeTwoSegmentChapter() },
                book: 42, chapter: 1, segmentId: 2);
            try
            {
                var deleteTask = StartDeleteSegment(cut, chapterIndex: 0, segmentIndex: 0);

                cut.WaitForAssertion(() =>
                    Assert.Contains(HttpHandler.Requests, r => r.Method == HttpMethod.Put),
                    TimeSpan.FromSeconds(5));
                Assert.DoesNotContain("Delete chapter?", provider.Markup);

                var savedLibrary = await GetSavedLibrary();
                Assert.NotNull(savedLibrary);
                var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
                Assert.NotNull(chapters);
                Assert.Single(chapters!);
                Assert.Single(chapters![0].Segments!);
                Assert.Equal(2, chapters[0].Segments![0].Id);
                Assert.Equal(3, chapters[0].Segments[0].VerseStart);
                Assert.Equal(4, chapters[0].Segments[0].VerseEnd);

                await deleteTask;
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: the chapter panel menu offers a Delete chapter item on every
        /// chapter; confirming it deletes the chapter and all its segments,
        /// and the dialog names how many segments go with it.
        /// </summary>
        [Fact]
        public async Task ChapterMenu_DeleteChapter_ConfirmRemovesChapterWithSegments()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1, segmentId: 1);
            try
            {
                // The ⋮ menu activator renders per chapter; its popover items
                // do not render in bUnit (see StartDeleteSegment).
                Assert.Equal(2, cut.FindAll("[aria-label='Chapter options']").Count());

                var deleteTask = StartDeleteChapterAsync(cut, chapterIndex: 1);

                cut.WaitForAssertion(() =>
                    Assert.Contains("and its 2 segments from the playlist?", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Delete chapter");

                cut.WaitForAssertion(() =>
                    Assert.Contains(HttpHandler.Requests, r => r.Method == HttpMethod.Put),
                    TimeSpan.FromSeconds(5));
                var savedLibrary = await GetSavedLibrary();
                Assert.NotNull(savedLibrary);
                var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
                Assert.NotNull(chapters);
                Assert.Single(chapters!);
                Assert.Equal("John", chapters![0].BookName);
                Assert.Single(chapters[0].Segments!);

                await deleteTask;
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #42: cancelling the chapter menu's delete keeps the chapter and
        /// its segments, and saves nothing. Uses the one-segment chapter, so
        /// the dialog's singular wording is covered too.
        /// </summary>
        [Fact]
        public async Task ChapterMenu_DeleteChapter_CancelKeepsChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1, segmentId: 1);
            try
            {
                var deleteTask = StartDeleteChapterAsync(cut, chapterIndex: 0);

                cut.WaitForAssertion(() =>
                    Assert.Contains("and its 1 segment from the playlist?", provider.Markup),
                    TimeSpan.FromSeconds(5));

                ClickDialogButton(provider, "Keep chapter");

                cut.WaitForAssertion(() =>
                    Assert.DoesNotContain("and its 1 segment from the playlist?", provider.Markup),
                    TimeSpan.FromSeconds(5));
                Assert.DoesNotContain(HttpHandler.Requests, r => r.Method == HttpMethod.Put);
                Assert.Contains("John 1", cut.Markup);

                await deleteTask;
            }
            finally { StopTimer(cut); }
        }
    }
}
