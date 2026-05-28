using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Blogs;
using Vivu.Application.Interfaces.BlogStoryDays;
using Vivu.Application.Interfaces.Cache;
using Vivu.Application.Interfaces.Email;
using Vivu.Application.Interfaces.Files;
using Vivu.Application.Interfaces.Invoices;
using Vivu.Application.Interfaces.Locations;
using Vivu.Application.Interfaces.Payment;
using Vivu.Application.Interfaces.TripFavourites;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;
using Vivu.Infrastructure.Repository;
using Vivu.Infrastructure.Services.AI;
using Vivu.Infrastructure.Services.AI.Clients;
using Vivu.Infrastructure.Services.AI.Converters;
using Vivu.Infrastructure.Services.AI.Factories;
using Vivu.Infrastructure.Services.Auth;
using Vivu.Infrastructure.Services.Blogs;
using Vivu.Infrastructure.Services.Cache;
using Vivu.Infrastructure.Services.Email;
using Vivu.Infrastructure.Services.Files;
using Vivu.Infrastructure.Services.Invoices;
using Vivu.Infrastructure.Services.Location;
using Vivu.Infrastructure.Services.Payment;
using Vivu.Infrastructure.Services.Trips;

namespace Vivu.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var redisConnectionString = configuration.GetConnectionString("RedisConnection");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseNetTopologySuite();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<VivuDbContext>(options =>
                options.UseNpgsql(dataSource, o => o.UseNetTopologySuite()));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ITripRepository, TripRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IOtpRepository, OtpRepository>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IBlogPostTagRepository, BlogPostTagRepository>();
            services.AddScoped<IBlogStoryDayRepository, BlogStoryDayRepository>();
            services.AddScoped<IBlogTagRepository, BlogTagRepository>();
            services.AddScoped<IBlogStoryDay, BlogStoryDayRepository>();
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IFilterLocation, LocationRepository>();
            services.AddScoped<IFilterTripFavourite, TripFavoriteRepository>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IBlogRepository, BlogRepository>();
            services.AddScoped<IBlogLikeRepository, BlogLikeRepository>();
            services.AddScoped<IBlogSaveRepository, BlogSaveRepository>();
            services.AddScoped<IHelperLocation, HelperLocation>();
            services.AddScoped<IBlogCommentRepository, BlogCommentRepository>();
            services.AddScoped<ISlugService, SlugService>();
            services.AddScoped<IPromptBuilder,PromptBuilder>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAuthTokenProcess, AuthTokenProcess>();
            services.AddScoped<IAuthorizeServices, AuthorizeServices>();
            services.AddScoped<ITripDayRepository, TripDayRepository>();
            services.AddScoped<IAIConvert, AIConvert>();
            services.AddScoped<ISearchTermLocation, SearchTermLocation>();
            services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            services.AddScoped<ISearchTermTrip, Services.Trips.SearchTermTrip>();
            services.AddScoped<IIntentDetectionService, IntentDetectionService>();
            services.AddScoped<ILocationRepository, LocationRepository>();
            services.AddScoped<ILocationCategoryRepository, LocationCategoryRepository>();
            services.AddScoped<ILocationReportRepository, LocationReportRepository>();
            services.AddScoped<IBlogReportRepository, BlogReportRepository>();
            services.AddScoped<ITripLocationRepository, TripLocationRepository>();
            services.AddScoped<ITripLocationAlternativeRepository, TripLocationAlternativeRepository>();
            services.AddScoped<ITripMemberRepository, TripMemberRepository>();
            services.AddScoped<ITripRatingRepository, TripRatingRepository>();
            services.AddScoped<ITripFavoriteRepository, TripFavoriteRepository>();
            services.AddScoped<IInviteCodeGenerator, InviteCodeGenerator>();
            services.AddScoped<IPublicTripService, PublicTripService>();
            services.AddScoped<ITripLimitChecker, TripLimitChecker>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<ICollectionRepository, CollectionRepository>();
            services.AddScoped<ICollectionLocationRepository, CollectionLocationRepository>();
            services.AddScoped<Vivu.Application.Interfaces.Notifications.INotificationFactory, Vivu.Infrastructure.Services.Notifications.NotificationFactory>();
            services.AddScoped<ILocationCategoryRepository, LocationCategoryRepository>();
            services.AddScoped<IStreamingAIService, StreamingAIService>();
            services.AddScoped<ILocationZoneService, LocationZoneService>();
            services.AddScoped<ITripItineraryOptimizer, TripItineraryOptimizer>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddHttpClient<ClaudeClient>()
            .AddPolicyHandler((sp, request) => GetRetryPolicy(sp))
            .AddPolicyHandler((sp, request) => GetCircuitBreakerPolicy(sp));

            services.AddHttpClient<OpenAIClient>()
                .AddPolicyHandler((sp, request) => GetRetryPolicy(sp))
                .AddPolicyHandler((sp, request) => GetCircuitBreakerPolicy(sp));

            services.AddHttpClient<GeminiClient>()
                .AddPolicyHandler((sp, request) => GetRetryPolicy(sp))
                .AddPolicyHandler((sp, request) => GetCircuitBreakerPolicy(sp));
            services.AddScoped<IAIClient, ClaudeClient>();
            services.AddScoped<IAIClient, OpenAIClient>();
            services.AddScoped<IAIClient, GeminiClient>();
            services.AddScoped<IAIParsing, AIParsing>();
            services.AddScoped<IAIClientFactory, AIClientFactory>();
            services.AddScoped<IRateLimitService, RateLimitService>();
            services.AddScoped<IApiUsageLogRepository, ApiUsageLogRepository>();
            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
            services.AddScoped<ISubscriptionPackageRepository, SubscriptionPackageRepository>();
            services.AddScoped<IUsageTrackingService, UsageTrackingService>();
            services.AddScoped<ICloudinaryService, CloudinaryService>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IUserPersonalizationService, UserPersonalizationService>();
            services.AddHttpClient<IPayOSService, PayOSService>();
            services.AddMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration["Redis:InstanceName"];
            });
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(redisConnectionString!);
            });
            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider sp)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var loggerFactory = sp.GetService<ILoggerFactory>();
                        var logger = loggerFactory?.CreateLogger("Vivu.AI.PollyHttpRetry");
                        if (logger != null)
                        {
                            var url = outcome.Result?.RequestMessage?.RequestUri?.Host ?? "Unknown API";
                            logger.LogWarning("POLLY RETRY [{Count}/3]: Delaying for {Delay}s due to error from {Url}. Status: {Status}", 
                                retryCount, 
                                timespan.TotalSeconds, 
                                url,
                                outcome.Result?.StatusCode);
                        }
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider sp)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));
        }
    }
}
