using System.Reflection;
using System.Text;
using System.Text.Json;
using dotenv.net;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog.Events;
using Serilog;
using StackExchange.Redis;
using Vivu.Application;
using Vivu.Application.Common.Behaviors;
using Vivu.Application.Interfaces.Email;
using Vivu.Infrastructure;
using Vivu.Infrastructure.Data;
using Vivu.Infrastructure.Services.Email;
using Vivu.WebApi.Middleware;
using Microsoft.Extensions.Options;
using Vivu.WebApi.Hubs;
using Hangfire;
using Hangfire.PostgreSql;
using Vivu.WebApi.BackgroundJobs;

namespace Vivu.WebApi;

public partial class Program
{
    public static void Main(string[] args)
    {
        DotEnv.Load();
        var builder = WebApplication.CreateBuilder(args);

        var corsPolicy = "AllowSpecificOrigins";

        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            var originsFromEnv = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(originsFromEnv))
            {
                allowedOrigins = originsFromEnv.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }


        //! NOTE: If using another SQL database (e.g. PostgreSQL, MySQL, etc.), change the above line accordingly.
        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSignalR();
        builder.Services.AddScoped<Vivu.Application.Interfaces.Notifications.INotificationHubService, Vivu.WebApi.Services.NotificationHubService>();
        builder.Services.AddScoped<Vivu.Application.Interfaces.Trips.ITripHubService, Vivu.WebApi.Services.TripHubService>();
        builder.Services.AddSingleton<Vivu.Application.Interfaces.AI.IGenerationTrackingService, Vivu.WebApi.Services.GenerationTrackingService>();
        ////* JWT Authentication
        //////


        ////* Lowercase URLs
        //builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        ////* CORS Policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SignalRPolicy", policy =>
            {
                var origins = new List<string> { "null" };
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                    origins.AddRange(allowedOrigins);

                policy
                    .WithOrigins(origins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        ////* Add services to the container.
        //var currentAssembly  = typeof(Program).Assembly;
        //var referencedAssemblies = currentAssembly .GetReferencedAssemblies()
        //    .Where(x => x.Name!.StartsWith("Vivu") && x.Name != null); //! Replace "Vivu" with your actual project prefix

        //var assemblies = referencedAssemblies
        //    .Select(Assembly.Load)
        //    .Append(currentAssembly);

        //foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        //{
        //    if (type.IsClass && !type.IsAbstract)
        //    {
        //        foreach (var iface in type.GetInterfaces())
        //        {
        //            if (iface.Name == $"I{type.Name}")
        //            {
        //                builder.Services.AddScoped(iface, type);
        //            }
        //        }
        //    }
        //}

        ////* Some DI registrations can override for services lifetimes
        //builder.Services.AddTransient<IEmailSender, EmailSender>();

        ////* Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Vivu API", Version = "v1", Description = "Vivu API Description" });

            //* Add JWT Authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                    new string[]{}
                }
            });
        });

        ////* Additional for controllers with json
        //builder.Services.AddControllers()
        //    .AddJsonOptions(options =>
        //    {
        //        options.JsonSerializerOptions.PropertyNamingPolicy =
        //            JsonNamingPolicy.CamelCase; //* Use original property names
        //        options.JsonSerializerOptions.PropertyNameCaseInsensitive =
        //            true; //* Enable case-insensitive property names
        //    });

        ////* Add Authentication and Authorization
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = "External";
        })
        .AddJwtBearer("Bearer", options => {
        })
        .AddCookie("External", options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401; // Unauthorized
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = 403; // Forbidden
                return Task.CompletedTask;
            };
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        });
        //.AddGoogle(options =>
        //{
        //    var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        //    var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        //    options.ClientId = clientId;
        //    options.ClientSecret = clientSecret;
        //    options.SaveTokens = true;
        //    options.ClaimActions.MapJsonKey("picture", "picture");
        //    options.CallbackPath = "/signin-google";
        //});

        builder.Services.PostConfigure<JwtBearerOptions>("Bearer", options =>
        {
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? string.Empty;
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? string.Empty;
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"] ?? string.Empty;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero, //* Disable the default 5-minute clock skew
                RequireExpirationTime = true //* Require the token to have an expiration time
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Cookies["ACCESS_TOKEN"];
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        var queryToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(queryToken) && path.StartsWithSegments("/hubs"))
                        {
                            accessToken = queryToken;
                            Console.WriteLine("SignalR token from query: " + accessToken);
                        }
                    }
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Exception, "Authentication failed.");
                    return Task.CompletedTask;
                },
                OnTokenValidated = _ =>
                {
                    // You can add additional validation here if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Skip the default logic.
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new { error = "You are not authorized" });
                    return context.Response.WriteAsync(result);
                }
            };
        });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        ////* Redis Cache
        //builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        //{
        //    var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

        //    if (string.IsNullOrEmpty(redisConnectionString))
        //    {
        //        redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        //        if (string.IsNullOrEmpty(redisConnectionString))
        //        {
        //            throw new InvalidOperationException("Redis connection string is not set in environment variables or configuration.");
        //        }
        //    }

        //    var configurationOptions = ConfigurationOptions.Parse(redisConnectionString, true);
        //    configurationOptions.AbortOnConnectFail = false;

        //    try
        //    {
        //        return ConnectionMultiplexer.Connect(configurationOptions);
        //    }
        //    catch (RedisConnectionException ex)
        //    {
        //        throw new InvalidOperationException("Failed to connect to Redis: " + ex.Message, ex);
        //    }
        //});

        //builder.Services.AddScoped<IDatabase>(sp =>
        //{
        //    var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
        //    return connectionMultiplexer.GetDatabase();
        //});
        // STEP 1: Configure Serilog
        Log.Logger = new LoggerConfiguration()
            // Minimum levels
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)

            // Enrich logs with contextual info
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Vivu.API")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()

            // Console output (for local development)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")

            // File output (rolling daily logs)
            .WriteTo.File(
                path: "logs/vivu-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30, // Keep 30 days
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                shared: true) // Allow multiple processes to write

            // Separate file for errors only
            .WriteTo.File(
                path: "logs/errors/vivu-error-.log",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                retainedFileCountLimit: 90, // Keep errors for 90 days
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")

            // Optional: Seq (Centralized logging server)
            // .WriteTo.Seq("http://localhost:5341")

            .CreateLogger();

        // STEP 2: Use Serilog
        builder.Host.UseSerilog();

        // STEP 3: Add services
        builder.Services.AddHttpContextAccessor(); // For LoggingBehavior
        builder.Services.AddControllers();

        // Register behaviors
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Add Hangfire
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddHangfireServer();


        builder.Services.AddHangfireServer();

        var app = builder.Build();
        app.UseSerilogRequestLogging(options =>
        {
            // Customize log level based on response status
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : elapsed > 10000 // > 10 seconds
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;

            // Enrich logs with additional data
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                }
            };
        });
        app.UseExceptionHandler();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("EnableSwagger"))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHangfireDashboard("/hangfire");
        app.Services.RegisterRecurringJobs();
        //else
        //{
        //    // Custom error handling endpoint
        //    app.UseExceptionHandler(appError =>
        //    {
        //        appError.Run(async context =>
        //        {
        //            var logger = app.Services.GetRequiredService<ILogger<Program>>();
        //            var exceptionHandlerPathFeature = 
        //                context.Features.Get<IExceptionHandlerFeature>();

        //            if (exceptionHandlerPathFeature?.Error != null)
        //            {
        //                logger.LogError(exceptionHandlerPathFeature.Error, "An unhandled exception has occurred.");
        //            }

        //            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        //            context.Response.ContentType = "application/json";
        //            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal Server Error" }));
        //        });
        //    });
        //    app.UseHsts();
        //}

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("SignalRPolicy");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<TripChatHub>("/hubs/trip-chat");
        app.MapHub<NotificationHub>("/hubs/notifications");

        app.MapControllers();

        app.Run();
        Log.CloseAndFlush();

    }
}