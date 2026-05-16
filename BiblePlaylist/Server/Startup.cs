using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using BiblePlaylist.Server.Data;
using System;
using BiblePlaylist.Shared.Structs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using BiblePlaylist.Shared.Utilities;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;

namespace BiblePlaylist.Server
{
    public class Startup
    {
        private string _corsAllowSpecificOrigins = "corsAllowSpecificOrigins";
        [Inject]
        private ILogger<Startup> _logger { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;          
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: _corsAllowSpecificOrigins,
                                  policy =>
                                  {
                                      policy.WithOrigins("https://raw.githubusercontent.com/Hardinsoft",
                                                         "https://localhost:44342,",
                                                         "https://bibleplaylist.azurewebsites.net");
                                  });
            });
            services.AddLogging(options => options.SetMinimumLevel(LogLevel.Debug));
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddScoped<IVersionRepository, VersionRepository>();
            services.AddScoped<IBibleMenuRepository, BibleMenuRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IAudioTimingProcessor, AudioTimingProcessorSimple>();
            var endpointsSection = Configuration.GetSection(ConfigurationSections.Endpoints);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<ICache, Cache>();

            services.Configure<Configuration.Endpoints>(endpointsSection);

            services.AddHttpClient(ClientName.Audio, client =>
            {
                client.BaseAddress = new Uri(endpointsSection[nameof(ClientName.Audio)]);
            });
            services.AddHttpClient(ClientName.Text, client =>
            {
                client.BaseAddress = new Uri(endpointsSection[nameof(ClientName.Text)]);
            });

            //Add Response Compression
            services.AddResponseCompression(opt =>
            {
                opt.Providers.Add<GzipCompressionProvider>();
                opt.EnableForHttps = true;
            });
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
            //services.AddBlazoredLocalStorage();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseCors(_corsAllowSpecificOrigins);            
            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });

            loggerFactory.AddFile(Path.Combine(env.ContentRootPath, $"Storage/logs/BPLog.txt"));
        }
    }
}
