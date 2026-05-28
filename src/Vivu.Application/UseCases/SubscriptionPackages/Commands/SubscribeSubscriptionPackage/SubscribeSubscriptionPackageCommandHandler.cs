using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.SubscriptionPackages.Commands.SubscribeSubscriptionPackage;

public class SubscribeSubscriptionPackageCommandHandler: IRequestHandler<SubscribeSubscriptionPackageCommand, Result<UserSubscriptionDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
    private readonly IUserSubscriptionRepository _userSubscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SubscribeSubscriptionPackageCommandHandler> _logger;

    public SubscribeSubscriptionPackageCommandHandler(
        ICurrentUser currentUser,
        ISubscriptionPackageRepository subscriptionPackageRepository,
        IUserSubscriptionRepository userSubscriptionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SubscribeSubscriptionPackageCommandHandler> logger)
    {
        _currentUser = currentUser;
        _subscriptionPackageRepository = subscriptionPackageRepository;
        _userSubscriptionRepository = userSubscriptionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserSubscriptionDto>> Handle(SubscribeSubscriptionPackageCommand request, CancellationToken cancellationToken)
    {

        // Check Auth
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
        {
            _logger.LogWarning("Subscribe package failed: Invalid or missing user ID");
            return Result<UserSubscriptionDto>.Failure(DomainErrors.Auth.InvalidToken);
        }

        var package = await _subscriptionPackageRepository.GetByIdAsync(request.PackageId);
        if (package == null || !package.IsActive)
        {
            _logger.LogWarning(
                "Subscribe package failed: Package not found or inactive. PackageId: {PackageId}",
                request.PackageId);
            return Result<UserSubscriptionDto>.Failure(DomainErrors.Subscription.PackageNotFound);
        }

        var activeSubscription = await _userSubscriptionRepository.GetUserActiveSubscription(userId, cancellationToken);

        if (activeSubscription != null)
        {
            _logger.LogInformation(
                "Subscribe package skipped: User {UserId} already has active subscription {SubscriptionId}",
                userId,
                activeSubscription.Id);
            return Result<UserSubscriptionDto>.Failure(DomainErrors.Subscription.AlreadySubscribed);
        }

        var userSubscription = Domain.Entities.UserSubscription.Create(userId, package);

        await _userSubscriptionRepository.AddAsync(userSubscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} subscribed package {PackageId} with subscription {SubscriptionId}",
            userId,
            package.Id,
            userSubscription.Id);

        var response = _mapper.Map<UserSubscriptionDto>(userSubscription);

        return Result<UserSubscriptionDto>.Success(response);
    }
}