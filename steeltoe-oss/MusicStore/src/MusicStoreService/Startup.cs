using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicStore.Models;
using Steeltoe.Discovery.Client;
using Steeltoe.Connector.MySql.EFCore;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace MusicStore
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
            // Add Steeltoe Management services
            services.AddHypermediaActuator(Configuration);
            services.AddInfoActuator(Configuration);
            services.AddHealthActuator(Configuration);
            services.AddLoggersActuator(Configuration);
            services.AddTraceActuator(Configuration);
            services.AddMappingsActuator(Configuration);

            // Add framework services.
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // Steeltoe Service Discovery
            services.AddDiscoveryClient(Configuration);

            // Steeltoe MySQL Connector
            services.AddDbContext<MusicStoreContext>(options => options.UseMySql(Configuration));

            // Add Framework services
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            // Add Steeltoe Management endpoints into pipeline
            app.UseHypermediaActuator();
            app.UseInfoActuator();
            app.UseHealthActuator();
            app.UseLoggersActuator();
            app.UseTraceActuator();
            app.UseMappingsActuator();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "api",
                    pattern: "{controller}/{id?}");
            });
            
            app.RegisterSpringBootAdmin(Configuration);
            
            // Start Steeltoe Discovery services
            app.UseDiscoveryClient();
        }
    }
}
