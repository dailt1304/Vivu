using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<bool>>
    {
        private readonly ILocationCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateCategoryCommandHandler> _logger;

        public UpdateCategoryCommandHandler(
            ILocationCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            UpdateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating location category {Id}", request.Id);

            var category = await _categoryRepository.GetByIdAsync(request.Id);
            if (category == null)
            {
                _logger.LogWarning("Location category {Id} not found", request.Id);
                return Result<bool>.Failure(DomainErrors.LocationCategory.NotFoundById(request.Id));
            }

            // Check duplicate name (exclude self)
            var allNames = await _categoryRepository.GetAllCategoryNamesAsync();
            var otherCategories = await _categoryRepository.GetAllAsync();
            if (otherCategories.Any(c => c.Id != request.Id && c.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Duplicate category name: {Name}", request.Name);
                return Result<bool>.Failure(DomainErrors.LocationCategory.DuplicateName);
            }

            category.Name = request.Name;
            category.IconUrl = request.IconUrl;
            category.CategoryType = (LocationCategoryType)request.CategoryType;

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated location category {Id}", request.Id);

            return Result<bool>.Success(true);
        }
    }
}
