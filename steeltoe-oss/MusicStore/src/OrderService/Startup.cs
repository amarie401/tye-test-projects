using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Models;
using Steeltoe.CloudFoundry.Connector.MySql.EFCore;
using Steeltoe.Discovery.Client;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Trace;

namespace OrderService
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
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // Add framework services.
            services.AddMvc();

            services.AddDiscoveryClient(Configuration);

            services.AddDbContext<OrdersContext>(options => options.UseMySql(Configuration));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // Add management endpoints into pipeline
            app.UseHypermediaActuator();
            app.UseInfoActuator();
            app.UseHealthActuator();
            app.UseLoggersActuator();
            app.UseTraceActuator();
            app.UseMappingsActuator();

            app.UseMvc();

            app.UseDiscoveryClient();
        }
    }
}
