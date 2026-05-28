using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.UseCases.Locations.Queries.GetLocationCategories;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationCategories
{
    public class GetLocationCategoriesQueryHandlerTests
    {
        private readonly Mock<ILocationCategoryRepository> _locationCategoryRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetLocationCategoriesQueryHandler>> _loggerMock;
        private readonly GetLocationCategoriesQueryHandler _handler;

        public GetLocationCategoriesQueryHandlerTests()
        {
            _locationCategoryRepositoryMock = new Mock<ILocationCategoryRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetLocationCategoriesQueryHandler>>();

            _handler = new GetLocationCategoriesQueryHandler(
                _locationCategoryRepositoryMock.Object,
                _loggerMock.Object,
                _mapperMock.Object
            );
        }

        #region Helper Methods

        private static GetLocationCategoriesQuery CreateQuery(int pageNumber = 1, int pageSize = 10)
        {
            return new GetLocationCategoriesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static LocationCategory CreateCategory(string name = "Test Category", int? locationCount = null)
        {
            return new LocationCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                IconUrl = "https://example.com/icon.png"
            };
        }

        private static LocationCategoryDto CreateCategoryDto(LocationCategory category)
        {
            return new LocationCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IconUrl = category.IconUrl,
                LocationCount = 0
            };
        }

        private void SetupRepository(List<LocationCategory> categories)
        {
            var mockQueryable = categories.AsQueryable().BuildMock();
            _locationCategoryRepositoryMock
                .Setup(x => x.GetAllCategoriesWithLocationCountQuery())
                .Returns(mockQueryable);
        }

        private void SetupMapper(List<LocationCategory> categories, List<LocationCategoryDto> dtos)
        {
            _mapperMock
                .Setup(x => x.Map<List<LocationCategoryDto>>(It.IsAny<List<LocationCategory>>()))
                .Returns(dtos);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithCategories_ReturnsSuccessWithPaginatedList()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory>
            {
                CreateCategory("Category A"),
                CreateCategory("Category B"),
                CreateCategory("Category C")
            };

            var dtos = categories.Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithSingleCategory_ReturnsSingleItem()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory>
            {
                CreateCategory("Only Category")
            };

            var dtos = categories.Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(1);
            result.Value.Items[0].Name.Should().Be("Only Category");
            result.Value.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_WithEmptyRepository_ReturnsEmptyList()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory>();
            var dtos = new List<LocationCategoryDto>();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            var categories = new List<LocationCategory>
            {
                CreateCategory("Category 1"),
                CreateCategory("Category 2"),
                CreateCategory("Category 3"),
                CreateCategory("Category 4"),
                CreateCategory("Category 5")
            };

            // Page 2 with size 2 → items 3 & 4
            var pageDtos = categories.Skip(2).Take(2).Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, pageDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(5);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.Items.Should().HaveCount(2);
            result.Value.TotalPages.Should().Be(3);
            result.Value.HasPreviousPage.Should().BeTrue();
            result.Value.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FirstPage_HasNoPreviousPage()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 1, pageSize: 2);

            var categories = new List<LocationCategory>
            {
                CreateCategory("Category 1"),
                CreateCategory("Category 2"),
                CreateCategory("Category 3")
            };

            var dtos = categories.Take(2).Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.HasPreviousPage.Should().BeFalse();
            result.Value.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LastPage_HasNoNextPage()
        {
            // Arrange
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            var categories = new List<LocationCategory>
            {
                CreateCategory("Category 1"),
                CreateCategory("Category 2"),
                CreateCategory("Category 3"),
                CreateCategory("Category 4")
            };

            var dtos = categories.Skip(2).Take(2).Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.HasPreviousPage.Should().BeTrue();
            result.Value.HasNextPage.Should().BeFalse();
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 5)]
        [InlineData(3, 3)]
        public async Task Handle_WithDifferentPageSizes_ReturnsCorrectPagination(int pageNumber, int pageSize)
        {
            // Arrange
            var query = CreateQuery(pageNumber: pageNumber, pageSize: pageSize);

            var categories = Enumerable.Range(1, 15)
                .Select(i => CreateCategory($"Category {i}"))
                .ToList();

            var pageDtos = categories
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(CreateCategoryDto)
                .ToList();

            SetupRepository(categories);
            SetupMapper(categories, pageDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.PageNumber.Should().Be(pageNumber);
            result.Value.PageSize.Should().Be(pageSize);
            result.Value.TotalCount.Should().Be(15);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_Always_CallsGetAllCategoriesOnce()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory> { CreateCategory() };
            var dtos = categories.Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _locationCategoryRepositoryMock.Verify(
                x => x.GetAllCategoriesWithLocationCountQuery(),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithCategories_CallsMapperOnce()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory>
            {
                CreateCategory("Category A"),
                CreateCategory("Category B")
            };

            var dtos = categories.Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(
                x => x.Map<List<LocationCategoryDto>>(It.IsAny<List<LocationCategory>>()),
                Times.Once);
        }

        #endregion

        #region Return Type Tests

        [Fact]
        public async Task Handle_WithCategories_ReturnsResultSuccessType()
        {
            // Arrange
            var query = CreateQuery();
            var categories = new List<LocationCategory> { CreateCategory() };
            var dtos = categories.Select(CreateCategoryDto).ToList();

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().BeOfType<PaginatedList<LocationCategoryDto>>();
        }

        [Fact]
        public async Task Handle_MappedDtosAreReturnedCorrectly()
        {
            // Arrange
            var query = CreateQuery();
            var categoryId = Guid.NewGuid();
            var category = new LocationCategory
            {
                Id = categoryId,
                Name = "Food & Drink",
                IconUrl = "https://example.com/food.png"
            };

            var dto = new LocationCategoryDto
            {
                Id = categoryId,
                Name = "Food & Drink",
                IconUrl = "https://example.com/food.png",
                LocationCount = 42
            };

            var categories = new List<LocationCategory> { category };
            var dtos = new List<LocationCategoryDto> { dto };

            SetupRepository(categories);
            SetupMapper(categories, dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(1);
            result.Value.Items[0].Id.Should().Be(categoryId);
            result.Value.Items[0].Name.Should().Be("Food & Drink");
            result.Value.Items[0].IconUrl.Should().Be("https://example.com/food.png");
            result.Value.Items[0].LocationCount.Should().Be(42);
        }

        #endregion
    }
}
