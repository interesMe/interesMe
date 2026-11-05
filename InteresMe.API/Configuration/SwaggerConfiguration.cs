using Microsoft.OpenApi.Models;

namespace InteresMe.API.Configuration;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Some endpoints (e.g. chat messages) expose JSON and multipart variants on the same route.
            // OpenAPI 3.0 needs one operation per method/path, so document the first variant.
            options.ResolveConflictingActions(descriptions => descriptions.First());

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "InteresMe API",
                Version = "v1",
                Description = "Use POST /api/auth/login to get a JWT, then click Authorize and paste the token."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Authorization header. Enter only the token (without 'Bearer').",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
