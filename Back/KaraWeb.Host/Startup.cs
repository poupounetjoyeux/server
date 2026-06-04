using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaraWeb.Core.Helpers;
using KaraWeb.Core.Persistence;
using KaraWeb.Core.Services.LibrariesAnalyzer;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Host.Conventions;
using KaraWeb.Host.Providers.Libraries;
using KaraWeb.Host.Providers.Songs;
using KaraWeb.Host.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace KaraWeb.Host
{
    internal sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.AllowTrailingCommas = true;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddDbContext<KaraWebDbContext>();

            services.AddControllers(o =>
                o.Conventions.Add(new GlobalRoutePrefixConvention(Constants.ApiMainRoutePrefix)));
            services.AddSignalR();

            services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
                c.EnableAnnotations();
                c.SwaggerDoc(Constants.ProjectName, new OpenApiInfo
                {
                    Title = Constants.ProjectName,
                    Version = $"{GetType().Assembly.GetName().Version}",
                    Description = "KaraWeb allows you to manage and server your karaoke sound files"
                });
                c.DocumentAsyncFilter<RoutePrefixDocumentFilter>(Constants.ApiMainRoutePrefix);
            });

            RegisterServices(services);
            RegisterProviders(services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            services
                .AddSingleton<ILibrariesAnalyzerService, LibrariesAnalyzerService>()
                .AddSingleton<ISongParserService, SongParserService>();
        }

        private void RegisterProviders(IServiceCollection services)
        {
            services
                .AddSingleton<ILibrariesProvider, LibrariesProvider>()
                .AddSingleton<ISongsProvider, SongsProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            var swaggerPrefix = Constants.ApiMainRoutePrefix + "/swagger";
            const string docName = "openapi.json";
            app.UseSwagger(c =>
            {
                c.RouteTemplate = swaggerPrefix + "/{documentName}/" + docName;
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer>
                    {
                        new() { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}/{Constants.ApiMainRoutePrefix}" }
                    };
                });
            });
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = swaggerPrefix;
                c.SwaggerEndpoint($"{Constants.ProjectName}/{docName}", Constants.ProjectName);
            });
        }
    }
}