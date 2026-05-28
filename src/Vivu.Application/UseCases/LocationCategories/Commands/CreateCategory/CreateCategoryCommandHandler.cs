using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
    {
        private readonly ILocationCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateCategoryCommandHandler> _logger;

        public CreateCategoryCommandHandler(
            ILocationCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<CreateCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(
            CreateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new location category: {Name}", request.Name);

            // Check duplicate name
            var allNames = await _categoryRepository.GetAllCategoryNamesAsync();
            if (allNames.Any(n => n.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Duplicate category name: {Name}", request.Name);
                return Result<Guid>.Failure(DomainErrors.LocationCategory.DuplicateName);
            }

            var category = new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                IconUrl = request.IconUrl,
                CategoryType = (LocationCategoryType)request.CategoryType,
                IsActive = true
            };

            await _categoryRepository.AddAsync(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created location category {Id}: {Name}", category.Id, category.Name);

            return Result<Guid>.Success(category.Id);
        }
    }
}
