using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.DeleteSubscriptionPackage;

public class DeleteSubscriptionPackageCommandHandler
    : IRequestHandler<DeleteSubscriptionPackageCommand, Result<SubscriptionPackageDto>>
{
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DeleteSubscriptionPackageCommandHandler> _logger;

    public DeleteSubscriptionPackageCommandHandler(
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<DeleteSubscriptionPackageCommandHandler> logger)
    {
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPackageDto>> Handle(
        DeleteSubscriptionPackageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get existing package
            var package = await _subscriptionPackageRepository.GetByIdAsync(request.Id);
            if (package == null)
            {
                _logger.LogWarning(
                    "Delete subscription package failed: Package not found. PackageId: {PackageId}",
                    request.Id);
                return Result<SubscriptionPackageDto>.Failure(
                    DomainErrors.Subscription.PackageNotFound);
            }

            // Soft delete - deactivate the package
            package.Deactivate();

            _subscriptionPackageRepository.Update(package);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Deactivated subscription package {PackageId} with name {PackageName}",
                package.Id,
                package.Name);

            var dto = _mapper.Map<SubscriptionPackageDto>(package);
            return Result<SubscriptionPackageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subscription package {PackageId}", request.Id);
            return Result<SubscriptionPackageDto>.Failure(
                DomainErrors.Subscription.DeleteFailed);
        }
    }
}
