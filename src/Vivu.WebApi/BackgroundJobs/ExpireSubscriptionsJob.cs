using MediatR;
using Vivu.Application.UseCases.Subscriptions.Commands.ExpireSubscriptions;

namespace Vivu.WebApi.BackgroundJobs
{
    public class ExpireSubscriptionsJob : IRecurringJobService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ExpireSubscriptionsJob> _logger;

        public ExpireSubscriptionsJob(
            IMediator mediator,
            ILogger<ExpireSubscriptionsJob> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Expire Subscriptions Job started at {Time}", DateTime.UtcNow);

            await _mediator.Send(new ExpireSubscriptionsCommand(), cancellationToken);

            _logger.LogInformation("Expire Subscriptions Job finished at {Time}", DateTime.UtcNow);
        }
    }
}
