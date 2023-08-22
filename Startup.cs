using imcd_api_response_speed.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nest;
using System;

namespace imcd_api_response_speed
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddControllers();
            services.AddHttpClient();

            // Add CloudHubIntegration as a singleton service
            services.AddSingleton<CloudHubIntegrationService>();

            // Add OpenSearchIntegration as a singleton service
            services.AddSingleton<OpenSearchIntegrationService>();

            // Add ElasticClient as a singleton service
            services.AddSingleton<ElasticClient>(sp =>
            {
                var endpoint = Configuration["OpenSearch:Endpoint"];
                var indexName = Configuration["OpenSearch:NonProdIndexnameDashboardStats"]; // Use the appropriate index name

                var username = Configuration["OpenSearch:Username"];
                var password = Configuration["OpenSearch:Password"];

                var settings = new ConnectionSettings(new Uri(endpoint))
                    .BasicAuthentication(username, password)
                    .DefaultIndex(indexName);

                return new ElasticClient(settings);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}