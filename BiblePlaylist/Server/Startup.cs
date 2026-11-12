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
using System.Text;
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

                // Debug cache buster: the <Version> injection in
                // BiblePlaylist.Client.csproj only runs on Publish, so in debug the
                // served index.html still carries the literal {{CACHE_BUST_VERSION}}
                // placeholder and browsers keep caching the framework files across
                // rebuilds. The dev server serves index.html straight from the
                // Client's source wwwroot (there is no build-output copy to rewrite),
                // so replace the placeholder in the served page instead, with a
                // timestamp taken once when the server starts. Every debug run gets
                // fresh files; nothing is written to disk and Publish is untouched.
                // The debug proxy (/_framework/debug) and static assets (/_content)
                // are excluded: the proxy upgrades the connection to a websocket and
                // must keep the real response stream.
                var debugCacheBustVersion = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                app.Use(async (context, next) =>
                {
                    var path = context.Request.Path.Value ?? string.Empty;
                    var extension = Path.GetExtension(path);
                    var isHtmlCandidate = path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrEmpty(extension);
                    if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
                        || path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase)
                        || !isHtmlCandidate)
                    {
                        await next();
                        return;
                    }

                    var originalBody = context.Response.Body;
                    await using var buffer = new MemoryStream();
                    context.Response.Body = buffer;
                    await next();
                    context.Response.Body = originalBody;

                    var servedBytes = buffer.ToArray();
                    var isHtml = context.Response.StatusCode == 200
                        && context.Response.ContentType != null
                        && context.Response.ContentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
                    if (!isHtml)
                    {
                        await originalBody.WriteAsync(servedBytes);
                        return;
                    }

                    var html = Encoding.UTF8.GetString(servedBytes);
                    if (!html.Contains("{{CACHE_BUST_VERSION}}", StringComparison.Ordinal))
                    {
                        await originalBody.WriteAsync(servedBytes);
                        return;
                    }

                    html = html.Replace("{{CACHE_BUST_VERSION}}", debugCacheBustVersion);
                    var outBytes = Encoding.UTF8.GetBytes(html);
                    if (!context.Response.HasStarted)
                    {
                        context.Response.ContentLength = outBytes.Length;
                    }
                    await originalBody.WriteAsync(outBytes);
                });
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
