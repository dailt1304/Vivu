using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.ChatMessages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetTripChatHistory
{
    public class GetTripChatHistoryQuery : PaginationRequest, IRequest<Result<PaginatedList<ChatMessageDto>>>
    {
        public Guid TripId { get; init; }
    }
}
