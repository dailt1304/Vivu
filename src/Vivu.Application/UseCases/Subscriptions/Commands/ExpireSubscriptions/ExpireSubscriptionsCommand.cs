using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Subscriptions.Commands.ExpireSubscriptions;

public class ExpireSubscriptionsCommand : IRequest<Result> { }
