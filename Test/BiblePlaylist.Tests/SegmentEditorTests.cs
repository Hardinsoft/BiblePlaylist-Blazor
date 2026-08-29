using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.JSInterop;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BiblePlaylist.Shared.Bible;
using BiblePlaylist.Shared.DTO;
using BiblePlaylist.Shared.Playlist;
using BiblePlaylist.Shared.Data;
using System.Net.Http;
using System.Text;

namespace BiblePlaylist.Tests
{
    public class SegmentEditorTests : TestContext
    {
        private readonly Mock<IJSRuntime> _jsMock;
        private readonly Mock<HttpClient> _httpMock;

        public SegmentEditorTests()
        {
            Services.AddMudBlazorDialog();
            Services.AddMudServices();
            Services.AddBlazoredLocalStorage();

            _jsMock = new Mock<IJSRuntime>();
            Services.AddSingleton(_jsMock.Object);

            _httpMock = new Mock<HttpClient>();
            Services.AddSingleton(_httpMock.Object);
        }

        private void SetupVersionResponse(int bookNumber, int chapterNumber, string audioUrl = "http://example.com/chapter.mp3")
        {
            var version = new Version
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

            _httpMock.Setup(h => h.GetAsync(It.IsAny<string>()))
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

            _httpMock.Setup(h => h.GetAsync(It.IsAny<string>()))
                .ReturnsAsync(response);

            _httpMock.Setup(h => h.PutAsJsonAsync(It.IsAny<string>(), It.IsAny<Library>()))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        }

        [Fact]
        public void Editor_RendersChapterReference_WhenLoaded()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
            });

            Assert.Contains("Test Book 5", cut.Markup);
        }

        [Fact]
        public async Task Editor_TogglesVerseSelection_ShowsSegmentRange()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
            });

            // Initially in Create mode with no verses selected
            Assert.Contains("Select verses to create a new segment", cut.Markup);

            // Click verse 1 to select it
            var verseElements = cut.FindAll("div.d-flex.align-start");
            Assert.NotEmpty(verseElements);
            verseElements[0].Click();

            // After selection, show segment range
            Assert.Contains("1-1 (1 verses)", cut.Markup);
        }

        [Fact]
        public async Task Editor_PlayButton_WithNoSelection_NeverCallsInitializeAudio()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
            });

            // Click play button with no selection
            var buttons = cut.FindAll("button");
            buttons[0].Click();

            // Should NOT call JS interop for audio initialization
            _jsMock.Verify(js => js.InvokeVoidAsync("initializeAudioPlayer", It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Editor_PlayButton_WithSelection_CallsJSInterop()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.Add(p => p.PlaylistKey, "test-playlist");
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
                parameters.AddQueryString("segmentStart", "1");
                parameters.AddQueryString("segmentEnd", "3");
            });

            // Wait for async load
            await cut.InvokeAsync(() => Task.Delay(100));

            // Select verses 1-3
            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < 3 && i < verseElements.Count; i++)
            {
                verseElements[i].Click();
            }

            // Click play
            var buttons = cut.FindAll("button");
            buttons[0].Click();

            // Verify JS interop was called
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
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.Add(p => p.PlaylistKey, "test-playlist");
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
                parameters.AddQueryString("segmentStart", "1");
                parameters.AddQueryString("segmentEnd", "3");
            });

            await cut.InvokeAsync(() => Task.Delay(100));

            // Select verses and start playback
            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < 3 && i < verseElements.Count; i++)
            {
                verseElements[i].Click();
            }

            var buttons = cut.FindAll("button");
            buttons[0].Click(); // play

            _jsMock.Verify(js => js.InvokeVoidAsync("PlayAudioSegment",
                It.IsAny<decimal>(), It.IsAny<decimal>(), "segmentEditorPlayer"),
                Times.Once);

            buttons[0].Click(); // stop

            _jsMock.Verify(js => js.InvokeVoidAsync("pauseAudioPlayer", "segmentEditorPlayer"),
                Times.Once);
        }

        [Fact]
        public async Task Editor_SaveSegment_CallsPutLibrary()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.Add(p => p.PlaylistKey, "test-playlist");
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
                parameters.AddQueryString("segmentStart", "1");
                parameters.AddQueryString("segmentEnd", "3");
            });

            await cut.InvokeAsync(() => Task.Delay(100));

            // Select verses 1-3
            var verseElements = cut.FindAll("div.d-flex.align-start");
            for (int i = 0; i < 3 && i < verseElements.Count; i++)
            {
                verseElements[i].Click();
            }

            // Click save (third button)
            var buttons = cut.FindAll("button");
            buttons[2].Click();

            // Verify PUT /library was called
            _httpMock.Verify(h => h.PutAsJsonAsync("library", It.IsAny<Library>()),
                Times.Once);
        }

        [Fact]
        public async Task Editor_CancelButton_NavigatesBack()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
            });

            await cut.InvokeAsync(() => Task.Delay(100));

            // Cancel is second button
            var buttons = cut.FindAll("button");
            buttons[1].Click();

            // Verify navigation happened
            Assert.NotEqual("http://localhost/edit-segment?book=1&chapter=5", cut.Navigation.Location);
        }

        [Fact]
        public void Editor_CreateMode_WithoutPlaylistKey_ShowsPrompt()
        {
            SetupVersionResponse(1, 5);

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
            });

            Assert.Contains("Select verses to create a new segment", cut.Markup);
        }

        [Fact]
        public async Task Editor_EditMode_LoadsExistingSegment()
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
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.Add(p => p.PlaylistKey, "test-playlist");
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
                parameters.AddQueryString("segmentStart", "2");
                parameters.AddQueryString("segmentEnd", "4");
            });

            await cut.InvokeAsync(() => Task.Delay(100));

            Assert.Contains("2-4 (3 verses)", cut.Markup);
        }

        [Fact]
        public async Task Editor_EditMode_WithoutSegmentInPlaylist_FallsBackToCreate()
        {
            SetupVersionResponse(1, 5);
            SetupLibraryResponse("test-playlist", 1, 5, new List<Segment>());

            var cut = RenderComponent<BiblePlaylist.Client.Pages.SegmentEditor>();
            cut.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.BookNumber, 1);
                parameters.Add(p => p.ChapterNumber, 5);
                parameters.Add(p => p.PlaylistKey, "test-playlist");
                parameters.AddQueryString("book", "1");
                parameters.AddQueryString("chapter", "5");
                parameters.AddQueryString("playlist", "test-playlist");
                parameters.AddQueryString("segmentStart", "2");
                parameters.AddQueryString("segmentEnd", "4");
            });

            await cut.InvokeAsync(() => Task.Delay(100));

            // Should fall back to Create mode
            Assert.Contains("Select verses to create a new segment", cut.Markup);
        }
    }
}
