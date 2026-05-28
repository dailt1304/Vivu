using System;
using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Notifications.Queries.GetUnreadCount
{
    public class GetUnreadCountQuery : IRequest<Result<int>>
    {
    }
}
