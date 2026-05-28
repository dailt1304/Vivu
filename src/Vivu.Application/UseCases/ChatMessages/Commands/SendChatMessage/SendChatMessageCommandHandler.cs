using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.ChatMessages;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.ChatMessages.Commands.SendChatMessage
{
    public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, Result<ChatMessageDto>>
    {
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ITripRepository _tripRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IGenericRepository<FileAttachment> _fileAttachmentRepository;

        public SendChatMessageCommandHandler(
            IChatMessageRepository chatMessageRepository,
            ITripMemberRepository tripMemberRepository,
            ITripRepository tripRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper,
            ICloudinaryService cloudinaryService,
            IGenericRepository<FileAttachment> fileAttachmentRepository)
        {
            _chatMessageRepository = chatMessageRepository;
            _tripRepository = tripRepository;
            _tripMemberRepository = tripMemberRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _fileAttachmentRepository = fileAttachmentRepository;
        }

        public async Task<Result<ChatMessageDto>> Handle(
            SendChatMessageCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<ChatMessageDto>.Failure(DomainErrors.Auth.InvalidToken);
            }
            var trip = await _tripRepository.GetByIdAsync(request.TripId);
            if (trip == null)
            {
                return Result<ChatMessageDto>.Failure(DomainErrors.Trip.NotFound);
            }

            var isMember = await _tripMemberRepository.GetByTripAndUserAsync(request.TripId, userId, cancellationToken);
            if (isMember == null)
            {
                return Result<ChatMessageDto>.Failure(DomainErrors.TripMember.NotMember);
            }

            if (request.ReplyToId.HasValue)
            {
                var replyTo = await _chatMessageRepository.GetByIdAsync(request.ReplyToId.Value);
                if (replyTo == null || replyTo.TripId != request.TripId)
                {
                    return Result<ChatMessageDto>.Failure(DomainErrors.Chat.ReplyMessageNotFound);
                }
            }

            // Determine message type based on whether image is provided
            var messageType = request.Image is not null ? "image" : request.MessageType;

            var message = ChatMessage.Create(
                tripId: request.TripId,
                senderId: userId,
                content: request.Content,
                messageType: messageType,
                isAiMessage: false,
                replyToId: request.ReplyToId
            );

            await _chatMessageRepository.AddAsync(message);

            // Upload image if provided
            if (request.Image is not null)
            {
                using var stream = request.Image.OpenReadStream();
                var imageUrl = await _cloudinaryService.UploadImageAsync(
                    stream, request.Image.FileName, "trip-chat");

                var attachment = new FileAttachment
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    UploaderId = userId,
                    Url = imageUrl,
                    FileName = request.Image.FileName,
                    FileType = Path.GetExtension(request.Image.FileName).TrimStart('.').ToLower(),
                    FileSize = request.Image.Length,
                    CreatedDate = DateTime.UtcNow
                };
                await _fileAttachmentRepository.AddAsync(attachment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var savedMessage = await _chatMessageRepository.GetByIdWithSenderAsync(message.Id, cancellationToken);
            var dto = _mapper.Map<ChatMessageDto>(savedMessage);

            return Result<ChatMessageDto>.Success(dto);
        }
    }
}
