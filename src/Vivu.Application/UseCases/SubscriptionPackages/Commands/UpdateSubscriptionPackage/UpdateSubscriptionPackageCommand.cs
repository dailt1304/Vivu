using MediatR;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Enums;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.UpdateSubscriptionPackage;

public class UpdateSubscriptionPackageCommand : IRequest<Result<SubscriptionPackageDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Feature { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public int MaxAiRequestPerDay { get; set; }
    public bool IsActive { get; set; }
    public SubscriptionType? Type { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRecommended { get; set; }
}
