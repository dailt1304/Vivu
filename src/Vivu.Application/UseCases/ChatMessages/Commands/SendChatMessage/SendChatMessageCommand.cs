using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Application.DTOs.Responses.ChatMessages;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.ChatMessages.Commands.SendChatMessage
{
    public record SendChatMessageCommand : IRequest<Result<ChatMessageDto>>
    {
        public Guid TripId { get; init; }
        public string Content { get; init; } = string.Empty;
        public string MessageType { get; init; } = "text";
        public Guid? ReplyToId { get; init; }
        public IFormFile? Image { get; init; }
    }
}
