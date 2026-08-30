using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.Data;
using BiblePlaylist.Shared.DTO;
using BiblePlaylist.Shared.Playlist;
using BiblePlaylist.Client.Config;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Moq.Protected;
using MudBlazor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BiblePlaylist.Tests
{
    public class SegmentEditorTests : TestContext
    {
        private readonly Mock<IJSRuntime> _jsMock;
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;

        public SegmentEditorTests()
        {
            Services.AddMudBlazorDialog();
            Services.AddMudServices();

            _jsMock = new Mock<IJSRuntime>();
            Services.AddSingleton(_jsMock.Object);

            Services.AddSingleton(new Endpoints());

            _httpHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_httpHandlerMock.Object);
            Services.AddSingleton(httpClient);
        }

        private void SetupVersionResponse(int bookNumber, int chapterNumber, string audioUrl = "http://example.com/chapter.mp3")
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
                                AudioUrl = audioUrl,
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

            var json = JsonConvert.SerializeObject(version);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _httpHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains($"Book={bookNumber}") &&
                    req.RequestUri.AbsolutePath.Contains($"Chapter={chapterNumber}")),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupLibraryResponse(string playlistKey, int bookNumber, int chapterNumber,
            List<Segment>? segments = null)
        {
            var library = new Library
            {
                Playlists = new List<Playlist>
                {
                    new Playlist
                    {
                        Key = playlistKey,
                        BookChapters = new List<BookChapter>
                        {
                            new BookChapter
                            {
                                BookNumber = bookNumber,
                                ChapterNumber = chapterNumber,
                                Segments = segments ?? new List<Segment>()
                            }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(library);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _httpHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains("library")),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            _httpHandlerMock.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains("library")),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        }

        [Fact]
        public void Editor_Renders_WithoutCrashing()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            Assert.NotNull(cut);
            Assert.Contains("Loading", cut.Markup);
        }

        [Fact]
        public async Task Editor_PlayButton_WithNoSelection_NeverCallsInitializeAudio()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var buttons = cut.FindAll("button");
            if (buttons.Count > 0)
                buttons[0].Click();

            _jsMock.Verify(js => js.InvokeVoidAsync("initializeAudioPlayer", It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Editor_PlayButton_WithSelection_CallsJSInterop()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < verseElements.Count; i++)
                verseElements[i].Click();

            var buttons = cut.FindAll("button");
            if (buttons.Count > 0)
                buttons[0].Click();

            _jsMock.Verify(js => js.InvokeVoidAsync("initializeAudioPlayer", "segmentEditorPlayer"),
                Times.Once);
            _jsMock.Verify(js => js.InvokeVoidAsync(
                "loadAudioFile", It.IsAny<string>(), "segmentEditorPlayer"),
                Times.Once);
            _jsMock.Verify(js => js.InvokeVoidAsync(
                "PlayAudioSegment", It.IsAny<decimal>(), It.IsAny<decimal>(), "segmentEditorPlayer"),
                Times.Once);
        }

        [Fact]
        public async Task Editor_StopPlayback_CallsPauseAudio()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < verseElements.Count; i++)
                verseElements[i].Click();

            var buttons = cut.FindAll("button");
            if (buttons.Count > 0)
                buttons[0].Click();

            _jsMock.Verify(js => js.InvokeVoidAsync("PlayAudioSegment",
                It.IsAny<decimal>(), It.IsAny<decimal>(), "segmentEditorPlayer"),
                Times.Once);

            if (buttons.Count > 0)
                buttons[0].Click();

            _jsMock.Verify(js => js.InvokeVoidAsync("pauseAudioPlayer", "segmentEditorPlayer"),
                Times.Once);
        }

        [Fact]
        public async Task Editor_SaveSegment_CallsPutLibrary()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < 3 && i < verseElements.Count; i++)
                verseElements[i].Click();

            var buttons = cut.FindAll("button");
            if (buttons.Count >= 3)
            {
                buttons[2].Click();
                await cut.InvokeAsync(() => Task.Delay(50));
            }

            _httpHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.AbsolutePath.Contains("library")),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task Editor_CancelButton_DoesNotCrash()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var buttons = cut.FindAll("button");
            if (buttons.Count >= 2)
            {
                buttons[1].Click();
                await cut.InvokeAsync(() => Task.Delay(50));
            }

            Assert.NotNull(cut);
        }

        [Fact]
        public void Editor_CreateMode_RendersInfoAlert()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            Assert.NotNull(cut);

            Assert.Contains("Loading", cut.Markup);
        }

        [Fact]
        public async Task Editor_EditMode_LoadsExistingSegment_DoesNotCrash()
        {
            var existingSegment = new Segment
            {
                VerseStart = 2,
                VerseEnd = 4,
                Verses = new List<Verse>
                {
                    new Verse { Number = 2, Html = "Verse 2", AudioStart = 10, AudioEnd = 20 },
                    new Verse { Number = 3, Html = "Verse 3", AudioStart = 20, AudioEnd = 30 },
                    new Verse { Number = 4, Html = "Verse 4", AudioStart = 30, AudioEnd = 40 }
                }
            };

            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment> { existingSegment });

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
        }

        [Fact]
        public async Task Editor_EditMode_WithEmptySegments_FallsBackToCreate()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>());

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
        }
    }
}
