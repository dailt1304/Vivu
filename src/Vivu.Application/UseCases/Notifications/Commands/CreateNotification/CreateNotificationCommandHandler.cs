using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Application.Interfaces.Cache;
using Vivu.Application.Interfaces.Notifications;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.CreateNotification
{
    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<NotificationDto>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationFactory _notificationFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CreateNotificationCommandHandler> _logger;

        public CreateNotificationCommandHandler(
            INotificationRepository notificationRepository,
            INotificationFactory notificationFactory,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<CreateNotificationCommandHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _notificationFactory = notificationFactory;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<NotificationDto>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating notification for User: {UserId}, Type: {Type}", request.UserId, request.Type);

            string title = request.Title ?? string.Empty;
            string content = request.Content ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                var generatedContent = _notificationFactory.CreateContent(request.Type);
                title = string.IsNullOrWhiteSpace(title) ? generatedContent.Title : title;
                content = string.IsNullOrWhiteSpace(content) ? generatedContent.Content : content;
            }

            var notification = Notification.Create(
                userId: request.UserId,
                type: request.Type,
                title: title,
                content: content,
                referenceId: request.ReferenceId);

            await _notificationRepository.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate unread count cache
            await _cacheService.RemoveAsync($"notifications:unread-count:{request.UserId}", cancellationToken);

            _logger.LogInformation("Notification created successfully. Id: {Id}", notification.Id);

            var dto = _mapper.Map<NotificationDto>(notification);
            return Result<NotificationDto>.Success(dto);
        }
    }
}
