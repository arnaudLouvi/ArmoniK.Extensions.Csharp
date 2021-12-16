﻿using System.IO;
using ArmoniK.Adapters.WorkerApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ArmoniK.DevelopmentKit.WorkerApi.Services;
using Microsoft.Extensions.Configuration;

namespace ArmoniK.DevelopmentKit.WorkerApi
{
  public class Startup
  {

      public Startup(IWebHostEnvironment env)
      {
          var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
              .AddEnvironmentVariables();


          Configuration = builder.Build();
      }

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
    {
        //services.AddConfiguration(Configuration);
        services.AddGrpc();
      
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      app.UseRouting();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGrpcService<ComputerService>();

        endpoints.MapGet("/",
                         async context =>
                         {
                           await context.Response
                                        .WriteAsync(
                                          "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                         });
      });
    }
  }
}