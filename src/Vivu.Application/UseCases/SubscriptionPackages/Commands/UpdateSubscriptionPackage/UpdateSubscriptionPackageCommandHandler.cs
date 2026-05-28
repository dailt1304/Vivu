using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.UpdateSubscriptionPackage;

public class UpdateSubscriptionPackageCommandHandler
    : IRequestHandler<UpdateSubscriptionPackageCommand, Result<SubscriptionPackageDto>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateSubscriptionPackageCommandHandler> _logger;

    public UpdateSubscriptionPackageCommandHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateSubscriptionPackageCommandHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPackageDto>> Handle(
        UpdateSubscriptionPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get existing package
            var package = await _subscriptionPackageRepository.GetByIdAsync(request.Id);
            if (package == null)
            {
                _logger.LogWarning(
                    "Update subscription package failed: Package not found. PackageId: {PackageId}",
                    request.Id);
                return Result<SubscriptionPackageDto>.Failure(
                    DomainErrors.Subscription.PackageNotFound);
            }

            // Validate code uniqueness if code is being changed
            if (!string.IsNullOrWhiteSpace(request.Code) && 
                request.Code != package.Code)
            {
                var codeExists = await _subscriptionPackageRepository.IsCodeExistsAsync(
                    request.Code,
                    cancellationToken);

                if (codeExists)
                {
                    return Result<SubscriptionPackageDto>.Failure(
                        DomainErrors.Subscription.CodeExists(request.Code));
                }
            }

            // Update package
            package.Update(
                name: request.Name,
                price: request.Price,
                durationDays: request.DurationDays,
                maxAiRequestPerDay: request.MaxAiRequestPerDay,
                type: request.Type,
                feature: request.Feature,
                code: request.Code,
                description: request.Description,
                isActive: request.IsActive,
                displayOrder: request.DisplayOrder,
                isRecommended: request.IsRecommended
            );

            _subscriptionPackageRepository.Update(package);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated subscription package {PackageId} with name {PackageName}",
                package.Id,
                package.Name);

            var dto = _mapper.Map<SubscriptionPackageDto>(package);
            return Result<SubscriptionPackageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription package {PackageId}", request.Id);
            return Result<SubscriptionPackageDto>.Failure(
                DomainErrors.Subscription.UpdateFailed);
        }
    }
}
