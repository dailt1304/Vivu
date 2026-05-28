using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.LocationCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
    {
        private readonly ILocationCategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCategoryCommandHandler> _logger;

        public DeleteCategoryCommandHandler(
            ILocationCategoryRepository categoryRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(
            DeleteCategoryCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting location category {Id}", request.Id);

            // Lấy category kèm theo Locations để kiểm tra ràng buộc
            var allCategories = _categoryRepository.GetAllCategoriesWithLocationCountQuery();
            var category = await allCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
            {
                _logger.LogWarning("Location category {Id} not found", request.Id);
                return Result<bool>.Failure(DomainErrors.LocationCategory.NotFoundById(request.Id));
            }

            // Check ràng buộc: không cho xóa nếu đang có Locations sử dụng
            if (category.Locations != null && category.Locations.Any())
            {
                _logger.LogWarning(
                    "Cannot delete category {Id}: has {Count} locations",
                    request.Id, category.Locations.Count);
                return Result<bool>.Failure(DomainErrors.LocationCategory.HasLocations);
            }

            _categoryRepository.Remove(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted location category {Id}", request.Id);

            return Result<bool>.Success(true);
        }
    }
}
