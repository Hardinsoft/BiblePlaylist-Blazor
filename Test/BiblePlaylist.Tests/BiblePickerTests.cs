using BiblePlaylist.Shared.Navigation;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace BiblePlaylist.Tests
{
    /// <summary>
    /// #41: the BiblePicker (book/chapter list split out of NavBibleMenu for
    /// the edit segment page). The picker only selects a chapter — what a
    /// selection does is the parent's job (the editor reloads for it).
    /// </summary>
    public class BiblePickerTests : Bunit.TestContext
    {
        public RecordingHttpMessageHandler HttpHandler { get; private set; } = null!;

        public BiblePickerTests()
        {
            Services.AddMudServices();
            Services.AddSingleton<IJSRuntime>(new RecordingJSRuntime());
            HttpHandler = new RecordingHttpMessageHandler();
            var httpClient = new HttpClient(HttpHandler) { BaseAddress = new Uri("http://localhost/") };
            Services.AddSingleton(httpClient);
        }

        private void SetupBibleMenuResponse()
        {
            var menu = new BibleMenu
            {
                Books = new List<NavBook>
                {
                    new NavBook
                    {
                        Number = 1,
                        Name = "Test Book",
                        Chapters = Enumerable.Range(1, 5)
                            .Select(n => new NavChapter { Number = n })
                            .ToList()
                    },
                    new NavBook
                    {
                        Number = 43,
                        Name = "John",
                        Chapters = Enumerable.Range(1, 3)
                            .Select(n => new NavChapter { Number = n })
                            .ToList()
                    }
                }
            };
            HttpHandler.SetupGet("BibleMenu?key=", JsonConvert.SerializeObject(menu));
        }

        private IRenderedComponent<BiblePlaylist.Client.Shared.BiblePicker> RenderPicker(
            int book, int chapter, EventCallback<(int Book, string BookName, int Chapter)>? callback = null)
        {
            var parameters = new List<ComponentParameter>
            {
                ComponentParameter.CreateParameter("SelectedBook", book),
                ComponentParameter.CreateParameter("SelectedChapter", chapter)
            };
            if (callback.HasValue)
                parameters.Add(ComponentParameter.CreateParameter("OnChapterSelected", callback.Value));
            var cut = RenderComponent<BiblePlaylist.Client.Shared.BiblePicker>(parameters.ToArray());
            cut.WaitForAssertion(() => Assert.Contains("John", cut.Markup), TimeSpan.FromSeconds(5));
            return cut;
        }

        [Fact]
        public void Picker_RendersBooks_AndSelectedBookChapters()
        {
            SetupBibleMenuResponse();
            var cut = RenderPicker(1, 2);

            Assert.Contains("Test Book", cut.Markup);
            Assert.Contains("John", cut.Markup);
            // The selected book's chapters are expanded on open.
            var cells = cut.FindAll(".mud-paper.py-4");
            Assert.Equal(5, cells.Count);
            Assert.Contains(cells, c => c.TextContent.Trim() == "2");
        }

        [Fact]
        public void Picker_SelectBook_ExpandsItsChapters()
        {
            SetupBibleMenuResponse();
            var cut = RenderPicker(1, 2);

            cut.FindAll(".mud-list-item").First(e => e.TextContent.Contains("John")).Click();

            var cells = cut.FindAll(".mud-paper.py-4");
            Assert.Equal(3, cells.Count);
            Assert.Contains(cells, c => c.TextContent.Trim() == "3");
        }

        [Fact]
        public void Picker_SelectChapter_InvokesCallbackWithBookAndChapter()
        {
            SetupBibleMenuResponse();
            (int Book, string BookName, int Chapter) captured = default;
            var callback = EventCallback.Factory.Create<(int, string, int)>(this, t => captured = t);

            var cut = RenderPicker(1, 2, callback);

            cut.FindAll(".mud-list-item").First(e => e.TextContent.Contains("John")).Click();
            cut.FindAll(".mud-paper.py-4").First(e => e.TextContent.Trim() == "2").Click();

            Assert.Equal((43, "John", 2), captured);
        }
    }
}
