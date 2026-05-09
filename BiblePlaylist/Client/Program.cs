using BiblePlaylist.Client.Config;
using BiblePlaylist.Client.Events;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace BiblePlaylist.Client
{
    public class Program
    {
        public delegate Task AsyncEventHandler(object sender, EventArgs e);
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var services = builder.Services;

            services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            services.AddScoped<IDelegateLibrary, DelegateLibrary>();            
            services.AddScoped(sp => sp.GetRequiredService<IConfiguration>().GetSection("Endpoints").Get<Endpoints>());
            services.AddBlazoredLocalStorage();
            services.AddMudServices();

            await builder.Build().RunAsync();
        }
    }
}
