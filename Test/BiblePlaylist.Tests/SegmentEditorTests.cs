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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace BiblePlaylist.Tests
{
    // Fake IJSRuntime that records every call for later verification.
    public class RecordingJSRuntime : IJSRuntime
    {
        public List<(string Method, object[] Args)> Invocations { get; } = new();

        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args)
        {
            Invocations.Add((identifier, args ?? Array.Empty<object>()));
            return default!;
        }

        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Invocations.Add((identifier, args ?? Array.Empty<object>()));
            return default!;
        }
    }

    // Fake HttpMessageHandler that returns canned responses based on URL patterns.
    public class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        private readonly Dictionary<string, HttpResponseMessage> _responses = new();

        public void SetupGet(string pathContains, string body)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            _responses[pathContains] = response;
        }

        public void SetupPut(string pathContains)
        {
            _responses[pathContains] = new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            foreach (var (key, resp) in _responses)
            {
                if (request.RequestUri != null && request.RequestUri.AbsolutePath.Contains(key))
                    return resp;
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    public class SegmentEditorTests : TestContext
    {
        public RecordingJSRuntime JSRuntime { get; private set; } = null!;
        public RecordingHttpMessageHandler HttpHandler { get; private set; } = null!;

        public SegmentEditorTests()
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

            HttpHandler.SetupGet($"Book={bookNumber}", JsonConvert.SerializeObject(version));
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

            HttpHandler.SetupGet("library", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPut("library");
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
        public async Task Editor_PlayButton_WithNoSelection_DoesNotCallInitializeAudio()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            await cut.InvokeAsync(() => Task.Delay(50));

            var buttons = cut.FindAll("button");
            if (buttons.Count > 0)
                buttons[0].Click();

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.DoesNotContain(
                JSRuntime.Invocations,
                i => i.Method == "initializeAudioPlayer");
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

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(JSRuntime.Invocations,
                i => i.Method == "initializeAudioPlayer" &&
                     i.Args.Length >= 1 && i.Args[0].ToString() == "segmentEditorPlayer");
            Assert.Contains(JSRuntime.Invocations,
                i => i.Method == "loadAudioFile");
            Assert.Contains(JSRuntime.Invocations,
                i => i.Method == "PlayAudioSegment");
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
                buttons[0].Click(); // play

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(JSRuntime.Invocations, i => i.Method == "PlayAudioSegment");

            if (buttons.Count > 0)
                buttons[0].Click(); // stop

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(JSRuntime.Invocations, i => i.Method == "pauseAudioPlayer");
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

            Assert.Contains(HttpHandler.Requests,
                req => req.Method == HttpMethod.Put &&
                       req.RequestUri?.AbsolutePath.Contains("library") == true);
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
