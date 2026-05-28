using MediatR;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.ChatMessages.Commands.DeleteChatMessage
{
    public class DeleteChatMessageCommandHandler
        : IRequestHandler<DeleteChatMessageCommand, Result<bool>>
    {
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;

        public DeleteChatMessageCommandHandler(
            IChatMessageRepository chatMessageRepository,
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser)
        {
            _chatMessageRepository = chatMessageRepository;
            _tripRepository = tripRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<bool>> Handle(
            DeleteChatMessageCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<bool>.Failure(DomainErrors.Auth.InvalidToken);
            }

            // Fetch the message (tracked by EF so we can update it)
            var message = await _chatMessageRepository.GetByIdAsync(request.MessageId);
            if (message == null || message.TripId != request.TripId || message.IsDeleted)
            {
                return Result<bool>.Failure(DomainErrors.Chat.MessageNotFound);
            }

            // Authorization: sender can delete their own messages,
            // trip owner can delete any message (content moderation)
            if (message.SenderId != userId)
            {
                var trip = await _tripRepository.GetByIdAsync(request.TripId);
                if (trip == null || trip.UserId != userId)
                {
                    return Result<bool>.Failure(DomainErrors.Chat.NotMessageOwner);
                }
            }

            // Soft delete
            message.IsDeleted = true;
            _chatMessageRepository.Update(message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}
