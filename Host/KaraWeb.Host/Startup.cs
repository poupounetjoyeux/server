using KaraWeb.Core;
using KaraWeb.Core.Helpers;
using KaraWeb.Host.Providers.Collections;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KaraWeb.Core.Services.CollectionsAnalyzer;
using KaraWeb.Core.Services.SongParser;
using KaraWeb.Host.Providers.Songs;
using Microsoft.AspNetCore.Mvc;

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

            services.AddControllers();
            services.AddSignalR();

            services.AddSwaggerGen(c =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, true);
                c.EnableAnnotations();
                c.SwaggerDoc(Constants.ProjectName, new OpenApiInfo { Title = Constants.ProjectName, Version = $"{GetType().Assembly.GetName().Version}" });
            });

            RegisterServices(services);
            RegisterProviders(services);
        }

        private void RegisterServices(IServiceCollection services)
        {
            services
                .AddSingleton<ICollectionsAnalyzerService, CollectionsAnalyzerService>()
                .AddSingleton<ISongParserService, SongParserService>();
        }

        private void RegisterProviders(IServiceCollection services)
        {
            services
                .AddSingleton<ICollectionsProvider, CollectionsProvider>()
                .AddSingleton<ISongsProvider, SongsProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var swaggerPrefix = Constants.ApiMainRoutePrefix + "swagger";
            const string DocName = "openapi.json";
            app.UseSwagger(c =>
            {
                c.RouteTemplate = swaggerPrefix + "/{documentName}/" + DocName;
            });
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = swaggerPrefix;
                c.SwaggerEndpoint($"{Constants.ProjectName}/{DocName}", Constants.ProjectName);

            });
        }
    }
}