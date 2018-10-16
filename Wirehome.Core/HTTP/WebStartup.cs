﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swashbuckle.AspNetCore.Swagger;
using Wirehome.Core.HTTP.Controllers.Diagnostics;
using Wirehome.Core.Storage;

namespace Wirehome.Core.HTTP
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WebStartup
    {
        // ReSharper disable once UnusedParameter.Local
        public WebStartup(IConfiguration configuration)
        {
        }

        public static Action<IServiceCollection> OnServiceRegistration { get; set; }
        public static IServiceProvider ServiceProvider { get; set; }
        public static StorageService StorageService { get; set; }

        // ReSharper disable once UnusedMember.Global
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Wirehome.Core API",
                    Version = "v1",
                    Description = "This is the public API for the Wirehome.Core backend.",
                    License = new License()
                    {
                        Name = "Apache-2.0",
                        Url = "https://github.com/chkr1011/Wirehome.Core/blob/master/LICENSE"
                    },
                    Contact = new Contact
                    {
                        Name = "Wirehome.Core",
                        Email = string.Empty,
                        Url = "https://github.com/chkr1011/Wirehome.Core"
                    },
                });
            });

            OnServiceRegistration(services);
            services.AddSingleton(StorageService);

            ServiceProvider = services.BuildServiceProvider();
            return ServiceProvider;
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            ConfigureSwagger(app);
            ConfigureWebApps(app);
            ConfigureMvc(app);
        }

        private static void ConfigureMvc(IApplicationBuilder app)
        {
            app.UseMvc(config =>
            {
                var dataTokens = new RouteValueDictionary
                {
                    {
                        "Namespaces", new[] {typeof(SystemStatusController).Namespace}
                    }
                };

                config.MapRoute(
                    name: "default",
                    template: "api/{controller}/{action}/{id?}",
                    defaults: null,
                    constraints: null,
                    dataTokens: dataTokens
                );

                // TODO: Forward to htto listener from scripts.

                //app.Run(context =>
                //    context.Response.WriteAsync("Hello, World!"));
            });
        }

        private static void ConfigureWebApps(IApplicationBuilder app)
        {
            var webAppRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebApp");
            var webConfiguratorRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebConfigurator");
            var customContentRootPath = Path.Combine(StorageService.DataPath, "CustomContent");
            var repositoriesRootPath = Path.Combine(StorageService.DataPath, "Repositories");

            if (Debugger.IsAttached)
            {
                webAppRootPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Wirehome.App");

                webConfiguratorRootPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Wirehome.Configurator");
            }

            ExposeDirectory(app, "/app", webAppRootPath);
            ExposeDirectory(app, "/configurator", webConfiguratorRootPath);
            ExposeDirectory(app, "/customContent", customContentRootPath);
            ExposeDirectory(app, "/repositories", repositoriesRootPath);
        }

        private static void ExposeDirectory(IApplicationBuilder app, string uri, string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            app.UseFileServer(new FileServerOptions
            {
                RequestPath = uri,
                FileProvider = new PhysicalFileProvider(path)
            });
        }

        private static void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

            app.UseSwaggerUI(o =>
            {
                o.SwaggerEndpoint("/api/v1/swagger.json", "Wirehome.Core API v1");
            });
        }
    }
}