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
        public List<(string Method, object?[] Args)> Invocations { get; } = new();

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
        private readonly Dictionary<string, string> _getResponses = new();
        private readonly Dictionary<string, string> _postResponses = new();
        private readonly HashSet<string> _putResponses = new();

        public void SetupGet(string uriContains, string body)
        {
            // Store the body, not the response: HttpClient disposes each
            // response after reading it, so a shared instance breaks on the
            // second GET of the same URI — Edit mode loads the library, then
            // saves it with a fresh GET.
            _getResponses[uriContains] = body;
        }

        public void SetupPut(string uriContains)
        {
            _putResponses.Add(uriContains);
        }

        public void SetupPost(string uriContains, string body)
        {
            _postResponses[uriContains] = body;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var uri = request.RequestUri?.AbsoluteUri ?? "";

            if (request.Method == HttpMethod.Get)
            {
                foreach (var (key, body) in _getResponses)
                    if (uri.Contains(key))
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(body, Encoding.UTF8, "application/json")
                        };
            }
            else if (request.Method == HttpMethod.Put)
            {
                foreach (var key in _putResponses)
                    if (uri.Contains(key)) return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else if (request.Method == HttpMethod.Post)
            {
                foreach (var (key, body) in _postResponses)
                    if (uri.Contains(key))
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(body, Encoding.UTF8, "application/json")
                        };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }

    public class SegmentEditorTests : Bunit.TestContext
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
            List<Segment>? segments = null, string libraryCode = "test-library")
        {
            var library = new Library
            {
                Code = libraryCode,
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

            HttpHandler.SetupGet($"library?key={libraryCode}", JsonConvert.SerializeObject(library));
            HttpHandler.SetupPut("library");
        }

        private IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> RenderEditor(int book, int chapter,
            string? playlist = null, int? segmentId = null, int? selectStart = null, int? selectEnd = null, string libraryCode = "test-library")
        {
            var ps = new List<ComponentParameter>
            {
                ComponentParameter.CreateParameter("Book", book),
                ComponentParameter.CreateParameter("Chapter", chapter)
            };
            if (playlist != null)
            {
                ps.Add(ComponentParameter.CreateParameter("Playlist", playlist));
                ps.Add(ComponentParameter.CreateParameter("LibraryCode", libraryCode));
            }
            if (segmentId.HasValue)
            {
                ps.Add(ComponentParameter.CreateParameter("SegmentId", segmentId.Value));
            }
            if (selectStart.HasValue && selectEnd.HasValue)
            {
                ps.Add(ComponentParameter.CreateParameter("SelectStart", selectStart.Value));
                ps.Add(ComponentParameter.CreateParameter("SelectEnd", selectEnd.Value));
            }
            return RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>(ps.ToArray());
        }

        // Use reflection to set selection on the component instance — avoids Bunit 1.40.0
        // broken RefreshableElementCollection indexer entirely.
        // After mutating the field, flush an empty InvokeAsync to force a render cycle so
        // the DOM picks up the new selection state.
        private void SetSelectedVerses(IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> cut,
            IEnumerable<int> verseNumbers)
        {
            var type = typeof(BiblePlaylist.Client.Pages.SegmentEditor);
            var field = type.GetField("_selectedVerseNumbers", BindingFlags.NonPublic | BindingFlags.Instance);
            var instance = cut.Instance;
            if (field == null || instance == null) return;

            field.SetValue(instance, new HashSet<int>(verseNumbers));
            _ = cut.InvokeAsync(() => { });
        }

        // Read the private selection field — the ToggleVerseAsync path is the only
        // way the UI changes it, so tests below exercise the real click handler.
        private HashSet<int> GetSelectedVerses(IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> cut)
        {
            var field = typeof(BiblePlaylist.Client.Pages.SegmentEditor)
                .GetField("_selectedVerseNumbers", BindingFlags.NonPublic | BindingFlags.Instance);
            return (field?.GetValue(cut.Instance) as HashSet<int>) ?? new HashSet<int>();
        }

        private void ClickVerseRow(IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> cut, int verseNumber)
        {
            var row = cut.FindAll("div.d-flex.align-start")
                .FirstOrDefault(d => d.TextContent.Contains($"Verse {verseNumber} text."));
            Assert.NotNull(row);
            row!.Click();
        }

        /// <summary>
        /// #24: toggling verses 1 then 5 must visibly select the whole 1-5 range,
        /// so the displayed selection is exactly what SaveSegment will store.
        /// </summary>
        [Fact]
        public async Task Editor_Toggle_NonContiguous_FillsRange()
        {
            SetupVersionResponse(1, 5);
            var cut = RenderEditor(1, 5);
            await cut.InvokeAsync(() => Task.Delay(50));

            await cut.InvokeAsync(() => ClickVerseRow(cut, 1));
            await cut.InvokeAsync(() => ClickVerseRow(cut, 5));
            await cut.InvokeAsync(() => Task.Delay(50));

            cut.WaitForAssertion(() =>
            {
                Assert.Equal(new HashSet<int> { 1, 2, 3, 4, 5 }, GetSelectedVerses(cut));
                Assert.Contains("Segment: 1-5 (5 verses)", cut.Markup);
            });
        }

        /// <summary>
        /// #24: after toggling 1 then 5, the saved segment must be 1-5 — the
        /// displayed range, not a silent rewrite of a non-contiguous selection.
        /// </summary>
        [Fact]
        public async Task Editor_Toggle_NonContiguous_SavePersistsDisplayedRange()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            await cut.InvokeAsync(() => ClickVerseRow(cut, 1));
            await cut.InvokeAsync(() => ClickVerseRow(cut, 5));
            await cut.InvokeAsync(() => Task.Delay(50));

            await cut.InvokeAsync(() =>
            {
                var saveButton = cut.FindAll("button").LastOrDefault(b => b.TextContent.Contains("Save"));
                saveButton?.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            var putRequest = HttpHandler.Requests.FirstOrDefault(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri?.AbsoluteUri.Contains("library") == true);
            Assert.NotNull(putRequest);

            var putBody = await putRequest!.Content!.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
            var savedLibrary = JsonConvert.DeserializeObject<Library>(putBody);
            var savedSegment = savedLibrary?.Playlists?.FirstOrDefault()?.BookChapters?
                .FirstOrDefault()?.Segments?.FirstOrDefault();
            Assert.NotNull(savedSegment);
            Assert.Equal(1, savedSegment!.VerseStart);
            Assert.Equal(5, savedSegment.VerseEnd);
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
                     i.Args.Length >= 1 && i.Args[0]?.ToString() == "segmentEditorAudioPlayer");
            Assert.Contains(JSRuntime.Invocations,
                i => i.Method == "loadAudioFile" &&
                     i.Args.Length >= 2 && i.Args[0]?.ToString() == "segmentEditorAudioPlayer" &&
                     i.Args[1]?.ToString() == "segmentEditorAudioPlayerSource");
            Assert.Contains(JSRuntime.Invocations,
                i => i.Method == "PlayAudioSegment" &&
                     i.Args.Length >= 2 && i.Args[0]?.ToString() == "segmentEditorAudioPlayer" &&
                     i.Args[1]?.ToString() == "segmentEditorAudioPlayerSource");
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

            // No Id on the segment: retrofit assigns Id 1 at load.
            var cut = RenderEditor(1, 5, "test-playlist", 1);
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

            var cut = RenderEditor(1, 5, "test-playlist", 2);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.NotNull(cut);
            Assert.Contains("Select verses to create a new segment", cut.Markup);
        }

        private async Task ClickSave(IRenderedComponent<BiblePlaylist.Client.Pages.SegmentEditor> cut)
        {
            await cut.InvokeAsync(() =>
            {
                var saveButton = cut.FindAll("button").LastOrDefault(b => b.TextContent.Contains("Save"));
                saveButton?.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));
        }

        private async Task<List<Segment>?> GetSavedSegments()
        {
            var putRequest = HttpHandler.Requests.FirstOrDefault(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri?.AbsoluteUri.Contains("library") == true);
            if (putRequest?.Content == null) return null;

            var putBody = await putRequest.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
            var savedLibrary = JsonConvert.DeserializeObject<Library>(putBody);
            return savedLibrary?.Playlists?.FirstOrDefault()?.BookChapters?
                .FirstOrDefault()?.Segments?.ToList();
        }

        /// <summary>
        /// #25: editing A (Id 1, 2-4) to exactly B's range (1-5) must replace
        /// A in place, keeping its Id. Overlap between segments is allowed,
        /// so B is untouched. The old range-based lookup overwrote B.
        /// </summary>
        [Fact]
        public async Task Editor_EditMode_SaveToExistingRange_ReplacesOriginalNotOther()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 2, VerseEnd = 4 },
                new Segment { Id = 2, VerseStart = 1, VerseEnd = 5 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist", 1);
            await cut.InvokeAsync(() => Task.Delay(50));

            SetSelectedVerses(cut, new[] { 1, 2, 3, 4, 5 });
            await ClickSave(cut);

            var saved = await GetSavedSegments();
            Assert.NotNull(saved);
            Assert.Equal(2, saved!.Count);
            Assert.Equal(1, saved[0].Id);
            Assert.Equal(1, saved[0].VerseStart);
            Assert.Equal(5, saved[0].VerseEnd);
            Assert.Equal(2, saved[1].Id);
            Assert.Equal(1, saved[1].VerseStart);
            Assert.Equal(5, saved[1].VerseEnd);
        }

        /// <summary>
        /// #25: editing with a changed range and no collision applies in place,
        /// keeping the segment's Id.
        /// </summary>
        [Fact]
        public async Task Editor_EditMode_SaveChangedRange_AppliesInPlace()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 2, VerseEnd = 4 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist", 1);
            await cut.InvokeAsync(() => Task.Delay(50));

            SetSelectedVerses(cut, new[] { 1, 2, 3 });
            await ClickSave(cut);

            var saved = await GetSavedSegments();
            Assert.NotNull(saved);
            Assert.Single(saved!);
            Assert.Equal(1, saved[0].Id);
            Assert.Equal(1, saved[0].VerseStart);
            Assert.Equal(3, saved[0].VerseEnd);
        }

        /// <summary>
        /// #25: when the segment being edited is gone by save time (stale Id),
        /// the save must fail without writing anything — no PUT, nothing added.
        /// </summary>
        [Fact]
        public async Task Editor_EditMode_StaleSegmentId_DoesNotSave()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 2, VerseEnd = 4 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist", 1);
            await cut.InvokeAsync(() => Task.Delay(50));

            // The segment is deleted before saving: the next library GET
            // returns a chapter with no segments.
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>());

            SetSelectedVerses(cut, new[] { 1, 2 });
            await ClickSave(cut);

            Assert.DoesNotContain(HttpHandler.Requests,
                req => req.Method == HttpMethod.Put &&
                       req.RequestUri?.AbsoluteUri.Contains("library") == true);
        }

        /// <summary>
        /// #25: an edit that keeps the same range still applies as before,
        /// keeping the segment's Id.
        /// </summary>
        [Fact]
        public async Task Editor_EditMode_UnchangedRange_AppliesAsBefore()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 2, VerseEnd = 4 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist", 1);
            await cut.InvokeAsync(() => Task.Delay(50));

            SetSelectedVerses(cut, new[] { 2, 3, 4 });
            await ClickSave(cut);

            var saved = await GetSavedSegments();
            Assert.NotNull(saved);
            Assert.Single(saved!);
            Assert.Equal(1, saved[0].Id);
            Assert.Equal(2, saved[0].VerseStart);
            Assert.Equal(4, saved[0].VerseEnd);
        }

        /// <summary>
        /// #25: creating a segment assigns the next Id in the chapter
        /// (max existing Id + 1); existing Ids are untouched.
        /// </summary>
        [Fact]
        public async Task Editor_CreateMode_SaveAssignsNextSegmentId()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 2, VerseEnd = 4 },
                new Segment { Id = 2, VerseStart = 1, VerseEnd = 5 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            SetSelectedVerses(cut, new[] { 1, 2, 3 });
            await ClickSave(cut);

            var saved = await GetSavedSegments();
            Assert.NotNull(saved);
            Assert.Equal(3, saved!.Count);
            Assert.Equal(1, saved[0].Id);
            Assert.Equal(2, saved[1].Id);
            Assert.Equal(3, saved[2].Id);
            Assert.Equal(1, saved[2].VerseStart);
            Assert.Equal(3, saved[2].VerseEnd);
        }

        /// <summary>
        /// #25: segments saved before Id existed (Id == 0) are retrofitted
        /// deterministically on load — in list order, max + 1 — and the Ids
        /// persist in the saved library.
        /// </summary>
        [Fact]
        public async Task Editor_EditMode_RetrofitsMissingSegmentIds()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>
            {
                new Segment { VerseStart = 2, VerseEnd = 4 },
                new Segment { VerseStart = 1, VerseEnd = 5 }
            });
            HttpHandler.SetupPut("library");

            // The first segment has no Id yet; retrofit assigns it Id 1.
            var cut = RenderEditor(1, 5, "test-playlist", 1);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains("2-4 (3 verses)", cut.Markup);

            SetSelectedVerses(cut, new[] { 1, 2, 3 });
            await ClickSave(cut);

            var saved = await GetSavedSegments();
            Assert.NotNull(saved);
            Assert.Equal(2, saved!.Count);
            Assert.Equal(1, saved[0].Id);
            Assert.Equal(1, saved[0].VerseStart);
            Assert.Equal(3, saved[0].VerseEnd);
            Assert.Equal(2, saved[1].Id);
            Assert.Equal(1, saved[1].VerseStart);
            Assert.Equal(5, saved[1].VerseEnd);
        }

        private async Task<Library?> GetSavedLibrary()
        {
            var putRequest = HttpHandler.Requests.FirstOrDefault(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri?.AbsoluteUri.Contains("library") == true);
            if (putRequest?.Content == null) return null;

            var putBody = await putRequest.Content.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
            return JsonConvert.DeserializeObject<Library>(putBody);
        }

        /// <summary>
        /// #37: a pre-selected range from the Read page (selectStart/selectEnd)
        /// fills the editor's selection in Create mode.
        /// </summary>
        [Fact]
        public async Task Editor_CreateMode_Preselection_FillsSelection()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderEditor(1, 5, "test-playlist", selectStart: 1, selectEnd: 3);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains("1-3 (3 verses)", cut.Markup);
        }

        /// <summary>
        /// #37: a pre-selected range outside the chapter leaves the selection
        /// empty — the editor behaves as if nothing was pre-selected.
        /// </summary>
        [Fact]
        public async Task Editor_CreateMode_PreselectionOutOfRange_LeavesSelectionEmpty()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderEditor(1, 5, "test-playlist", selectStart: 9, selectEnd: 12);
            await cut.InvokeAsync(() => Task.Delay(50));

            Assert.Contains("Select verses to create a new segment", cut.Markup);
        }

        /// <summary>
        /// #37: saving a segment for a chapter that is not in the playlist
        /// creates the chapter (appended at the end) and the segment in it,
        /// with Id 1; the existing chapter is untouched.
        /// </summary>
        [Fact]
        public async Task Editor_CreateMode_SaveNewChapter_CreatesBookChapter()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 1, new List<Segment>
            {
                new Segment { Id = 1, VerseStart = 1, VerseEnd = 2 }
            });
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            SetSelectedVerses(cut, new[] { 1, 2, 3 });
            await ClickSave(cut);

            var savedLibrary = await GetSavedLibrary();
            Assert.NotNull(savedLibrary);
            var chapters = savedLibrary!.Playlists?.FirstOrDefault()?.BookChapters?.ToList();
            Assert.NotNull(chapters);
            Assert.Equal(2, chapters!.Count);

            // Existing chapter untouched.
            Assert.Equal(1, chapters[0].BookNumber);
            Assert.Equal(1, chapters[0].ChapterNumber);
            Assert.Single(chapters[0].Segments!);
            Assert.Equal(1, chapters[0].Segments![0].Id);

            // New chapter appended, built from the loaded chapter data.
            var newChapter = chapters[1];
            Assert.Equal(1, newChapter.BookNumber);
            Assert.Equal(5, newChapter.ChapterNumber);
            Assert.Equal("Test Book", newChapter.BookName);
            Assert.Equal("http://example.com/chapter.mp3", newChapter.ChapterAudioUrl);
            Assert.Equal("NET", newChapter.VersionCode);
            Assert.Single(newChapter.Segments!);
            Assert.Equal(1, newChapter.Segments![0].Id);
            Assert.Equal(1, newChapter.Segments[0].VerseStart);
            Assert.Equal(3, newChapter.Segments[0].VerseEnd);
        }

        /// <summary>
        /// The server's SaveEntityAsync uses Newtonsoft.Json.JsonConvert.SerializeObject.
        /// Segment.Verses must be ignored by Newtonsoft so verse data (Html, AudioStart,
        /// AudioEnd) is never persisted to the library JSON file.
        /// </summary>
        [Fact]
        public void Segment_Verses_NotSerialized_ByNewtonsoft()
        {
            var segment = new Segment
            {
                VerseStart = 1,
                VerseEnd = 9,
                Verses = new List<Verse>
                {
                    new Verse { Number = 1, Html = "In the beginning...", AudioStart = 0, AudioEnd = 10.5m },
                    new Verse { Number = 2, Html = "The same word...", AudioStart = 10.5m, AudioEnd = 20.0m }
                }
            };

            string json = JsonConvert.SerializeObject(segment);

            Assert.DoesNotContain("Verses", json);
            Assert.DoesNotContain("In the beginning", json);
            Assert.Contains("VerseStart", json);
            Assert.Contains("VerseEnd", json);
        }

        /// <summary>
        /// A Library containing segments with populated Verses (as happens after
        /// the /playlist/inflate endpoint enriches chapters at runtime) must not
        /// have that verse data in the JSON when saved via the server's Newtonsoft
        /// serializer. This is the core of the drag-drop-save bug: previously all
        /// verse text and audio timing leaked into library_greg_hardin_live_com.json.
        /// </summary>
        [Fact]
        public void Library_Serialization_Excludes_Verses()
        {
            var library = new Library
            {
                Code = "test-lib",
                Playlists = new List<Playlist>
                {
                    new Playlist
                    {
                        Key = "test-key",
                        Name = "Test Playlist",
                        BookChapters = new List<BookChapter>
                        {
                            new BookChapter
                            {
                                BookNumber = 43,
                                ChapterNumber = 1,
                                Segments = new List<Segment>
                                {
                                    new Segment
                                    {
                                        VerseStart = 1,
                                        VerseEnd = 9,
                                        Verses = new List<Verse>
                                        {
                                            new Verse { Number = 1, Html = "Gzipped verse text", AudioStart = 0, AudioEnd = 10 }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(library);

            Assert.DoesNotContain("Verses", json);
            Assert.DoesNotContain("Gzipped verse text", json);
            Assert.Contains("VerseStart", json);
            Assert.Contains("VerseEnd", json);
        }

        /// <summary>
        /// When a segment is edited and saved via SegmentEditor, the PUT request
        /// body sent to the server must not contain verse data. Server-side
        /// Newtonsoft [JsonIgnore] on Segment.Verses strips verse data from the
        /// persisted JSON file; the library loaded from the GET endpoint also
        /// excludes Verses via the server's Newtonsoft round-trip.
        /// </summary>
        [Fact]
        public async Task Editor_SaveSegment_PUTBody_DoesNotContain_Verses()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);
            HttpHandler.SetupPut("library");

            var cut = RenderEditor(1, 5, "test-playlist");
            await cut.InvokeAsync(() => Task.Delay(50));

            // Select first 3 verses and save
            SetSelectedVerses(cut, new[] { 1, 2, 3 });

            await cut.InvokeAsync(() =>
            {
                var saveButton = cut.FindAll("button").LastOrDefault(b => b.TextContent.Contains("Save"));
                saveButton?.Click();
            });

            await cut.InvokeAsync(() => Task.Delay(50));

            var putRequest = HttpHandler.Requests.FirstOrDefault(req =>
                req.Method == HttpMethod.Put &&
                req.RequestUri?.AbsoluteUri.Contains("library") == true);

            Assert.NotNull(putRequest);

            var putBody = await putRequest!.Content!.ReadAsStringAsync(Xunit.TestContext.Current.CancellationToken);
            // Verses may appear as null (client uses System.Text.Json which doesn't
            // see Newtonsoft's [JsonIgnore]), but the key requirement is NO verse DATA:
            Assert.DoesNotContain("[{\"number\"", putBody);   // no verse array
            Assert.DoesNotContain("Verse 1 text", putBody);   // no verse text
            Assert.DoesNotContain("audioStart", putBody);     // no audio timing
            Assert.Contains("verseStart", putBody);           // segment range preserved
            Assert.Contains("verseEnd", putBody);
        }
    }
}
