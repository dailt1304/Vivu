namespace Vivu.WebApi.BackgroundJobs
{
    public interface IRecurringJobService
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
