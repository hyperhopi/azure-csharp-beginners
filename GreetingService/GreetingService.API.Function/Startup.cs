﻿using GreetingService.API.Function.Authentication;
using GreetingService.Core.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;
using Serilog;
using GreetingService.Infrastructure.GreetingRepository;
using GreetingService.Infrastructure.UserService;

[assembly: FunctionsStartup(typeof(GreetingService.API.Function.Startup))]
namespace GreetingService.API.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = builder.GetContext().Configuration;        //Get all configured app settings in an IConfiguration like this

            builder.Services.AddHttpClient();
            builder.Services.AddLogging();

            //Create a Serilog logger and register it as a logger
            //Get the Azure Storage Account connection string from our IConfiguration
            builder.Services.AddLogging(c => 
            {
                var connectionString = config["LoggingStorageAccount"];
                if (string.IsNullOrWhiteSpace(connectionString))
                    return;

                var logName = $"{Assembly.GetCallingAssembly().GetName().Name}.log";
                var logger = new LoggerConfiguration()
                                    .WriteTo.AzureBlobStorage(connectionString,
                                                              restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                                                              storageFileName: "{yyyy}/{MM}/{dd}/" + logName,
                                                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                    .CreateLogger();

                c.AddSerilog(logger, true);
            });

            builder.Services.AddSingleton<IGreetingRepository, BlobGreetingRepository>();

            builder.Services.AddScoped<IUserService, BlobUserService>();

            builder.Services.AddScoped<IAuthHandler, BasicAuthHandler>();
        }
    }
}