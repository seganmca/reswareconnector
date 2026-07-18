using Microsoft.AspNetCore.Authentication;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Extensions;
using ReswareConnectorWeb.Security;
using ReswareConnectorWeb.ReswareServices;
using ReswareConnectorWeb.Services;
using Microsoft.EntityFrameworkCore;
using ReswareConnectorWeb.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using ReswareConnectorWeb.BackgroundServices;
using ReswareConnectorWeb.RetryServices;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Serilog;
using Refit;
using System.Net;
using ReswareConnectorWeb.TitleHub;
using System.Text.Json.Serialization;
using MySqlConnector;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Asp.Versioning;
using ReswareConnectorWeb.Validators;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ReswareConnectorWeb.Converters;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        try
        {
            Log.Logger.Information("Starting the application");
            var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfig>()
                    ?? throw new InvalidOperationException("Missing Authentication configuration");
            builder.Services.AddSingleton(authConfig);

            builder.Services.Configure<FileStorageConfig>(builder.Configuration.GetSection("FileStorageConfig"));

            builder.Services.Configure<BackgroundServiceConfig>(builder.Configuration.GetSection("BackgroundServiceConfig"));

            var titlehubApiConfig = builder.Configuration.GetSection("TitleHubApiConfig").Get<TitleHubConfig>()
                                ?? throw new InvalidOperationException("Missing TitleHubApiConfig in appsettings.json");

            // Add services to the container.
            builder.Services.Configure<ServiceClientOptions>(options =>
            {
                var configuration = builder.Configuration.GetSection("ServiceClients").Get<ServiceClientOptions>();

                options.ReceiveNoteService = new ServiceConfiguration
                {
                    ServiceUrl = configuration?.ReceiveNoteService.ServiceUrl ?? throw new ArgumentNullException("ServiceClients:ReceiveNoteService:ServiceUrl is required"),
                    UserNameVariable = configuration?.ReceiveNoteService.UserNameVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveNoteService:UserNameVariable is required"),
                    PasswordVariable = configuration?.ReceiveNoteService.PasswordVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveNoteService:PasswordVariable is required"),
                    BypassServiceCall = configuration?.ReceiveNoteService.BypassServiceCall ?? false,
                    LogSoapMessages = configuration?.ReceiveNoteService.LogSoapMessages ?? false,
                };
                options.ReceiveActionEventService = new ServiceConfiguration
                {
                    ServiceUrl = configuration?.ReceiveActionEventService.ServiceUrl ?? throw new ArgumentNullException("ServiceClients:ReceiveActionEventService:ServiceUrl is required"),
                    UserNameVariable = configuration?.ReceiveActionEventService.UserNameVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveActionEventService:UserNameVariable is required"),
                    PasswordVariable = configuration?.ReceiveActionEventService.PasswordVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveActionEventService:PasswordVariable is required"),
                    BypassServiceCall = configuration?.ReceiveActionEventService.BypassServiceCall ?? false,
                    LogSoapMessages = configuration?.ReceiveActionEventService.LogSoapMessages ?? false,
                };
                options.ReceiveSearchDataService = new ServiceConfiguration
                {
                    ServiceUrl = configuration?.ReceiveSearchDataService.ServiceUrl ?? throw new ArgumentNullException("ServiceClients:ReceiveSearchDataService:ServiceUrl is required"),
                    UserNameVariable = configuration?.ReceiveSearchDataService.UserNameVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveSearchDataService:UserNameVariable is required"),
                    PasswordVariable = configuration?.ReceiveSearchDataService.PasswordVariable ?? throw new ArgumentNullException("ServiceClients:ReceiveSearchDataService:PasswordVariable is required"),
                    BypassServiceCall = configuration?.ReceiveSearchDataService.BypassServiceCall ?? false,
                    LogSoapMessages = configuration?.ReceiveSearchDataService.LogSoapMessages ?? false,
                };
                options.CustomFieldService = new ServiceConfiguration
                {
                    ServiceUrl = configuration?.CustomFieldService.ServiceUrl ?? throw new ArgumentNullException("ServiceClients:CustomFieldService:ServiceUrl is required"),
                    UserNameVariable = configuration?.CustomFieldService.UserNameVariable ?? throw new ArgumentNullException("ServiceClients:CustomFieldService:UserNameVariable is required"),
                    PasswordVariable = configuration?.CustomFieldService.PasswordVariable ?? throw new ArgumentNullException("ServiceClients:CustomFieldService:PasswordVariable is required"),
                    BypassServiceCall = configuration?.CustomFieldService.BypassServiceCall ?? false,
                    LogSoapMessages = configuration?.CustomFieldService.LogSoapMessages ?? false,
                };
            });
            // Configure retry policies
            builder.Services.Configure<RetryPolicyConfig>(
                builder.Configuration.GetSection("RetryPolicyConfig"));

            // 3. Register Core Services
            builder.Services.AddSingleton<IServiceClientFactory, ServiceClientFactory>();
            builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
            builder.Services.AddSingleton<IServiceWrapperFactory, ServiceWrapperFactory>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<IBackgroundTaskService, BackgroundTaskService>();
            builder.Services.AddHostedService<AsyncPeriodicBackgroundService>();
            builder.Services.AddSingleton(typeof(BackgroundServiceHealth<>));
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.AllowTrailingCommas = true;
                    options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                    options.JsonSerializerOptions.Converters.Add(new ReceiveCurativeTypeEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            builder.Services.AddValidatorsFromAssemblyContaining<OrderDtoValidator>();

            // 5. Register Your Business Services (if you have any)
            builder.Services.AddTransient<IIntegrationService, IntegrationService>();

            // Configure Refit client for TitleHub API 
            ConfigureRefitClient<ITitleHubApi>(builder.Services, titlehubApiConfig);

            // Add DbContext with MySQL
            builder.Services.AddDbContext<ReswareConnectorDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }
                ));

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104857600; // 100MB
            });

            builder.Services.Configure<MvcOptions>(options =>
            {
                options.MaxModelBindingRecursionDepth = 32;
            });

            //builder.Services.AddSwaggerWithApiKey(builder.Configuration, "ReswareConnector");
            builder.Services.AddSwaggerWithApiKeyAndVersioning(builder.Configuration, "ReswareConnector");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = "ApiKeyOrWindows";
                options.DefaultChallengeScheme = "ApiKeyOrWindows";
            })
            .AddPolicyScheme("ApiKeyOrWindows", "API Key or Windows Auth", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    // Always try API key first if header exists
                    if (context.Request.Headers.ContainsKey(authConfig.ApiKey.HeaderName))
                        return "ApiKey";

                    // Fall back to Windows Auth only if no API key
                    return "Windows";
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null)
            .AddScheme<AuthenticationSchemeOptions, WindowsAuthHandler>("Windows", null); // Custom Windows hand

            builder.Services.AddHealthChecks()
                            .AddMySql()
                            .AddCheck<BackgroundServiceHealthCheck<AsyncPeriodicBackgroundService>>(nameof(AsyncPeriodicBackgroundService));

            // 3. (Prerequisite) Ensure MySqlDataSource is registered, often done by an ORM like Dapper.
            builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("DefaultConnection"));

            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0); // Set default version
                options.AssumeDefaultVersionWhenUnspecified = true; // Use default if client doesn't specify
                options.ReportApiVersions = true; // Return supported versions in the 'api-supported-versions' header
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"), // e.g., ?api-version=2.0
                    new HeaderApiVersionReader("x-api-version"), // e.g., Header: x-api-version 2.0
                    new UrlSegmentApiVersionReader() // e.g., /api/v2/products
                );
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Formats version as 'v1', 'v2', etc.
                options.SubstituteApiVersionInUrl = true; // Helps with URL segment versioning
            });

            //builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // Ensure database is created and migrations are applied
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ReswareConnectorDbContext>();
                dbContext.Database.Migrate();
            }

            //app.ConfigureSwagger("reswareconnector");
            app.ConfigureSwaggerWithVersioning("reswareconnector");

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            duration = entry.Value.Duration,
                            description = entry.Value.Description,
                            exception = entry.Value.Exception?.Message
                        }),
                        totalDuration = report.TotalDuration
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
                }
            });

            //app.MapHealthChecksUI(options =>
            //{
            //    options.UIPath = "/healthdashboard";
            //});

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureRefitClient<T>(IServiceCollection services, TitleHubConfig config) where T : class
    {
        services.AddRefitClient<T>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(config.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(config.RetryPolicy.TimeoutSeconds);
                // Add default headers
                c.DefaultRequestHeaders.Add(config.ApiKeyName, config.ApiKeyValue);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            })
            .AddStandardResilienceHandler(options =>
            {
                // Configure timeout policies FIRST
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(config.RetryPolicy.TimeoutSeconds);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(config.RetryPolicy.TimeoutSeconds * (config.RetryPolicy.Retry.MaxRetryAttempts + 1)); // Total timeout including retries

                // Configure retry policy
                //options.Retry.ShouldHandle = args =>
                //    ValueTask.FromResult(
                //        args.Outcome.Exception is HttpRequestException
                //        || (args.Outcome.Exception is System.Threading.Tasks.TaskCanceledException or OperationCanceledException)
                //            && !args.Context.CancellationToken.IsCancellationRequested
                //        || args.Outcome.Result?.StatusCode is >= HttpStatusCode.InternalServerError
                //            or HttpStatusCode.RequestTimeout
                //            or (HttpStatusCode)429  // TooManyRequests
                //    );
                //options.Retry.MaxRetryAttempts = config.RetryPolicy.Retry.MaxRetryAttempts;
                //options.Retry.Delay = config.RetryPolicy.Retry.RetryDelay;

                // Configure circuit breaker - ensure sampling duration is at least 2x attempt timeout
                var samplingDuration = TimeSpan.FromSeconds(config.RetryPolicy.CircuitBreaker.SamplingDurationSeconds);
                var attemptTimeout = TimeSpan.FromSeconds(config.RetryPolicy.TimeoutSeconds);

                if (samplingDuration < attemptTimeout * 2)
                {
                    // Auto-adjust to meet the requirement
                    samplingDuration = attemptTimeout * 2;
                }

                options.CircuitBreaker.SamplingDuration = samplingDuration;
                options.CircuitBreaker.FailureRatio = config.RetryPolicy.CircuitBreaker.FailureRatio;
                options.CircuitBreaker.MinimumThroughput = config.RetryPolicy.CircuitBreaker.MinimumThroughput;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(config.RetryPolicy.CircuitBreaker.BreakDurationSeconds);
            });
    }
}