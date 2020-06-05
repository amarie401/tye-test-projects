using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicStoreUI.Models;
using MusicStoreUI.Services;
using Steeltoe.CircuitBreaker.Hystrix;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Connector.MySql.EFCore;
using Steeltoe.Connector.Redis;
using Steeltoe.Discovery.Client;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Security.DataProtection;
using System;
using Command = MusicStoreUI.Services.HystrixCommands;

namespace MusicStoreUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            // Add framework services.
            services.AddRedisConnectionMultiplexer(Configuration);
            services.AddDataProtection()
                .PersistKeysToRedis()
                .SetApplicationName("MusicStoreUI");
            services.AddDistributedRedisCache(Configuration);

            // Add management endpoint services
            services.AddHypermediaActuator(Configuration);
            services.AddInfoActuator(Configuration);
            services.AddHealthActuator(Configuration);
            services.AddLoggersActuator(Configuration);
            services.AddTraceActuator(Configuration);
            services.AddMappingsActuator(Configuration);

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddDbContext<AccountsContext>(options => options.UseMySql(Configuration));
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                    .AddEntityFrameworkStores<AccountsContext>()
                    .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.LoginPath = "/Account/LogIn";
                options.SlidingExpiration = true;
            });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
            });

            // Configure Auth
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "ManageStore",
                    authBuilder =>
                    {
                        authBuilder.RequireClaim("ManageStore", "Allowed");
                        authBuilder.RequireAuthenticatedUser();
                    });
            });

            services.AddDiscoveryClient(Configuration);

            services.AddHttpClient<IMusicStore, MusicStoreService>().AddRoundRobinLoadBalancer();
            services.AddHttpClient<IShoppingCart, ShoppingCartService>().AddServiceDiscovery();
            services.AddHttpClient<IOrderProcessing, OrderProcessingService>().AddServiceDiscovery();

            services.AddHystrixCommand<Command.GetTopAlbums>("MusicStore", Configuration);
            services.AddHystrixCommand<Command.GetGenres>("MusicStore", Configuration);
            services.AddHystrixCommand<Command.GetGenre>("MusicStore", Configuration);
            services.AddHystrixCommand<Command.GetAlbum>("MusicStore", Configuration);

            services.AddControllersWithViews();

            // Add Hystrix metrics stream to enable monitoring
            services.AddHystrixMetricsStream(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Add Hystrix Metrics context to pipeline
            app.UseHystrixRequestContext();

            // Add management endpoints into pipeline
            app.UseHypermediaActuator();
            app.UseInfoActuator();
            app.UseHealthActuator();
            app.UseLoggersActuator();
            app.UseTraceActuator();
            app.UseMappingsActuator();

            app.UseSession();
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();

            // Add cookie-based authentication to the request pipeline
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "areaRoute",
                    pattern: "{area:exists}/{controller}/{action}",
                    defaults: new { action = "Index" });

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.RegisterSpringBootAdmin(Configuration);
            app.UseDiscoveryClient();

            // Startup Hystrix metrics stream
            app.UseHystrixMetricsStream();
        }
    }
}
