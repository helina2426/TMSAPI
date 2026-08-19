using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Interfaces;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using TmsApi.Domain.Entities;
using TmsApi.Api.Filters;
using TmsApi.Infrastructure.Services;
using Asp.Versioning;
using TmsApi.Middleware;
using Microsoft.Extensions.Caching.Hybrid;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;

using MediatR;
using FluentValidation;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Api.ExceptionHandlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

//builder.Services.AddScoped<EnrollmentWorker>();

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddScoped<ICourseService, CourseService>();

builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(
    typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST — it must wrap ValidationBehavior
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(LoggingBehavior<,>));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

/*builder.Services
    .AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();*/

builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TmsDatabase"))
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging());

builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditFilter>();
});

/*builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v1";
});

builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description =>
        description.GroupName == "v2";
});
*/

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddOpenApi();

builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>(
        "Training",
        null);

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                var (partitionKey, tier) =
                    ApiKeyResolver.Resolve(httpContext);

                return tier switch
                {
                    ApiKeyTier.Paid =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            partitionKey: $"paid:{partitionKey}",
                            factory: _ =>
                                new TokenBucketRateLimiterOptions
                                {
                                    TokenLimit = 200,
                                    TokensPerPeriod = 100,
                                    ReplenishmentPeriod =
                                        TimeSpan.FromSeconds(10),
                                    QueueLimit = 0,
                                    AutoReplenishment = true
                                }),

                    ApiKeyTier.Free =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            partitionKey: $"free:{partitionKey}",
                            factory: _ =>
                                new TokenBucketRateLimiterOptions
                                {
                                    TokenLimit = 30,
                                    TokensPerPeriod = 10,
                                    ReplenishmentPeriod =
                                        TimeSpan.FromSeconds(10),
                                    QueueLimit = 0,
                                    AutoReplenishment = true
                                }),

                    _ =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            partitionKey: $"anon:{partitionKey}",
                            factory: _ =>
                                new TokenBucketRateLimiterOptions
                                {
                                    TokenLimit = 10,
                                    TokensPerPeriod = 5,
                                    ReplenishmentPeriod =
                                        TimeSpan.FromSeconds(10),
                                    QueueLimit = 0,
                                    AutoReplenishment = true
                                })
                };
            });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var retryAfterValue))
        {
            retryAfter =
                ((int)retryAfterValue.TotalSeconds).ToString();
        }

        context.HttpContext.Response.Headers.RetryAfter =
            retryAfter;

        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.HttpContext.Response.ContentType =
            "application/problem+json";

        var problem = new ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail =
                $"Too many requests. Retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tms.local/errors/rate_limit_exceeded"
        };

        await context.HttpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: ct);
    };

    // Exercise 4 — Step 3
    options.AddConcurrencyLimiter(
        "transcripts",
        opt =>
        {
            opt.PermitLimit = 5;
            opt.QueueLimit = 20;
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
        });

    // Exercise 4 — Step 5
    options.AddTokenBucketLimiter(
        "search",
        opt =>
        {
            opt.TokenLimit = 10;
            opt.TokensPerPeriod = 5;
            opt.ReplenishmentPeriod =
                TimeSpan.FromSeconds(10);
            opt.QueueLimit = 2;
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            opt.AutoReplenishment = true;
        });
});

var app = builder.Build();

app.UseCors("AllowAngular");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient)
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
}

// Configure the HTTP request pipeline.

app.UseExceptionHandler();

app.UseStatusCodePages();

app.UseMiddleware<RequestLoggingMiddleware>();

app.Use(async (context, next) =>
{
    await next();

    if (context.Request.Path.StartsWithSegments("/api/v1"))
    {
        context.Response.Headers["Deprecation"] = "true";

        context.Response.Headers["Sunset"] =
            "Thu, 31 Dec 2026 00:00:00 GMT";

        context.Response.Headers["Link"] =
            "<https://localhost:5103/api/v2/courses>; rel=\"successor-version\"";
    }
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<V1DeprecationMiddleware>();

app.UseRateLimiter();

app.MapControllers();

/*app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId = "S-001",
        letterGrade = "A"
    });
}).RequireAuthorization();*/

/*app.MapGet("/api/error", () =>
{
    throw new Exception("Test exception for ProblemDetails");
});*/

// Seeder goes here

/*using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<TmsDbContext>();

    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new()
            {
                RegistrationNumber = "TMS-2026-0001",
                Name = "Alice Smith",
                GPA = 3.8m,
                IsActive = true
            },

            new()
            {
                RegistrationNumber = "TMS-2026-0002",
                Name = "Bob Jones",
                GPA = 2.9m,
                IsActive = true
            },

            new()
            {
                RegistrationNumber = "TMS-2026-0003",
                Name = "Charlie Brown",
                GPA = 3.4m,
                IsActive = false
            },

            new()
            {
                RegistrationNumber = "TMS-2026-0004",
                Name = "Diana Prince",
                GPA = 3.9m,
                IsActive = true
            },

            new()
            {
                RegistrationNumber = "TMS-2026-0005",
                Name = "Evan Wright",
                GPA = 2.5m,
                IsActive = true
            }
        };

        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new()
            {
                Code = "CS-101",
                Title = "Introduction to Computer Science",
                MaxCapacity = 30
            },

            new()
            {
                Code = "CS-201",
                Title = "Data Structures and Algorithms",
                MaxCapacity = 25
            },

            new()
            {
                Code = "MAT-101",
                Title = "Calculus I",
                MaxCapacity = 40
            }
        };

        context.Courses.AddRange(courses);

        context.SaveChanges();

        var enrollments = new List<Enrollment>
        {
            new()
            {
                StudentId = students[0].Id,
                CourseId = courses[0].Id,
                Grade = 4.0m
            },

            new()
            {
                StudentId = students[0].Id,
                CourseId = courses[1].Id,
                Grade = 3.6m
            },

            new()
            {
                StudentId = students[1].Id,
                CourseId = courses[0].Id,
                Grade = 2.8m
            },

            new()
            {
                StudentId = students[3].Id,
                CourseId = courses[1].Id,
                Grade = 3.9m
            }
        };

        context.Enrollments.AddRange(enrollments);

        context.SaveChanges();
    }
}
*/

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<TmsDbContext>();

    await DataSeeder.SeedAsync(context);
}

app.Run();