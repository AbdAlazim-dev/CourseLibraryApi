﻿using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Logging;
using Marvin.Cache.Headers;

namespace CourseLibrary.API;

internal static class StartupHelperExtensions
{
    // Add services to the container
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(configure =>
        {
            configure.ReturnHttpNotAcceptable = true;
            configure.CacheProfiles.Add("240SecondCache",
                new() { Duration = 240 });
            
        }).AddNewtonsoftJson(setUpActions =>
        {
            setUpActions.SerializerSettings.ContractResolver = new
            CamelCasePropertyNamesContractResolver();
        }).AddXmlDataContractSerializerFormatters()
        .ConfigureApiBehaviorOptions(setUpAction => {

            setUpAction.InvalidModelStateResponseFactory = context =>
            {
                var problemDetailsFactory = context.HttpContext.RequestServices
                .GetRequiredService<ProblemDetailsFactory>();

                var validationProblemsDetails = problemDetailsFactory.CreateValidationProblemDetails(
                    context.HttpContext,
                    context.ModelState
                    );
                validationProblemsDetails.Detail = "see the error faild for more info";
                validationProblemsDetails.Instance = context.HttpContext.Request.Path;
                validationProblemsDetails.Type = "https://localhost:5000/CoureseLibrary/ValidationInputInfo";
                validationProblemsDetails.Status = StatusCodes.Status422UnprocessableEntity;
                validationProblemsDetails.Title = "one or more Validation errors eccure.";

                return new UnprocessableEntityObjectResult(
                    validationProblemsDetails
                    )
                {
                    ContentTypes = { "application/problem+json" }
                };

            };
        });

        builder.Services.Configure<MvcOptions>(config =>
        {
            var newToneSoftOutputFormatter = config.OutputFormatters
                .OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();

            if (newToneSoftOutputFormatter != null)
            {
                newToneSoftOutputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
            }
        });
        
        builder.Services.AddTransient<IProprtyMappingService,
            ProprtyMappingService>();

        builder.Services.AddScoped<ICourseLibraryRepository, 
            CourseLibraryRepository>();
        
        builder.Services.AddHttpCacheHeaders((expirationModelOptionsAction) =>
        {
            expirationModelOptionsAction.MaxAge = 120;
            expirationModelOptionsAction.CacheLocation =
                Marvin.Cache.Headers.CacheLocation.Private;
        },
        (ValidationModelOptions) =>
        {
            ValidationModelOptions.MustRevalidate = true;
        });

        builder.Services.AddLogging(builder => builder.AddConsole());

        builder.Services.AddTransient<IPropertyCheckService
            , PropertyCheckService>();

        builder.Services.AddDbContext<CourseLibraryContext>(options =>
        {
            options.UseSqlite(@"Data Source=library.db");
        });

        builder.Services.AddAutoMapper(
            AppDomain.CurrentDomain.GetAssemblies());

        builder.Services.AddResponseCaching();

        return builder.Build();
    }
    

    // Configure the request/response pipelien
    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("An Exvpected Error Accoure, Try Again Latter");
                });
            });
        }

        app.UseResponseCaching();

        app.UseHttpCacheHeaders();
 
        app.UseAuthorization();

        app.MapControllers(); 
         
        return app; 
    }

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {

                var context = scope.ServiceProvider.GetService<CourseLibraryContext>();
                if (context != null)
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.MigrateAsync();
                }

        }
    }
}