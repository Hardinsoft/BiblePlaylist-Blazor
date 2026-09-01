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
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace BiblePlaylist.Tests
{
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

    public class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        private readonly Dictionary<string, HttpResponseMessage> _getResponses = new();
        private readonly Dictionary<string, HttpResponseMessage> _putResponses = new();

        public void SetupGet(string uriContains, string body)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            _getResponses[uriContains] = response;
        }

        public void SetupPut(string uriContains)
        {
            _putResponses[uriContains] = new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (request.Method == HttpMethod.Get)
            {
                foreach (var (key, resp) in _getResponses)
                    if (uri.Contains(key)) return resp;
            }
            else if (request.Method == HttpMethod.Put)
            {
                foreach (var (key, resp) in _putResponses)
                    if (uri.Contains(key)) return resp;
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

            HttpHandler.SetupGet($"library?key={playlistKey}", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPut("library");
        }

        private IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> RenderEditor(int book, int chapter,
            string playlist = null, int? segmentStart = null, int? segmentEnd = null)
        {
            var ps = new List<ComponentParameter>
            {
                ComponentParameter.CreateParameter("Book", book),
                ComponentParameter.CreateParameter("Chapter", chapter)
            };
            if (playlist != null)
                ps.Add(ComponentParameter.CreateParameter("Playlist", playlist));
            if (segmentStart.HasValue && segmentEnd.HasValue)
            {
                ps.Add(ComponentParameter.CreateParameter("SegmentStart", segmentStart.Value));
                ps.Add(ComponentParameter.CreateParameter("SegmentEnd", segmentEnd.Value));
            }
            return RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>(ps.ToArray());
        }

        // Use reflection to set selection on the component instance — avoids Bunit 1.40.0
        // broken RefreshableElementCollection indexer entirely.
        private void SetSelectedVerses(IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> cut,
            IEnumerable<int> verseNumbers)
        {
            var instance = cut.Instance;
            var field = typeof(BiblePlaylist.Client.Pages.SegmentEditor)
                .GetField("_selectedVerseNumbers", BindingFlags.NonPublic | BindingFlags.Instance)!;
            field.SetValue(instance, new HashSet<int>(verseNumbers));
            cut.InvokeAsync(() => cut.Instance.GetType().GetMethod("StateHasChanged", BindingFlags.Public | BindingFlags.Instance)!.Invoke(cut.Instance, null));
        }

        [Fact]
        public void Editor_Renders_WithoutCrashing()
        {
            SetupVersionResponse(1, 5);
            var cut = RenderEditor(1, 5);
            Assert.NotNull(cut);
            Assert.Contains("Verse 1 text.", cut.Markup);
        }

        [Fact]
        public async Task Editor_PlayButton_WithNoSelection_DoesNotCallInitializeAudio()
        {
            SetupVersionResponse(1, 5);
            var cut = RenderEditor(1, 5);
            await cut.InvokeAsync(() => Task.Delay(50));

            await cut.InvokeAsync(() =>
            {
                var firstButton = cut.Find("button");
                if (firstButton != null)
                    firstButton.Click();
            });

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

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            // Set selection via reflection — selects all 5 verses
            SetSelectedVerses(cut, new[] { 1, 2, 3, 4, 5 });

            await cut.InvokeAsync(() =>
            {
                var playButton = cut.Find("button");
                if (playButton != null)
                    playButton.Click();
            });

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

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            // Select all 5 verses via reflection
            SetSelectedVerses(cut, new[] { 1, 2, 3, 4, 5 });

            await cut.InvokeAsync(() =>
            {
                var playButton = cut.Find("button");
                if (playButton != null)
                    playButton.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(JSRuntime.Invocations, i => i.Method == "PlayAudioSegment");

            await cut.InvokeAsync(() =>
            {
                var playButton = cut.Find("button");
                if (playButton != null)
                    playButton.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(JSRuntime.Invocations, i => i.Method == "pauseAudioPlayer");
        }

        [Fact]
        public async Task Editor_SaveSegment_CallsPutLibrary()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            // Select first 3 verses via reflection
            SetSelectedVerses(cut, new[] { 1, 2, 3 });

            await cut.InvokeAsync(() =>
            {
                var saveButton = cut.FindAll("button").LastOrDefault(b => b.TextContent.Contains("Save"));
                if (saveButton != null)
                    saveButton.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains(HttpHandler.Requests,
                req => req.Method == HttpMethod.Put &&
                       req.RequestUri?.AbsoluteUri.Contains("library") == true);
        }

        [Fact]
        public async Task Editor_CancelButton_DoesNotCrash()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderEditor(1, 5);
            await cut.InvokeAsync(() => Task.Delay(50));

            await cut.InvokeAsync(() =>
            {
                var cancelButton = cut.FindAll("button").LastOrDefault(b => b.TextContent.Contains("Cancel"));
                if (cancelButton != null)
                    cancelButton.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
        }

        [Fact]
        public void Editor_CreateMode_RendersInfoAlert()
        {
            SetupVersionResponse(1, 5);
            var cut = RenderEditor(1, 5);
            Assert.NotNull(cut);
            Assert.Contains("Select verses to create a new segment", cut.Markup);
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

            var cut = RenderEditor(1, 5, "test-playlist", 2, 4);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
            Assert.Contains("2-4 (3 verses)", cut.Markup);
            Assert.Contains("Verse 2 text.", cut.Markup);
            Assert.Contains("Verse 3 text.", cut.Markup);
            Assert.Contains("Verse 4 text.", cut.Markup);
        }

        [Fact]
        public async Task Editor_EditMode_WithEmptySegments_FallsBackToCreate()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>());

            var cut = RenderEditor(1, 5, "test-playlist", 2, 4);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
            Assert.Contains("Select verses to create a new segment", cut.Markup);
        }
    }
}
