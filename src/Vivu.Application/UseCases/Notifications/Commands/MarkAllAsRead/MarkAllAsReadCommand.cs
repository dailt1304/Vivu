using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommand : IRequest<Result<int>>
    {
    }
}
