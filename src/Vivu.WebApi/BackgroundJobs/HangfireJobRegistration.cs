using Hangfire;
using TimeZoneConverter;

namespace Vivu.WebApi.BackgroundJobs
{
    public static class HangfireJobRegistration
    {
        public static void RegisterRecurringJobs(this IServiceProvider serviceProvider)
        {
            var vietnamTimeZone = TZConvert.GetTimeZoneInfo("Asia/Ho_Chi_Minh");
            RecurringJob.AddOrUpdate<UpdateTripStatusJob>(
                "update-trip-status",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily, 
                new RecurringJobOptions
                {
                    TimeZone = vietnamTimeZone
                });

            RecurringJob.AddOrUpdate<ExpireSubscriptionsJob>(
                "expire-subscriptions",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Daily,
                new RecurringJobOptions
                {
                    TimeZone = vietnamTimeZone
                });
        }
    }
}
