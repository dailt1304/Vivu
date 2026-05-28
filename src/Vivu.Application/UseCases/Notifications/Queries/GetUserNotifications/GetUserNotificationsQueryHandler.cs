using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Extensions;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Queries.GetUserNotifications
{
    public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, Result<PaginatedList<NotificationDto>>>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUserNotificationsQueryHandler> _logger;

        public GetUserNotificationsQueryHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetUserNotificationsQueryHandler> logger)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PaginatedList<NotificationDto>>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Fetching notifications for user: {UserId}, UnreadOnly: {UnreadOnly}, Type: {Type}, Page: {PageNumber}, PageSize: {PageSize}",
                request.UserId, request.UnreadOnly, request.Type, request.PageNumber, request.PageSize);

            var query = _notificationRepository.GetByUserIdQuery(request.UserId, trackChanges: false);

            if (request.UnreadOnly == true)
            {
                query = query.Where(n => !n.IsRead);
            }

            if (!string.IsNullOrWhiteSpace(request.Type))
            {
                var typeLower = request.Type.ToLower();
                query = query.Where(n => n.Type != null && n.Type.ToLower() == typeLower);
            }

            var count = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            var dtos = _mapper.Map<List<NotificationDto>>(items);
            
            var paginatedResult = new PaginatedList<NotificationDto>(
                dtos, 
                count, 
                request.PageNumber, 
                request.PageSize);

            return Result<PaginatedList<NotificationDto>>.Success(paginatedResult);
        }
    }
}
