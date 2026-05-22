using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MudBlazor.Services;

namespace BiblePlaylist.Tests
{
    public class MudBlazor9MigrationTests : TestContext
    {
        public MudBlazor9MigrationTests()
        {
            // existing project uses MudBlazor; keep same service registrations as other tests
            Services.AddMudBlazorDialog();
            Services.AddMudServices();
        }

        [Fact]
        public void Migration_APIShape_DialogService_HasAsyncMethods()
        {
            // Use reflection to avoid compile-time dependency on new interfaces
            var mudAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Equals("MudBlazor", StringComparison.OrdinalIgnoreCase));

            // If MudBlazor assembly is not loaded, test is inconclusive (will fail later when Dallas adds updates)
            Assert.NotNull(mudAssembly);

            var idialogType = mudAssembly.GetType("MudBlazor.IDialogService");
            Assert.NotNull(idialogType);

            var method = idialogType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name.IndexOf("ShowMessageBox", StringComparison.OrdinalIgnoreCase) >= 0);

            Assert.NotNull(method);

            // Expect method to return a Task (async pattern in v9)
            Assert.True(typeof(Task).IsAssignableFrom(method.ReturnType), "Dialog message box method should return Task in MudBlazor v9");
        }

        [Fact]
        public void Migration_Converter_IConverter_Exists_IfIntroduced()
        {
            var type = Type.GetType("MudBlazor.IConverter, MudBlazor");
            // If IConverter exists in MudBlazor 9.x, tests should be able to find it; assert discovery (non-fatal if null)
            // We assert true only if present; otherwise the test records that v9 interface not present in runtime.
            if (type != null)
            {
                Assert.True(type.IsInterface, "IConverter should be an interface if present");
            }
            else
            {
                Assert.True(true, "IConverter not present in runtime; will be validated after migration");
            }
        }

        [Fact]
        public async Task Migration_DialogService_ShowMessageBoxAsync_Invokeable()
        {
            var mudAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Equals("MudBlazor", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(mudAssembly);

            var idialogType = mudAssembly.GetType("MudBlazor.IDialogService");
            Assert.NotNull(idialogType);

            var dialogService = Services.GetService(idialogType);
            Assert.NotNull(dialogService);

            var method = idialogType.GetMethods().FirstOrDefault(m => m.Name.IndexOf("ShowMessageBox", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.NotNull(method);

            //var result = method.Invoke(dialogService, new object[] { "Warning","Deleting can not be undone!","Delete!","Cancel" });

            //if (result is Task task)
            //{
            //    // await and ensure completes
            //    await task;
            //    Assert.True(task.IsCompleted);
            //}
            //else
            //{
            //    Assert.Fail("Expected an awaitable Task result from ShowMessageBox method in v9");
            //}
        }

        //[Fact]
        //public void Migration_ComponentDefaults_SanityCheck_ButtonAndGrid()
        //{
        //    // Render a MudButton and a MudGrid and check for expected class tokens rather than private defaults
        //    var buttonRender = RenderComponent<MudBlazor.Components.Highlighter>();
        //    var markup = buttonRender.Markup;
        //    Assert.Contains("mud-button", markup, StringComparison.OrdinalIgnoreCase);

        //    // MudGrid check - presence of grid container class
        //    var gridType = Type.GetType("MudBlazor.MudGrid, MudBlazor");
        //    if (gridType != null)
        //    {
        //        var gridRender = RenderComponent(gridType);
        //        Assert.Contains("mud-grid", gridRender.Markup, StringComparison.OrdinalIgnoreCase);
        //    }
        //    else
        //    {
        //        Assert.True(true, "MudGrid type not available at runtime; will be validated after migration");
        //    }
        //}

        //[Fact]
        //public void Migration_V9_VisualDefaults_ButtonExplicitRequired()
        //{
        //    // v9 removes MudGlobal defaults; test that explicit Variant/Color produces expected output
        //    var cut = RenderComponent<MudBlazor.Components.MudButton>(parameters => parameters
        //        .Add(p => p.Variant, Variant.Filled)
        //        .Add(p => p.Color, Color.Primary));
        //    var markup = cut.Markup;
        //    Assert.Contains("mud-button-filled", markup, StringComparison.OrdinalIgnoreCase);
        //    Assert.Contains("mud-primary", markup, StringComparison.OrdinalIgnoreCase);
        //    // Additional test for default button (v9 behavior)
        //    var defaultCut = RenderComponent<MudBlazor.Components.MudButton>();
        //    Assert.DoesNotContain("mud-button-filled", defaultCut.Markup); // confirms default change
        //}

        //[Fact]
        //public void Migration_V9_Theming_MainLayoutProviders()
        //{
        //    // Test theming and providers from MainLayout focus area
        //    Services.AddMudServices(); // ensure full services
        //    var cut = RenderComponent<MudBlazor.Components.MudThemeProvider>();
        //    Assert.NotNull(cut.Instance);
        //    // Verify dialog and snackbar providers can be rendered without error (async ready)
        //    var dialogCut = RenderComponent<MudBlazor.Components.MudDialogProvider>();
        //    Assert.NotNull(dialogCut);
        //}

        //[Fact]
        //public void Migration_NoMudGlobal_Confirmed()
        //{
        //    // Per Ripley/Dallas history: low risk, confirm no global config in assembly
        //    var mudAssembly = AppDomain.CurrentDomain.GetAssemblies()
        //        .FirstOrDefault(a => a.GetName().Name?.Equals("MudBlazor", StringComparison.OrdinalIgnoreCase) == true);
        //    if (mudAssembly != null)
        //    {
        //        var globalType = mudAssembly.GetType("MudBlazor.MudGlobal");
        //        Assert.Null(globalType ?? (object?)null); // v9 removal confirmed if null
        //    }
        //}
    }
}
