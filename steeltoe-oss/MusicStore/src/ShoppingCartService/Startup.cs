using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShoppingCartService.Models;
using Steeltoe.Connector.MySql.EFCore;
using Steeltoe.Discovery.Client;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Steeltoe.Management.Endpoint.Trace;

namespace ShoppingCartService
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
            // Add managment endpoint services
            services.AddHypermediaActuator(Configuration);
            services.AddInfoActuator(Configuration);
            services.AddHealthActuator(Configuration);
            services.AddLoggersActuator(Configuration);
            services.AddTraceActuator(Configuration);
            services.AddMappingsActuator(Configuration);

            // Add framework services.
            services.AddControllers();

            services.AddDiscoveryClient(Configuration);

            services.AddDbContext<ShoppingCartContext>(options => options.UseMySql(Configuration));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            // Add management endpoints into pipeline
            app.UseHypermediaActuator();
            app.UseInfoActuator();
            app.UseHealthActuator();
            app.UseLoggersActuator();
            app.UseTraceActuator();
            app.UseMappingsActuator();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            app.RegisterSpringBootAdmin(Configuration);

            app.UseDiscoveryClient();
        }
    }
}
