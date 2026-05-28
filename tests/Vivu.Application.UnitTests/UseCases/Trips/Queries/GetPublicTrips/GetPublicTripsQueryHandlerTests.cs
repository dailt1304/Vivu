using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;

using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Trips.Queries.GetPublicTrips;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetPublicTrips
{
    public class GetPublicTripsQueryHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ICityRepository> _cityRepositoryMock;
        private readonly Mock<IPublicTripService> _publicTripServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetPublicTripsQueryHandler>> _loggerMock;
        private readonly GetPublicTripsQueryHandler _handler;

        public GetPublicTripsQueryHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _cityRepositoryMock = new Mock<ICityRepository>();
            _publicTripServiceMock = new Mock<IPublicTripService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetPublicTripsQueryHandler>>();

            _handler = new GetPublicTripsQueryHandler(
                _tripRepositoryMock.Object,
                _cityRepositoryMock.Object,
                _publicTripServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static GetPublicTripsQuery CreateQuery(
            Guid? cityId = null,
            Guid? countryId = null,
            int? duration = null,
            string sortBy = "newest",
            int pageNumber = 1,
            int pageSize = 10)
        {
            return new GetPublicTripsQuery
            {
                CityId = cityId,
                CountryId = countryId,
                Duration = duration,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static Trip CreatePublicTrip(Guid? userId = null, string title = "Public Trip", Guid? cityId = null)
        {
            var trip = Trip.Create(
                userId: userId ?? Guid.NewGuid(),
                title: title,
                description: "A public trip",
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(5),
                isPublic: true
            );
            if (cityId.HasValue)
                typeof(Trip).GetProperty("CityId")!.SetValue(trip, cityId.Value);
            return trip;
        }

        private static PublicTripDto CreatePublicTripDto(Trip trip, Guid? cityId = null)
        {
            return new PublicTripDto
            {
                Id = trip.Id,
                UserId = trip.UserId,
                Title = trip.Title,
                Description = trip.Description,
                CityId = cityId,
                Status = trip.Status,
                CreatedAt = trip.CreatedDate
            };
        }

        private void SetupRepository(List<Trip> trips)
        {
            var mockQueryable = trips.AsQueryable().ToAsyncQueryable();
            _tripRepositoryMock
                .Setup(x => x.GetPublicTripsQuery())
                .Returns(mockQueryable);
        }

        private void SetupPublicTripService(List<Trip>? filteredTrips = null)
        {
            // ApplyDurationFilter returns the same queryable by default
            _publicTripServiceMock
                .Setup(x => x.ApplyDurationFilter(It.IsAny<IQueryable<Trip>>(), It.IsAny<int>()))
                .Returns((IQueryable<Trip> q, int _) => q);

            // ApplySorting returns the same queryable by default
            _publicTripServiceMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<Trip>>(), It.IsAny<string>()))
                .Returns((IQueryable<Trip> q, string _) => q);
        }

        private void SetupMapper(List<PublicTripDto> dtos)
        {
            var queue = new Queue<PublicTripDto>(dtos);
            _mapperMock
                .Setup(x => x.Map<PublicTripDto>(It.IsAny<object>(), It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns(() => queue.Count > 0 ? queue.Dequeue() : new PublicTripDto());
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithPublicTrips_ReturnsSuccessWithPaginatedList()
        {
            // Arrange
            var trips = new List<Trip>
            {
                CreatePublicTrip(title: "Trip A"),
                CreatePublicTrip(title: "Trip B"),
                CreatePublicTrip(title: "Trip C"),
            };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery();

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

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
        public async Task Handle_WithNoTrips_ReturnsEmptyPaginatedList()
        {
            // Arrange
            var query = CreateQuery();
            SetupRepository(new List<Trip>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithNullQueryFromRepository_ReturnsEmptyPaginatedList()
        {
            // Arrange
            var query = CreateQuery();
            _tripRepositoryMock
                .Setup(x => x.GetPublicTripsQuery())
                .Returns((IQueryable<Trip>)null!);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region City Filter Tests

        [Fact]
        public async Task Handle_WithValidCityId_ChecksCityExistsAndFilters()
        {
            // Arrange
            var cityId = Guid.NewGuid();
            var city = new City { Id = cityId, Name = "Ho Chi Minh City" };
            var trips = new List<Trip>
            {
                CreatePublicTrip(title: "Trip in HCM", cityId: cityId),
                CreatePublicTrip(title: "Trip in HCM 2", cityId: cityId),
            };
            var dtos = trips.Select(t => CreatePublicTripDto(t, cityId)).ToList();
            var query = CreateQuery(cityId: cityId);

            _cityRepositoryMock.Setup(x => x.GetByIdAsync(cityId)).ReturnsAsync(city);
            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _cityRepositoryMock.Verify(x => x.GetByIdAsync(cityId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidCityId_ReturnsFailure()
        {
            // Arrange
            var cityId = Guid.NewGuid();
            var query = CreateQuery(cityId: cityId);

            _cityRepositoryMock.Setup(x => x.GetByIdAsync(cityId)).ReturnsAsync((City?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithNoCityId_DoesNotCheckCity()
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(cityId: null);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _cityRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Duration Filter Tests

        [Fact]
        public async Task Handle_WithDurationFilter_CallsApplyDurationFilter()
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(duration: 5);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _publicTripServiceMock.Verify(
                x => x.ApplyDurationFilter(It.IsAny<IQueryable<Trip>>(), 5),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithNoDurationFilter_DoesNotCallApplyDurationFilter()
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(duration: null);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _publicTripServiceMock.Verify(
                x => x.ApplyDurationFilter(It.IsAny<IQueryable<Trip>>(), It.IsAny<int>()),
                Times.Never);
        }

        #endregion

        #region Sorting Tests

        [Theory]
        [InlineData("newest")]
        [InlineData("popular")]
        [InlineData("trending")]
        public async Task Handle_WithSortBy_CallsApplySorting(string sortBy)
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(sortBy: sortBy);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _publicTripServiceMock.Verify(
                x => x.ApplySorting(It.IsAny<IQueryable<Trip>>(), sortBy),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Always_CallsApplySortingExactlyOnce()
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery();

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _publicTripServiceMock.Verify(
                x => x.ApplySorting(It.IsAny<IQueryable<Trip>>(), It.IsAny<string>()),
                Times.Once);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var trips = Enumerable.Range(1, 5)
                .Select(i => CreatePublicTrip(title: $"Trip {i}"))
                .ToList();
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task Handle_FirstPage_HasNoPreviousPage()
        {
            // Arrange
            var trips = Enumerable.Range(1, 5).Select(i => CreatePublicTrip(title: $"Trip {i}")).ToList();
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery(pageNumber: 1, pageSize: 2);

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.HasPreviousPage.Should().BeFalse();
            result.Value.HasNextPage.Should().BeTrue();
        }

        #endregion

        #region Return Type Tests

        [Fact]
        public async Task Handle_ReturnsCorrectResultType()
        {
            // Arrange
            var trips = new List<Trip> { CreatePublicTrip() };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery();

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType<PaginatedList<PublicTripDto>>();
        }

        [Fact]
        public async Task Handle_WithTrips_MapsAllTripsToDto()
        {
            // Arrange
            var trips = new List<Trip>
            {
                CreatePublicTrip(title: "A"),
                CreatePublicTrip(title: "B"),
                CreatePublicTrip(title: "C"),
            };
            var dtos = trips.Select(t => CreatePublicTripDto(t)).ToList();
            var query = CreateQuery();

            SetupRepository(trips);
            SetupPublicTripService();
            SetupMapper(dtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert – all 3 trips are returned = mapper was called for each
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
        }

        #endregion
    }
}
