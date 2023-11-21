﻿using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

namespace CourseLibrary.API;

internal static class StartupHelperExtensions
{
    // Add services to the container
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(configure =>
        {
            configure.ReturnHttpNotAcceptable = true;
            
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
        builder.Services.AddTransient<IProprtyMappingService,
            ProprtyMappingService>();

        builder.Services.AddScoped<ICourseLibraryRepository, 
            CourseLibraryRepository>();

        builder.Services.AddTransient<IPropertyCheckService
            , PropertyCheckService>();

        builder.Services.AddDbContext<CourseLibraryContext>(options =>
        {
            options.UseSqlite(@"Data Source=library.db");
        });

        builder.Services.AddAutoMapper(
            AppDomain.CurrentDomain.GetAssemblies());

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
 
        app.UseAuthorization();

        app.MapControllers(); 
         
        return app; 
    }

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var context = scope.ServiceProvider.GetService<CourseLibraryContext>();
                if (context != null)
                {
                    await context.Database.EnsureDeletedAsync();
                    await context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        } 
    }
}