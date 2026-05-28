using MediatR;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripStatus;

namespace Vivu.WebApi.BackgroundJobs
{
    public class UpdateTripStatusJob : IRecurringJobService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UpdateTripStatusJob> _logger;

        public UpdateTripStatusJob(
            IMediator mediator,
            ILogger<UpdateTripStatusJob> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Update Trip Status Job started at {Time}", DateTime.UtcNow);

            var command = new UpdateTripStatusCommand();
            await _mediator.Send(command);

            _logger.LogInformation("Update Trip Status Job finished at {Time}", DateTime.UtcNow);
        }
    }
}
