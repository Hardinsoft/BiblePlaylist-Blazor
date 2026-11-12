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
    /// #37 (Add segment entry points), #42 (deleting the last segment of
    /// a chapter prompts to delete the chapter too) and #45 (edit/delete
    /// playlists; edit mode hides the edit controls in listen/read mode).
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
        private static Segment PlainSegment(int id, int start, int end, string? introText = null) =>
            new Segment { Id = id, VerseStart = start, VerseEnd = end, IntroText = introText };

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
            // #39: inflate rebuilds the segments, so the intro text must be
            // carried over (as the server's Inflate does) or the menu would
            // never see it.
            Segments = chapter.Segments?
                .Select(s =>
                {
                    var inflated = SegmentWithVerses(s.Id, s.VerseStart, s.VerseEnd);
                    inflated.IntroText = s.IntroText;
                    return inflated;
                })
                .ToList() ?? new List<Segment>()
        };

        /// <summary>
        /// #42: render the menu for a playlist built from the given chapters,
        /// navigated to via the return URL so the given chapter is expanded.
        /// Waits until inflate has completed (verse rows only render then).
        /// </summary>
        private IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> RenderMenu(
            List<BookChapter> chapters, int book, int chapter, int segmentId = 0, string? description = "Test playlist", bool edit = false)
        {
            var playlist = new Playlist
            {
                Key = "test-key",
                Name = "Test Playlist",
                Description = description,
                BookChapters = chapters
            };
            return RenderMenuWithPlaylists(new List<Playlist> { playlist }, "test-key", book, chapter, segmentId, edit);
        }

        /// <summary>
        /// #45: render the menu for the given playlists, navigated to via the
        /// return URL so the given playlist is selected and the given chapter
        /// is expanded. Waits until inflate has completed (verse rows only
        /// render then). The edit flag appends edit=1 to the return URL, which
        /// restores edit mode on return. The wait assumes the selected
        /// playlist's first chapter has a verse-1 segment.
        /// </summary>
        private IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> RenderMenuWithPlaylists(
            List<Playlist> playlists, string selectedKey, int book, int chapter, int segmentId = 0, bool edit = false)
        {
            var library = new Library
            {
                Code = "library_greg_hardin_live_com",
                Playlists = playlists
            };
            HttpHandler.SetupGet("library?key=library_greg_hardin_live_com", JsonConvert.SerializeObject(library));
            var selected = playlists.First(p => p.Key == selectedKey);
            HttpHandler.SetupPost("playlist/inflate",
                System.Text.Json.JsonSerializer.Serialize(
                    (selected.BookChapters ?? new List<BookChapter>()).Select(InflatedChapter).ToList()));
            HttpHandler.SetupPut("library");

            var url = $"http://localhost/playlists?playlist={selectedKey}&library=library_greg_hardin_live_com&book={book}&chapter={chapter}";
            if (segmentId > 0) url += $"&segmentId={segmentId}";
            if (edit) url += "&edit=1";
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

        /// <summary>
        /// #45: start the playlist ⋮ menu's "Edit name & description" handler
        /// for the given playlist (by library order). Same MudMenu popover
        /// limitation as StartDeleteSegment: the menu items never render in
        /// bUnit, so the handler the menu item is wired to is driven here
        /// and the click path is covered by the real-browser check. Returns
        /// the handler's task without awaiting it: the caller drives the
        /// dialog, then awaits the task once the handler has run to completion.
        /// </summary>
        private static Task StartEditPlaylistAsync(
            IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, int playlistIndex)
        {
            var instance = cut.Instance;
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            var library = (Library)type
                .GetProperty("Library", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance)!;
            var playlist = library.Playlists![playlistIndex];
            var method = type.GetMethod("StartEditPlaylistAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return cut.InvokeAsync(() => (Task)method.Invoke(instance, new object[] { playlist })!);
        }

        /// <summary>
        /// #45: start the playlist ⋮ menu's "Delete playlist" handler for the
        /// given playlist (by library order). Same MudMenu popover limitation
        /// as StartEditPlaylistAsync.
        /// </summary>
        private static Task StartDeletePlaylistAsync(
            IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, int playlistIndex)
        {
            var instance = cut.Instance;
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            var library = (Library)type
                .GetProperty("Library", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance)!;
            var playlist = library.Playlists![playlistIndex];
            var method = type.GetMethod("StartDeletePlaylistAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return cut.InvokeAsync(() => (Task)method.Invoke(instance, new object[] { playlist })!);
        }

        /// <summary>
        /// #45: invoke the playlist ⋮ menu's edit-mode toggle handler for the
        /// selected playlist (the menu item is wired to ToggleEditModeAsync;
        /// the menu items never render in bUnit). Toggles edit mode on or off.
        /// </summary>
        private static async Task ToggleEditMode(
            IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut)
        {
            var instance = cut.Instance;
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            var playlist = (Playlist)type
                .GetProperty("selectedPlaylist", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(instance)!;
            var method = type.GetMethod("ToggleEditModeAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await cut.InvokeAsync(() => (Task)method.Invoke(instance, new object[] { playlist })!);
        }

        private static bool GetBool(IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut, string field)
        {
            var f = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (bool)f.GetValue(cut.Instance)!;
        }

        private static Playlist? GetSelectedPlaylist(IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut)
        {
            var type = typeof(BiblePlaylist.Client.Shared.NavPlaylistMenu);
            return (Playlist?)type
                .GetProperty("selectedPlaylist", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(cut.Instance);
        }

        /// <summary>
        /// #45: whether any edit control is present in the markup — a
        /// per-segment ⋮ menu, a per-chapter Add segment button, the
        /// Add-new-chapter button, or a chapter header ⋮ menu. In listen/read
        /// mode none of them render.
        /// </summary>
        private static bool HasEditControls(IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> cut)
        {
            return cut.FindAll("[aria-label='Segment options']").Count > 0
                || cut.FindAll("[aria-label='Chapter options']").Count > 0
                || cut.FindAll("button").Any(b => b.TextContent.Trim() == "Add segment")
                || cut.FindAll("button").Any(b => b.TextContent.Contains("Add segment to a new chapter"));
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
        /// #45: the button is an edit control, so edit mode is entered first.
        /// </summary>
        [Fact]
        public async Task Chapter_AddSegment_NavigatesToEditorCreateMode()
        {
            var cut = RenderReturnMenu();
            try
            {
                await ToggleEditMode(cut);
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
        /// Adding a segment to a new chapter from the playlist goes straight
        /// to the editor in Create mode with the playlist already set — no
        /// explainer dialog, no trip to the Read page, no playlist to pick.
        /// The editor opens with no chapter (book/chapter 0), so its chapter
        /// picker is the entry point.
        /// #45: the button is an edit control, so edit mode is entered first
        /// and the editor URL carries edit=1.
        /// </summary>
        [Fact]
        public async Task Playlist_AddSegmentToNewChapter_NavigatesToEditorCreateMode()
        {
            var cut = RenderReturnMenu();
            try
            {
                await ToggleEditMode(cut);
                var addButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("Add segment to a new chapter"));
                Assert.NotNull(addButton);
                addButton!.Click();

                cut.WaitForAssertion(() =>
                    Assert.Equal("http://localhost/edit-segment?library=library_greg_hardin_live_com&book=0&chapter=0&playlist=test-key&edit=1",
                        Services.GetRequiredService<NavigationManager>().Uri),
                    TimeSpan.FromSeconds(5));

                // No explainer dialog, no one-time flag: the button no longer
                // routes through the Read page at all.
                Assert.DoesNotContain("Pick the chapter on the Read page", cut.Markup);
                Assert.DoesNotContain(JSRuntime.Invocations, i =>
                    i.Method == "localStorage.setItem" &&
                    i.Args.Length >= 1 &&
                    i.Args[0]?.ToString() == "userName_addSegmentHelpSeen");
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
                // #45: the empty-chapter actions are edit controls — enter edit
                // mode to see them.
                await ToggleEditMode(cut);
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
                // #45: the empty-chapter Delete button is an edit control.
                await ToggleEditMode(cut);
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
        public async Task EmptyChapter_DeleteChapterButton_CancelKeepsChapter()
        {
            var provider = RenderComponent<MudDialogProvider>();
            var cut = RenderMenu(new List<BookChapter> { JohnEmptyChapter(), LukeTwoSegmentChapter() },
                book: 43, chapter: 1);
            try
            {
                // #45: the empty-chapter Delete button is an edit control.
                await ToggleEditMode(cut);
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
                // #45: the chapter ⋮ menus are edit controls — enter edit mode
                // to see them. The ⋮ menu activator renders per chapter; its
                // popover items do not render in bUnit (see StartDeleteSegment).
                await ToggleEditMode(cut);
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
        /// #39: a segment with intro text shows it as the card's heading,
        /// above the verse rows. The intro is a description introducing the
        /// Scripture, not Scripture itself, so the heading is centered, bold,
        /// and the same size as the verse text. Asserted by the visible
        /// outcome, not by which MudBlazor Typo renders it, so a styling
        /// change that keeps the outcome does not break the test.
        /// </summary>
        [Fact]
        public void SegmentCard_WithIntroText_ShowsItAsHeading()
        {
            var chapter = new BookChapter
            {
                BookNumber = 43,
                ChapterNumber = 1,
                BookName = "John",
                ChapterAudioUrl = "43-John-01.mp3",
                Segments = new List<Segment> { PlainSegment(1, 1, 2, "The fall of man") }
            };
            var cut = RenderMenu(new List<BookChapter> { chapter }, book: 43, chapter: 1);
            try
            {
                Assert.Contains("The fall of man", cut.Markup);
                Assert.Contains("Verse 1 text.", cut.Markup);

                var heading = cut.FindAll(".font-weight-bold")
                    .First(e => e.TextContent.Contains("The fall of man"));
                Assert.Contains("font-weight-bold", heading.GetAttribute("class"));
                Assert.Contains("text-align: center", heading.GetAttribute("style"));
                Assert.Contains("font-size: 1.05rem", heading.GetAttribute("style"));

                // The heading matches the verse text size (the requirement),
                // rather than a larger heading size.
                var verseText = cut.Find(".verse-text");
                Assert.Contains("font-size: 1.05rem", verseText.GetAttribute("style"));
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #39: a segment without intro text renders the card as today — no
        /// heading, verse rows unchanged.
        /// </summary>
        [Fact]
        public void SegmentCard_WithoutIntroText_HasNoHeading()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() },
                book: 43, chapter: 1);
            try
            {
                Assert.Contains("Verse 1 text.", cut.Markup);
                Assert.DoesNotContain("The fall of man", cut.Markup);
            }
            finally { StopTimer(cut); }
        }

        private List<string> GetSpokenTexts()
        {
            var speak = JSRuntime.Invocations.Single(i => i.Method == "speakSequenceAsync");
            return Assert.IsAssignableFrom<IEnumerable<string>>(speak.Args[0]).ToList();
        }

        private IRenderedComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu> RenderIntroMenu(string? description)
        {
            var chapter = new BookChapter
            {
                BookNumber = 43,
                ChapterNumber = 1,
                BookName = "John",
                ChapterAudioUrl = "43-John-01.mp3",
                Segments = new List<Segment> { PlainSegment(1, 1, 2, "The fall of man") }
            };
            // segmentId 1 makes the intro segment the pending (then current)
            // segment, so Play speaks for it.
            return RenderMenu(new List<BookChapter> { chapter }, book: 43, chapter: 1,
                segmentId: 1, description: description);
        }

        /// <summary>
        /// #39: with an intro and no playlist description, playback TTS is
        /// exactly intro, then chapter, then verses.
        /// </summary>
        [Fact]
        public void PlayCurrentSegment_WithIntroText_SpeaksIntroFirst()
        {
            var cut = RenderIntroMenu(description: null);
            try
            {
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(0, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));

                var expectedChapter = new BookChapter { BookName = "John", ChapterNumber = 1 }.VoiceText;
                var expectedSegment = new Segment { VerseStart = 1, VerseEnd = 2 }.VoiceText;
                Assert.Equal(new[] { "The fall of man", expectedChapter, expectedSegment }, GetSpokenTexts());
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #39: the playlist Description (first segment only) stays before
        /// the intro — playlist-level first, then the section heading, then
        /// chapter and verses.
        /// </summary>
        [Fact]
        public void PlayCurrentSegment_WithDescriptionAndIntro_SpeaksDescriptionThenIntro()
        {
            var cut = RenderIntroMenu(description: "Test playlist");
            try
            {
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(0, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));

                var expectedChapter = new BookChapter { BookName = "John", ChapterNumber = 1 }.VoiceText;
                var expectedSegment = new Segment { VerseStart = 1, VerseEnd = 2 }.VoiceText;
                Assert.Equal(new[] { "Test playlist", "The fall of man", expectedChapter, expectedSegment },
                    GetSpokenTexts());
            }
            finally { StopTimer(cut); }
        }

        /// <summary>
        /// #39: with no intro text the playback sequence is exactly today's
        /// (description, chapter, verses) — no empty first entry.
        /// </summary>
        [Fact]
        public void PlayCurrentSegment_WithoutIntroText_SequenceUnchanged()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() },
                book: 43, chapter: 1, segmentId: 1);
            try
            {
                cut.Find("[aria-label='Play audio']").Click();
                cut.WaitForAssertion(() => Assert.Equal(0, GetInt(cut, "_currentSegmentIndex")),
                    TimeSpan.FromSeconds(5));

                var expectedChapter = new BookChapter { BookName = "John", ChapterNumber = 1 }.VoiceText;
                var expectedSegment = new Segment { VerseStart = 1, VerseEnd = 2 }.VoiceText;
                Assert.Equal(new[] { "Test playlist", expectedChapter, expectedSegment }, GetSpokenTexts());
            }
            finally { StopTimer(cut); }
        }

        // #45: two playlists with the same John chapter, so the inflate
        // response is correct whichever one is selected.
        private static List<Playlist> TwoPlaylists() => new List<Playlist>
        {
            new Playlist { Key = "test-key", Name = "Test Playlist", Description = "Test playlist", BookChapters = new List<BookChapter> { JohnSingleSegmentChapter() } },
            new Playlist { Key = "second-key", Name = "Second Playlist", Description = "Second", BookChapters = new List<BookChapter> { JohnSingleSegmentChapter() } }
        };

        /// <summary>
        /// #45: the playlist ⋮ menu's "Edit name & description" saves the new
        /// name and description in place — the Key never changes — PUTs the
        /// library, and keeps the playlist selected.
        /// </summary>
        [Fact]
        public async Task EditPlaylist_Save_UpdatesNameAndDescriptionKeepsSelected()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() }, 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var editTask = StartEditPlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Edit playlist", provider.Markup), TimeSpan.FromSeconds(5));

            provider.FindAll("input").First().Change("Renamed Playlist");
            provider.FindAll("input").Skip(1).First().Change("A new description");
            ClickDialogButton(provider, "Save");
            await editTask;

            var saved = await GetSavedLibrary();
            Assert.NotNull(saved);
            var playlist = Assert.Single(saved!.Playlists);
            Assert.Equal("test-key", playlist.Key);
            Assert.Equal("Renamed Playlist", playlist.Name);
            Assert.Equal("A new description", playlist.Description);

            var selected = GetSelectedPlaylist(cut);
            Assert.NotNull(selected);
            Assert.Equal("test-key", selected!.Key);
            Assert.Equal("Renamed Playlist", selected.Name);
            Assert.NotNull(cut.Find("#Renamed_Playlist"));
        }

        /// <summary>
        /// #45: an empty name is rejected in the dialog with a clear message
        /// and nothing is saved; the dialog stays open so the user can fix it.
        /// </summary>
        [Fact]
        public async Task EditPlaylist_EmptyName_Rejected()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() }, 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var editTask = StartEditPlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Edit playlist", provider.Markup), TimeSpan.FromSeconds(5));

            provider.FindAll("input").First().Change("   ");
            ClickDialogButton(provider, "Save");

            cut.WaitForAssertion(() => Assert.Contains("Name is required.", provider.Markup), TimeSpan.FromSeconds(5));
            Assert.Null(await GetSavedLibrary());

            ClickDialogButton(provider, "Cancel");
            await editTask;
            Assert.Null(await GetSavedLibrary());
        }

        /// <summary>
        /// #45: a duplicate name (case-insensitive) is rejected in the dialog
        /// with a clear message and nothing is saved.
        /// </summary>
        [Fact]
        public async Task EditPlaylist_DuplicateName_Rejected()
        {
            var cut = RenderMenuWithPlaylists(TwoPlaylists(), "test-key", 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var editTask = StartEditPlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Edit playlist", provider.Markup), TimeSpan.FromSeconds(5));

            provider.FindAll("input").First().Change("second playlist");
            ClickDialogButton(provider, "Save");

            cut.WaitForAssertion(() => Assert.Contains("already exists", provider.Markup), TimeSpan.FromSeconds(5));
            Assert.Null(await GetSavedLibrary());

            ClickDialogButton(provider, "Cancel");
            await editTask;
            Assert.Null(await GetSavedLibrary());
        }

        /// <summary>
        /// #45: deleting a playlist (confirm) removes it, saves, and selects
        /// the first remaining playlist.
        /// </summary>
        [Fact]
        public async Task DeletePlaylist_Confirm_RemovesAndSelectsFirstRemaining()
        {
            var cut = RenderMenuWithPlaylists(TwoPlaylists(), "test-key", 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var deleteTask = StartDeletePlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Delete playlist?", provider.Markup), TimeSpan.FromSeconds(5));

            ClickDialogButton(provider, "Delete playlist");
            await deleteTask;

            var saved = await GetSavedLibrary();
            Assert.NotNull(saved);
            var remaining = Assert.Single(saved!.Playlists);
            Assert.Equal("second-key", remaining.Key);
            Assert.Equal("Second Playlist", remaining.Name);

            var selected = GetSelectedPlaylist(cut);
            Assert.NotNull(selected);
            Assert.Equal("second-key", selected!.Key);
        }

        /// <summary>
        /// #45: cancelling the delete confirm keeps the playlist and saves nothing.
        /// </summary>
        [Fact]
        public async Task DeletePlaylist_Cancel_KeepsPlaylist()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() }, 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var deleteTask = StartDeletePlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Delete playlist?", provider.Markup), TimeSpan.FromSeconds(5));

            ClickDialogButton(provider, "Keep playlist");
            await deleteTask;

            Assert.Null(await GetSavedLibrary());
            var selected = GetSelectedPlaylist(cut);
            Assert.NotNull(selected);
            Assert.Equal("test-key", selected!.Key);
        }

        /// <summary>
        /// #45: deleting the last playlist is allowed and leaves an empty list
        /// with no playlist selected.
        /// </summary>
        [Fact]
        public async Task DeletePlaylist_LastPlaylist_LeavesEmptyList()
        {
            var cut = RenderMenu(new List<BookChapter> { JohnSingleSegmentChapter() }, 43, 1);
            var provider = RenderComponent<MudDialogProvider>();

            var deleteTask = StartDeletePlaylistAsync(cut, playlistIndex: 0);
            cut.WaitForAssertion(() => Assert.Contains("Delete playlist?", provider.Markup), TimeSpan.FromSeconds(5));

            ClickDialogButton(provider, "Delete playlist");
            await deleteTask;

            var saved = await GetSavedLibrary();
            Assert.NotNull(saved);
            Assert.Empty(saved!.Playlists);
            Assert.Null(GetSelectedPlaylist(cut));
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-list-item")), TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// #45: in listen/read mode (the default) the edit controls are hidden:
        /// no segment ⋮ menus, no Add segment buttons, no Add-new-chapter button,
        /// and no chapter ⋮ menus.
        /// </summary>
        [Fact]
        public void EditMode_ByDefault_HidesEditControls()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2);

            Assert.False(GetBool(cut, "_isEditMode"));
            Assert.False(HasEditControls(cut));
            Assert.DoesNotContain("Done editing", cut.Markup);
        }

        /// <summary>
        /// #45: "Edit segments" reveals the edit controls for the playlist.
        /// </summary>
        [Fact]
        public async Task EditMode_EnterEditSegments_RevealsEditControls()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2);

            Assert.False(HasEditControls(cut));
            await ToggleEditMode(cut);

            cut.WaitForAssertion(() => Assert.True(HasEditControls(cut)), TimeSpan.FromSeconds(5));
            Assert.True(GetBool(cut, "_isEditMode"));
            Assert.Contains("Done editing", cut.Markup);
        }

        /// <summary>
        /// #45: the "Done editing" button returns to listen/read mode and
        /// hides the edit controls again.
        /// </summary>
        [Fact]
        public async Task EditMode_DoneEditingButton_ExitsEditMode()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2);
            await ToggleEditMode(cut);
            cut.WaitForAssertion(() => Assert.True(HasEditControls(cut)), TimeSpan.FromSeconds(5));

            cut.FindAll("button").First(b => b.TextContent.Contains("Done editing")).Click();
            cut.WaitForAssertion(() => Assert.False(HasEditControls(cut)), TimeSpan.FromSeconds(5));
            Assert.False(GetBool(cut, "_isEditMode"));
        }

        /// <summary>
        /// #45: toggling edit mode off (the playlist ⋮ menu's "Done editing"
        /// item is wired to the same handler; the menu items never render in
        /// bUnit, so the handler is driven here and the click path is covered
        /// by the real-browser check) hides the edit controls again.
        /// </summary>
        [Fact]
        public async Task EditMode_ToggleOff_HidesEditControls()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2);
            await ToggleEditMode(cut);
            cut.WaitForAssertion(() => Assert.True(HasEditControls(cut)), TimeSpan.FromSeconds(5));

            await ToggleEditMode(cut);
            cut.WaitForAssertion(() => Assert.False(HasEditControls(cut)), TimeSpan.FromSeconds(5));
            Assert.False(GetBool(cut, "_isEditMode"));
        }

        /// <summary>
        /// #45: selecting a different playlist exits edit mode — the newly
        /// selected playlist shows in listen/read mode.
        /// </summary>
        [Fact]
        public async Task EditMode_SelectDifferentPlaylist_ExitsEditMode()
        {
            var playlists = new List<Playlist>
            {
                new Playlist { Key = "test-key", Name = "Test Playlist", Description = "Test playlist", BookChapters = new List<BookChapter> { ChapterWithSegments(inflated: false) } },
                new Playlist { Key = "second-key", Name = "Second Playlist", Description = "Second", BookChapters = new List<BookChapter> { ChapterWithSegments(inflated: false) } }
            };
            var cut = RenderMenuWithPlaylists(playlists, "test-key", 43, 1, segmentId: 2);
            await ToggleEditMode(cut);
            cut.WaitForAssertion(() => Assert.True(HasEditControls(cut)), TimeSpan.FromSeconds(5));

            cut.Find("#Second_Playlist").Click();
            cut.WaitForAssertion(() => Assert.False(GetBool(cut, "_isEditMode")), TimeSpan.FromSeconds(5));
            Assert.False(HasEditControls(cut));
            Assert.Equal("second-key", GetSelectedPlaylist(cut)!.Key);
        }

        /// <summary>
        /// #45: returning from the editor with edit=1 restores edit mode.
        /// </summary>
        [Fact]
        public void EditMode_ReturnWithEditFlag_RestoresEditMode()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2, edit: true);

            Assert.True(GetBool(cut, "_isEditMode"));
            Assert.True(HasEditControls(cut));
        }

        /// <summary>
        /// #45: returning from the editor without edit=1 stays in listen/read mode.
        /// </summary>
        [Fact]
        public void EditMode_ReturnWithoutEditFlag_StaysInListenMode()
        {
            var cut = RenderMenu(new List<BookChapter> { ChapterWithSegments(inflated: false) }, 43, 1, segmentId: 2);

            Assert.False(GetBool(cut, "_isEditMode"));
            Assert.False(HasEditControls(cut));
        }
    }
}
