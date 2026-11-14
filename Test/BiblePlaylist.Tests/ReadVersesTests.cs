using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.Data;
using BiblePlaylist.Shared.DTO;
using BiblePlaylist.Shared.Playlist;
using BiblePlaylist.Client.Config;
using BiblePlaylist.Client.Events;
using Bunit;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace BiblePlaylist.Tests
{
    /// <summary>
    /// Tests for #37 (Read page: verse selection + add to playlist).
    /// RecordingJSRuntime and RecordingHttpMessageHandler are shared from
    /// SelectVersesTests (same assembly and namespace).
    /// </summary>
    public class ReadVersesTests : Bunit.TestContext
    {
        public RecordingJSRuntime JSRuntime { get; private set; } = null!;
        public RecordingHttpMessageHandler HttpHandler { get; private set; } = null!;

        public ReadVersesTests()
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

        private void SetupVersionResponse(int bookNumber, int chapterNumber)
        {
            var version = new Shared.Bible.Version
            {
                Books = new List<Book>
                {
                    new Book
                    {
                        Number = bookNumber,
                        Name = "Test Book",
                        Chapters = new List<Chapter>
                        {
                            new Chapter
                            {
                                Number = chapterNumber,
                                AudioUrl = "http://example.com/chapter.mp3",
                                Verses = Enumerable.Range(1, 5)
                                    .Select(n => new Verse
                                    {
                                        Number = n,
                                        Html = $"Verse {n} text.",
                                        AudioStart = (decimal)(n - 1) * 10,
                                        AudioEnd = (decimal)n * 10
                                    })
                                    .ToList()
                            }
                        }
                    }
                }
            };

            HttpHandler.SetupGet($"Book={bookNumber}", JsonConvert.SerializeObject(version));
        }

        private void SetupLibraryResponse()
        {
            var library = new Library
            {
                Code = "library_greg_hardin_live_com",
                Playlists = new List<Playlist>
                {
                    new Playlist { Key = "christmas-story", Name = "Christmas Story", BookChapters = new List<BookChapter>() },
                    new Playlist { Key = "test-key", Name = "Test Playlist", BookChapters = new List<BookChapter>() }
                }
            };

            HttpHandler.SetupGet("library?key=library_greg_hardin_live_com", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPut("library");
        }

        private IRenderedComponent<BiblePlaylist.Client.Shared.ReadVerses> RenderReadVerses()
        {
            var cut = RenderComponent<BiblePlaylist.Client.Shared.ReadVerses>();
            cut.WaitForAssertion(() => Assert.Contains("Verse 1 text.", cut.Markup),
                TimeSpan.FromSeconds(5));
            return cut;
        }

        /// <summary>
        /// #37: clicking verse rows on the Read page selects them, and the
        /// selection fills to a contiguous range like the editor (#24).
        /// </summary>
        [Fact]
        public void ReadVerse_ClickTogglesSelection_FillsRange()
        {
            SetupVersionResponse(1, 1);

            var cut = RenderReadVerses();

            cut.Find("#read-verse-1").Click();
            cut.Find("#read-verse-5").Click();

            Assert.Contains("Selected: 1-5 (5 verses)", cut.Markup);
        }

        /// <summary>
        /// #37: selecting verses, opening the playlist picker, and choosing a
        /// playlist navigates to the editor in Create mode with the selection
        /// and playlist pre-filled.
        /// </summary>
        [Fact]
        public void ReadVerse_AddToPlaylist_NavigatesToEditorCreateMode()
        {
            SetupVersionResponse(1, 1);
            SetupLibraryResponse();

            var cut = RenderReadVerses();

            cut.Find("#read-verse-1").Click();
            cut.Find("#read-verse-3").Click();

            var addButton = cut.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Add to playlist"));
            Assert.NotNull(addButton);
            addButton!.Click();

            cut.WaitForAssertion(() =>
            {
                var playlistButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("Christmas Story"));
                Assert.NotNull(playlistButton);
            }, TimeSpan.FromSeconds(5));

            var christmasButton = cut.FindAll("button")
                .First(b => b.TextContent.Contains("Christmas Story"));
            christmasButton.Click();

            var uri = Services.GetRequiredService<NavigationManager>().Uri;
            Assert.Contains("/select-verses?", uri);
            Assert.Contains("library=library_greg_hardin_live_com", uri);
            Assert.Contains("book=1", uri);
            Assert.Contains("chapter=1", uri);
            Assert.Contains("playlist=christmas-story", uri);
            Assert.Contains("selectStart=1", uri);
            Assert.Contains("selectEnd=3", uri);
            Assert.DoesNotContain("segmentId", uri);
        }

        /// <summary>
        /// #37: changing chapter clears the selection — a selection belongs
        /// to the chapter it was made in.
        /// </summary>
        [Fact]
        public async Task ReadVerse_ChapterChange_ClearsSelection()
        {
            SetupVersionResponse(1, 1);
            SetupVersionResponse(2, 1);

            var cut = RenderReadVerses();

            cut.Find("#read-verse-2").Click();
            Assert.Contains("Selected: 2-2 (1 verses)", cut.Markup);

            await cut.InvokeAsync(() => cut.Instance.ReloadChapter(
                new SelectedBookChapter { SelectedBook = 2, SelectedChapter = 1 }));

            cut.WaitForAssertion(() => Assert.DoesNotContain("Selected:", cut.Markup),
                TimeSpan.FromSeconds(5));
        }

        // #46: a library with no playlists at all.
        private void SetupEmptyLibraryResponse()
        {
            var library = new Library
            {
                Code = "library_greg_hardin_live_com",
                Playlists = new List<Playlist>()
            };

            HttpHandler.SetupGet("library?key=library_greg_hardin_live_com", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPut("library");
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

        // #46: select verses 1-3 and open the playlist picker, waiting until
        // the "New playlist..." option has rendered (the library loads async).
        private IRenderedComponent<BiblePlaylist.Client.Shared.ReadVerses> RenderWithPickerOpen()
        {
            SetupVersionResponse(1, 1);
            SetupLibraryResponse();

            var cut = RenderReadVerses();

            cut.Find("#read-verse-1").Click();
            cut.Find("#read-verse-3").Click();

            var addButton = cut.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Add to playlist"));
            Assert.NotNull(addButton);
            addButton!.Click();

            cut.WaitForAssertion(() =>
            {
                var newButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("New playlist"));
                Assert.NotNull(newButton);
            }, TimeSpan.FromSeconds(5));

            return cut;
        }

        private static void ClickNewPlaylist(
            IRenderedComponent<BiblePlaylist.Client.Shared.ReadVerses> cut)
        {
            cut.FindAll("button").First(b => b.TextContent.Contains("New playlist")).Click();
        }

        private static void SetNewPlaylistFields(
            IRenderedComponent<BiblePlaylist.Client.Shared.ReadVerses> cut, string name, string? description)
        {
            // bUnit's RefreshableElementCollection indexer throws
            // MissingMethodException — use LINQ, never [index].
            var inputs = cut.FindAll("input");
            inputs.First().Input(name);
            if (description != null)
                inputs.Skip(1).First().Input(description);
        }

        private static void ClickCreatePlaylist(
            IRenderedComponent<BiblePlaylist.Client.Shared.ReadVerses> cut)
        {
            cut.FindAll("button").First(b => b.TextContent.Trim() == "Create").Click();
        }

        /// <summary>
        /// #46: from the Read picker, "New playlist..." shows the inline
        /// form; Create saves the new playlist immediately (PUT) and opens
        /// the editor in Create mode with the selected verses pre-filled.
        /// </summary>
        [Fact]
        public async Task ReadVerse_NewPlaylist_CreatesSavesAndNavigatesToEditor()
        {
            var cut = RenderWithPickerOpen();

            ClickNewPlaylist(cut);
            SetNewPlaylistFields(cut, "Romans Road", "A gospel presentation");
            ClickCreatePlaylist(cut);

            cut.WaitForAssertion(() => Assert.Contains(HttpHandler.Requests, req => req.Method == HttpMethod.Put), TimeSpan.FromSeconds(5));
            var saved = await GetSavedLibrary();
            Assert.NotNull(saved);
            var added = Assert.Single(saved!.Playlists, p => p.Name == "Romans Road");
            Assert.False(string.IsNullOrWhiteSpace(added.Key));
            Assert.Equal("A gospel presentation", added.Description);
            Assert.NotNull(added.BookChapters);
            Assert.Empty(added.BookChapters);

            var uri = Services.GetRequiredService<NavigationManager>().Uri;
            Assert.Contains("/select-verses?", uri);
            Assert.Contains("library=library_greg_hardin_live_com", uri);
            Assert.Contains("book=1", uri);
            Assert.Contains("chapter=1", uri);
            Assert.Contains($"playlist={added.Key}", uri);
            Assert.Contains("selectStart=1", uri);
            Assert.Contains("selectEnd=3", uri);
            Assert.DoesNotContain("segmentId", uri);
        }

        /// <summary>
        /// #46: the "New playlist..." option appears in the Read picker even
        /// when there are no playlists at all, so a first-time user can
        /// create one from Read.
        /// </summary>
        [Fact]
        public void ReadVerse_NewPlaylist_AppearsWithZeroPlaylists()
        {
            SetupVersionResponse(1, 1);
            SetupEmptyLibraryResponse();

            var cut = RenderReadVerses();

            cut.Find("#read-verse-1").Click();

            var addButton = cut.FindAll("button")
                .FirstOrDefault(b => b.TextContent.Contains("Add to playlist"));
            Assert.NotNull(addButton);
            addButton!.Click();

            cut.WaitForAssertion(() =>
            {
                Assert.Contains("No playlists found.", cut.Markup);
                var newButton = cut.FindAll("button")
                    .FirstOrDefault(b => b.TextContent.Contains("New playlist"));
                Assert.NotNull(newButton);
            }, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// #46: an empty name is rejected inline with a clear message and
        /// nothing is saved or navigated to.
        /// </summary>
        [Fact]
        public void ReadVerse_NewPlaylist_EmptyName_Rejected()
        {
            var cut = RenderWithPickerOpen();

            ClickNewPlaylist(cut);
            ClickCreatePlaylist(cut);

            Assert.Contains("Name is required.", cut.Markup);
            Assert.DoesNotContain(HttpHandler.Requests, req => req.Method == HttpMethod.Put);
            Assert.DoesNotContain("/select-verses", Services.GetRequiredService<NavigationManager>().Uri);
        }

        /// <summary>
        /// #46: a duplicate name (case-insensitive) is rejected inline with
        /// a clear message and nothing is saved or navigated to.
        /// </summary>
        [Fact]
        public void ReadVerse_NewPlaylist_DuplicateName_Rejected()
        {
            var cut = RenderWithPickerOpen();

            ClickNewPlaylist(cut);
            SetNewPlaylistFields(cut, "christmas story", null);
            ClickCreatePlaylist(cut);

            Assert.Contains("A playlist named 'christmas story' already exists.", cut.Markup);
            Assert.DoesNotContain(HttpHandler.Requests, req => req.Method == HttpMethod.Put);
            Assert.DoesNotContain("/select-verses", Services.GetRequiredService<NavigationManager>().Uri);
        }
    }
}
