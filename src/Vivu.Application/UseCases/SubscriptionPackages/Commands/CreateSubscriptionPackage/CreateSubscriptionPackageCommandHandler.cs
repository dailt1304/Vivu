using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.CreateSubscriptionPackage;

public class CreateSubscriptionPackageCommandHandler
    : IRequestHandler<CreateSubscriptionPackageCommand, Result<SubscriptionPackageDto>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateSubscriptionPackageCommandHandler> _logger;

    public CreateSubscriptionPackageCommandHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateSubscriptionPackageCommandHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPackageDto>> Handle(
        CreateSubscriptionPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate code uniqueness if provided
            if (!string.IsNullOrWhiteSpace(request.Code))
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

            // Create new package 
            var package = SubscriptionPackage.Create(
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

            await _subscriptionPackageRepository.AddAsync(package);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created subscription package {PackageId} with name {PackageName}",
                package.Id,
                package.Name);

            var dto = _mapper.Map<SubscriptionPackageDto>(package);
            return Result<SubscriptionPackageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription package");
            return Result<SubscriptionPackageDto>.Failure(
                DomainErrors.Subscription.CreateFailed);
        }
    }
}