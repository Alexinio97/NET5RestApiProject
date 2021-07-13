using Catalog.Configuration;
using Catalog.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;


//docker run -d --rm --name mongo -p 27017:27017 -v mongodbData:/data/db mongo - start docker container that contains the database

namespace Catalog
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

            services.AddControllers(options =>
            {
                options.SuppressAsyncSuffixInActionNames = false;
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog", Version = "v1" });
            });

            BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.BsonType.String));
            BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(MongoDB.Bson.BsonType.String));

            var mongoDbConfig = Configuration.GetSection(nameof(MongoDbConfig))
                            .Get<MongoDbConfig>();

            services.AddSingleton<IMongoClient>(serviceProvider =>
            {
                return new MongoClient(mongoDbConfig.ConnectionString);
            });
            services.AddSingleton<IItemsRepository,MongoDbItemsRepo>();

            // needs middleware
            services.AddHealthChecks()
                .AddMongoDb(
                            mongoDbConfig.ConnectionString,
                            name: "mongodb health", 
                            timeout: TimeSpan.FromSeconds(3),
                            tags: new[] { "ready" });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                     Predicate = (check) => check.Tags.Contains("ready"),
                     ResponseWriter = async(context,report) =>
                     {
                         var result = JsonSerializer.Serialize(
                             new
                             {
                                 status = report.Status.ToString(),
                                 checks = report.Entries.Select(entry => new
                                 {
                                     name = entry.Key,
                                     status = entry.Value.Status.ToString(),
                                     exception = entry.Value.Exception != null ?
                                                            entry.Value.Exception.Message : "none",
                                     duration = entry.Value.Duration.ToString()
                                 })
                             }
                         );
                         // format the output
                         context.Response.ContentType = MediaTypeNames.Application.Json;
                         await context.Response.WriteAsync(result);
                     }
                     
                }); //middleware for healthChecks
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = (_) => false // exclude every check including the one above
                }); //middleware for healthChecks

            });
        }
    }
}
