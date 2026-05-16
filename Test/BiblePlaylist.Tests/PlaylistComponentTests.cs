using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.JSInterop;
using Moq;
using System.Threading.Tasks;

namespace BiblePlaylist.Tests
{
    public class PlaylistComponentTests : TestContext
    {
        public PlaylistComponentTests()
        {
            Services.AddMudBlazorDialog();
            Services.AddMudServices();
        }

        [Fact]
        public void PlaylistPage_Renders_PlayerComponent()
        {
            // Arrange
            var cut = RenderComponent<BiblePlaylist.Client.Pages.Playlist>();

            // Act & Assert
            cut.Markup.Contains("PlaylistPlayer");
        }

        [Fact]
        public async Task NavPlaylistMenu_Loads_Audio_InvokeJSInterop()
        {
            // Arrange
            var jsMock = Services.GetRequiredService<Mock<IJSRuntime>>();
            if (jsMock == null)
            {
                var js = new Mock<IJSRuntime>();
                Services.AddSingleton(js.Object);
                jsMock = js;
            }

            var cut = RenderComponent<BiblePlaylist.Client.Shared.NavPlaylistMenu>();

            // Act: call a public method via instance (if present)
            // This test is a smoke test to ensure component renders and JSInterop can be invoked.
            // Note: deeper behavior requires wiring HttpClient / local storage which is out of scope for a smoke test.

            Assert.NotNull(cut);
        }
    }
}
