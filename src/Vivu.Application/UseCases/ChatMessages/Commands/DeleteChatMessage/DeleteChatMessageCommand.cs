using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.ChatMessages.Commands.DeleteChatMessage
{
    public record DeleteChatMessageCommand : IRequest<Result<bool>>
    {
        public Guid TripId { get; init; }
        public Guid MessageId { get; init; }
    }
}
