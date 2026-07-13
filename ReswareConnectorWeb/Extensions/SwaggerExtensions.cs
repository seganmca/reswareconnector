// Add these using statements at the top
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using ReswareConnectorWeb.Config;

public static class SwaggerExtensions
{
    // Updated to be version-aware
    public static IServiceCollection AddSwaggerWithApiKeyAndVersioning(
        this IServiceCollection services,
        IConfiguration configuration,
        string title = "API")
    {
        var authConfig = configuration.GetSection("Authentication").Get<AuthenticationConfig>();

        // 1. Add Swagger generation services with version support
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            // 2. Keep your API Key security configuration here
            if (authConfig?.ApiKey != null)
            {
                options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Name = authConfig.ApiKey.HeaderName,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Description = $"API Key authentication using {authConfig.ApiKey.HeaderName} header"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            }

            // 3. (Optional) Add operation filter for version parameter
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    // Updated to be version-aware
    public static IApplicationBuilder ConfigureSwaggerWithVersioning(
        this IApplicationBuilder app,
        string prefix)
    {
        // Get the API version provider
        var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseSwagger(options =>
        {
            // Keep your route template if needed, but ensure it's version-aware
            string swaggerRoutePrefix = prefix + "/swagger";
            options.RouteTemplate = swaggerRoutePrefix + "/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            string swaggerRoutePrefix = prefix + "/swagger";
            options.RoutePrefix = swaggerRoutePrefix;

            // Build a Swagger endpoint for each discovered API version
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/{swaggerRoutePrefix}/{description.GroupName}/swagger.json",
                    $"API {description.GroupName.ToUpperInvariant()}");

                // Optional: Add a suffix for deprecated versions
                if (description.IsDeprecated)
                {
                    options.SwaggerEndpoint(
                        $"/{swaggerRoutePrefix}/{description.GroupName}/swagger.json",
                        $"API {description.GroupName.ToUpperInvariant()} (Deprecated)");
                }
            }
        });

        return app;
    }
}

// ============ SUPPORTING CLASSES ============

// This class creates Swagger documents for each API version
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        // Create a Swagger document for each discovered API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName, // This will be "1.0", "2.0", etc.
                CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo()
        {
            Title = "Your API Title",
            Version = description.ApiVersion.ToString(),
            Description = "API Description"
        };

        if (description.IsDeprecated)
        {
            info.Description += " **This API version has been deprecated.**";
        }

        return info;
    }
}

// Optional: This filter helps clean up version parameter display in Swagger
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Hide the version parameter from the Swagger docs
        operation.Parameters ??= new List<OpenApiParameter>();

        // If using URL segment versioning, Swagger will add a {version} parameter
        // You can remove it or adjust it as needed
        var versionParameter = operation.Parameters
            .FirstOrDefault(p => p.Name == "version" && p.In == ParameterLocation.Path);

        if (versionParameter != null)
        {
            operation.Parameters.Remove(versionParameter);
        }
    }
}