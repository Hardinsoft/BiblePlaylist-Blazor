using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BiblePlaylist.Client.Config;
using BiblePlaylist.Client.Events;
using BiblePlaylist.Shared.Bible;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Newtonsoft.Json;
using Xunit;

namespace BiblePlaylist.Tests;

/// <summary>
/// #49: the Parse page's per-verse Play buttons call PlayAudioSegment with
/// the AudioPlayer's source element id. The AudioPlayer renders its source
/// as {PlayerId}Source, so for the Parse page's player
/// (PlayerId="fullChapterAudioPlayer") the id is
/// "fullChapterAudioPlayerSource".
/// </summary>
public class ReadParseVersesTests : Bunit.TestContext
{
    public RecordingJSRuntime JSRuntime { get; private set; } = null!;
    public RecordingHttpMessageHandler HttpHandler { get; private set; } = null!;

    public ReadParseVersesTests()
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

    private void SetupVersionResponse()
    {
        var version = new Shared.Bible.Version
        {
            Books = new List<Book>
            {
                new Book
                {
                    Number = 1,
                    Name = "Genesis",
                    Chapters = new List<Chapter>
                    {
                        new Chapter
                        {
                            Number = 1,
                            AudioUrl = "01-Genesis-01.mp3",
                            Verses = Enumerable.Range(1, 3)
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

        HttpHandler.SetupGet("Book=1", JsonConvert.SerializeObject(version));
    }

    private IRenderedComponent<BiblePlaylist.Client.Shared.ReadParseVerses> RenderParseVerses()
    {
        SetupVersionResponse();
        var cut = RenderComponent<BiblePlaylist.Client.Shared.ReadParseVerses>();
        cut.WaitForAssertion(() => Assert.Contains("Verse 1 text.", cut.Markup),
            TimeSpan.FromSeconds(5));
        return cut;
    }

    /// <summary>
    /// #49: tapping a verse's Play button invokes PlayAudioSegment with
    /// the source element id the AudioPlayer actually renders
    /// ("fullChapterAudioPlayerSource"). The old id
    /// ("fullChapterAudioSource") matches no element, so PlayAudioSegment
    /// silently did nothing and the verse never played.
    /// </summary>
    [Fact]
    public void PlayVerse_UsesAudioPlayerSourceId()
    {
        var cut = RenderParseVerses();

        // The first fab in the first verse row is that verse's Play button.
        cut.FindAll(".mud-fab").First().Click();

        var play = JSRuntime.Invocations.Single(i => i.Method == "PlayAudioSegment");
        Assert.Equal("fullChapterAudioPlayer", play.Args[0]);
        Assert.Equal("fullChapterAudioPlayerSource", play.Args[1]);
        Assert.Contains("01-Genesis-01.mp3", play.Args[2]?.ToString());
        Assert.Equal(0m, play.Args[3]);
        Assert.Equal(10m, play.Args[4]);
    }
}
