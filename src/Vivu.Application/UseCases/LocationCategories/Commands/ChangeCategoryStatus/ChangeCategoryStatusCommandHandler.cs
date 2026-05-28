using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.ChangeCategoryStatus
{
    public class ChangeCategoryStatusCommandHandler : IRequestHandler<ChangeCategoryStatusCommand, Result<bool>>
    {
        private readonly ILocationCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChangeCategoryStatusCommandHandler> _logger;

        public ChangeCategoryStatusCommandHandler(
            ILocationCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<ChangeCategoryStatusCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            ChangeCategoryStatusCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Changing status of location category {Id} to IsActive={IsActive}",
                request.Id, request.IsActive);

            var category = await _categoryRepository.GetByIdAsync(request.Id);
            if (category == null)
            {
                _logger.LogWarning("Location category {Id} not found", request.Id);
                return Result<bool>.Failure(DomainErrors.LocationCategory.NotFoundById(request.Id));
            }

            category.IsActive = request.IsActive;

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully changed status of location category {Id} to IsActive={IsActive}",
                request.Id, request.IsActive);

            return Result<bool>.Success(true);
        }
    }
}
