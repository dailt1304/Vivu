using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.ChatMessages;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetTripChatHistory
{
    public class GetTripChatHistoryQueryHandler
    : IRequestHandler<GetTripChatHistoryQuery, Result<PaginatedList<ChatMessageDto>>>
    {
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ITripMemberRepository _tripMemberRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public GetTripChatHistoryQueryHandler(
            IChatMessageRepository chatMessageRepository,
            ITripMemberRepository tripMemberRepository,
            ICurrentUser currentUser,
            IMapper mapper)
        {
            _chatMessageRepository = chatMessageRepository;
            _tripMemberRepository = tripMemberRepository;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedList<ChatMessageDto>>> Handle(
            GetTripChatHistoryQuery request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
                return Result<PaginatedList<ChatMessageDto>>.Failure(DomainErrors.Auth.InvalidToken);

            var isMember = await _tripMemberRepository.GetByTripAndUserAsync(request.TripId,userId, cancellationToken);
            if (isMember == null)
                return Result<PaginatedList<ChatMessageDto>>.Failure(DomainErrors.TripMember.NotMember);

            var query = _chatMessageRepository.GetByTripId(request.TripId,cancellationToken);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);

            items.Reverse();

            var pagedMessages = PaginatedList<ChatMessage>.Create(items, totalCount, request.PageNumber, request.PageSize);

            var result = pagedMessages.Map(m => _mapper.Map<ChatMessageDto>(m));

            return Result<PaginatedList<ChatMessageDto>>.Success(result);
        }
    }
}
