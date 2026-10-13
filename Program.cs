using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;
using ProjectFlow.Api.Configuration;
using ProjectFlow.Api.Authorization;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.Hubs;
using ProjectFlow.Api.Helpers;
using ProjectFlow.Api.Interfaces;
using ProjectFlow.Api.Middleware;
using ProjectFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5121");



// CONFIGURATION
var configuration = builder.Configuration;

// CONTROLLERS
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    new GenericApiError
                    {
                        Field = entry.Key,
                        Code = "INVALID_FIELD",
                        Message = string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage
                    }));

            return new BadRequestObjectResult(
                GenericApiResponse<object?>.ValidationError(
                    errors,
                    "One or more validation errors occurred.",
                    context.HttpContext.TraceIdentifier
                )
            );
        };
    });



// SWAGGER / OPENAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // API information
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "ProjectFlow API",
            Version = "v1",
            Description =
                "ProjectFlow project management API. " +
                "Provides authentication, users, projects, tasks, " +
                "teams, dashboard, analytics, settings, " +
                "work submissions and AI features."
        }
    );


    // JWT authentication
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",

            Type = SecuritySchemeType.Http,

            Scheme = "bearer",

            BearerFormat = "JWT",

            In = ParameterLocation.Header,

            Description =
                "Enter your JWT access token.\n\n" +
                "Example:\n" +
                "Bearer eyJhbGciOiJIUzI1NiIs..."
        }
    );


    // Apply JWT authentication
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        }
    );
});



// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});



// MONGODB
MongoDbSettings mongoSettings =
    configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>()
    ?? new MongoDbSettings();

var mongoConnectionString =
    mongoSettings.ConnectionString;

var mongoDatabaseName =
    mongoSettings.DatabaseName;

if (string.IsNullOrWhiteSpace(
        mongoConnectionString))
{
    throw new InvalidOperationException(
        "MongoDB connection string is missing."
    );
}

if (string.IsNullOrWhiteSpace(
        mongoDatabaseName))
{
    throw new InvalidOperationException(
        "MongoDB database name is missing."
    );
}

builder.Services.AddSingleton(
    mongoSettings
);

builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(
        mongoConnectionString
    )
);

builder.Services.AddSingleton<MongoDbContext>();



// JWT AUTHENTICATION
JwtSettings jwtSettings =
    configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()
    ?? new JwtSettings();

var jwtKey =
    jwtSettings.SecretKey;

var jwtIssuer =
    jwtSettings.Issuer;

var jwtAudience =
    jwtSettings.Audience;

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT key is missing."
    );
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException(
        "JWT issuer is missing."
    );
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT audience is missing."
    );
}

builder.Services.AddSingleton(
    jwtSettings
);


builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtKey
                        )
                    ),

                ValidateIssuer = true,

                ValidIssuer =
                    jwtIssuer,

                ValidateAudience = true,

                ValidAudience =
                    jwtAudience,

                ValidateLifetime = true,

                ClockSkew =
                    TimeSpan.FromMinutes(60),

                RoleClaimType =
                    System.Security.Claims
                        .ClaimTypes.Role,

                NameClaimType =
                    System.Security.Claims
                        .ClaimTypes.Name
            };


        // JWT debugging
        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived =
                    context =>
                    {
                        if (context.Request.Cookies.TryGetValue(
                                AuthCookieHelper.AccessTokenCookie,
                                out var accessToken))
                        {
                            context.Token = accessToken;
                        }

                        Console.WriteLine(
                            "JWT Authorization header: " +
                            (
                                string.IsNullOrWhiteSpace(
                                    context.Request
                                        .Headers
                                        .Authorization
                                )
                                    ? "MISSING"
                                    : "PRESENT"
                            )
                        );

                        return Task.CompletedTask;
                    },


                OnAuthenticationFailed =
                    context =>
                    {
                        Console.WriteLine(
                            "JWT authentication failed: " +
                            context.Exception.Message
                        );

                        return Task.CompletedTask;
                    },


                OnTokenValidated =
                    context =>
                    {
                        Console.WriteLine(
                            "JWT token validated successfully."
                        );

                        return Task.CompletedTask;
                    }
            };
    });



// AUTHORIZATION
builder.Services.AddSingleton<IPermissionService, PermissionService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permission));
        });
    }
});
builder.Services.AddSignalR();



// APPLICATION SERVICES
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService, ProfileService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<WorkSubmissionService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// SETTINGS
builder.Services.Configure<JwtSettings>(
    configuration.GetSection(
        "JwtSettings"
    )
);

// BUILD APPLICATION
var app = builder.Build();



// SWAGGER
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "ProjectFlow API v1"
    );

    options.RoutePrefix =
        "swagger";

    options.DocumentTitle =
        "ProjectFlow API Documentation";
});



// EXCEPTION HANDLING
app.UseMiddleware<ExceptionMiddleware>();

// HTTPS
app.UseHttpsRedirection();

// STATIC FILES
app.UseStaticFiles();



// CORS
app.UseCors("Frontend");


// AUTHENTICATION
app.UseAuthentication();

// AUTHORIZATION
app.UseAuthorization();
// Run database seeder once during startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<MongoDbContext>();

    await DatabaseSeeder.SeedAsync(db);
}

// CONTROLLERS
app.MapControllers();
app.MapHub<NotificationHub>(
    "/hubs/notifications"
);

// RUN
app.Run();
