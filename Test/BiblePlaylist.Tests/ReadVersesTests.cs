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
    /// SegmentEditorTests (same assembly and namespace).
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
            Assert.Contains("/edit-segment?", uri);
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
    }
}
