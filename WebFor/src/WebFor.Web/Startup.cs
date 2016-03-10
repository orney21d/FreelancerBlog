﻿using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebFor.Core.Domain;
using WebFor.Core.Repository;
using WebFor.Core.Services;
using WebFor.DependencyInjection;
using WebFor.Infrastructure.EntityFramework;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using WebFor.DependencyInjection.Modules;
using WebFor.DependencyInjection.Modules.Article;
using WebFor.Web.Services;

namespace WebFor.Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<WebForDbContext>(options =>
                    options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<WebForDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            services.AddCaching();
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.CookieName = ".WebFor";
            });

            services.AddTransient<IUrlHelper, UrlHelper>();

            // Autofac container configuration and modules
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<UnitOfWorkModule>();
            containerBuilder.RegisterModule<AuthMessageSenderModule>();
            containerBuilder.RegisterModule<WebForDbContextSeedDataModule>();
            containerBuilder.RegisterModule<CkEditorFileUploderModule>();
            containerBuilder.RegisterModule<ArticleCreatorModule>();
            containerBuilder.RegisterModule<ArticleEditorModule>();
            containerBuilder.RegisterType<WebForMapper>().As<IWebForMapper>();

            containerBuilder.Populate(services);
            var container = containerBuilder.Build();
            return container.Resolve<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, WebForDbContextSeedData seeder)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // For more details on creating database during deployment see http://go.microsoft.com/fwlink/?LinkID=615859
                try
                {
                    using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
                        .CreateScope())
                    {
                        serviceScope.ServiceProvider.GetService<WebForDbContext>()
                             .Database.Migrate();
                    }
                }
                catch { }
            }

            app.UseIISPlatformHandler(options => options.AuthenticationDescriptions.Clear());

            app.UseStaticFiles();

            app.UseIdentity();

            // To configure external authentication please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseSession();

            app.UseMvc(routes =>
            {
                 routes.MapRoute(name: "AreaRoute",
                 template: "{area:exists}/{controller}/{action}/{id?}/{title?}",
                 defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}/{title?}");


            });

            seeder.SeedAdminUser();
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}