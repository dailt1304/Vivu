using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Trips.Queries.GetUserTrips;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetUserTrips
{
    public class GetUserTripsQueryHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITripLimitChecker> _tripLimitCheckerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetUserTripsQueryHandler>> _loggerMock;
        private readonly GetUserTripsQueryHandler _handler;

        public GetUserTripsQueryHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _tripLimitCheckerMock = new Mock<ITripLimitChecker>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetUserTripsQueryHandler>>();

            _handler = new GetUserTripsQueryHandler(
                _tripRepositoryMock.Object,
                _userRepositoryMock.Object,
                _tripLimitCheckerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static GetUserTripsQuery CreateValidQuery(Guid? userId = null, string? status = null, int pageNumber = 1, int pageSize = 10)
        {
            return new GetUserTripsQuery
            {
                UserId = userId ?? Guid.NewGuid(),
                Status = status,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        private static User CreateUser(bool isPremium = false)
        {
            var user = User.Create(
                email: "test@example.com",
                passwordHash: "hashedpassword",
                fullName: "Test User"
            );

            if (isPremium)
            {
                var premiumRole = new Role { Id = Guid.NewGuid(), RoleName = Role.Names.Premium };
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = premiumRole.Id, Role = premiumRole });
            }

            return user;
        }

        private static Trip CreateTrip(Guid userId, string status = "planning", string title = "Test Trip")
        {
            return Trip.Create(
                userId: userId,
                title: title,
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(5),
                isPublic: false
            );
        }

        private static TripDto CreateTripDto(Trip trip)
        {
            return new TripDto
            {
                Id = trip.Id,
                UserId = trip.UserId,
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Status = trip.Status,
                IsPublic = trip.IsPublic,
                CreatedAt = trip.CreatedDate,
                IsOwner = false // Default to false in tests
            };
        }

        private static List<Trip> CreateTrips(Guid userId, int count, string status = "planning")
        {
            var trips = new List<Trip>();
            for (int i = 0; i < count; i++)
            {
                var trip = Trip.Create(
                    userId: userId,
                    title: $"Trip {i + 1}",
                    description: $"Description {i + 1}",
                    startDate: DateTime.UtcNow.AddDays(i + 1),
                    endDate: DateTime.UtcNow.AddDays(i + 5),
                    isPublic: false
                );
                // Use reflection to set status since there's no public setter
                typeof(Trip).GetProperty("Status")!.SetValue(trip, status);
                trips.Add(trip);
            }
            return trips;
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessWithTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser(isPremium: false);
            var trips = CreateTrips(userId, 3);
            var tripsQueryable = trips.AsQueryable();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>(), It.IsAny<Action<IMappingOperationOptions>>()))
                .Returns((Trip t, Action<IMappingOperationOptions> opts) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Trips.Should().NotBeNull();
            result.Value.Trips.Items.Should().HaveCount(3);
            result.Value.NumberOfTripCreated.Should().Be(3);
            result.Value.TripLimit.Should().Be(5);

            _userRepositoryMock.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _tripRepositoryMock.Verify(x => x.GetTripsByUserIdQuery(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_PremiumUser_ReturnsCorrectTripLimit()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser(isPremium: true);
            var trips = CreateTrips(userId, 5);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(true))
                .Returns(100);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.TripLimit.Should().Be(100);
            _tripLimitCheckerMock.Verify(x => x.GetTripLimit(true), Times.Once);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ReturnsFilteredTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: "completed");
            var user = CreateUser();
            
            var planningTrips = CreateTrips(userId, 2, "planning");
            var completedTrips = CreateTrips(userId, 3, "completed");
            var allTrips = planningTrips.Concat(completedTrips).ToList();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(allTrips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(3);
            result.Value.Trips.Items.Should().OnlyContain(t => t.Status.ToLower() == "completed");
        }

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, pageNumber: 2, pageSize: 2);
            var user = CreateUser();
            var trips = CreateTrips(userId, 5);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.PageNumber.Should().Be(2);
            result.Value.Trips.PageSize.Should().Be(2);
            result.Value.Trips.TotalCount.Should().Be(5);
        }

        #endregion

        #region Empty Results Tests

        [Fact]
        public async Task Handle_NoTripsFound_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(new List<Trip>().ToAsyncQueryable());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().BeEmpty();
            result.Value.Trips.TotalCount.Should().Be(0);
            result.Value.NumberOfTripCreated.Should().Be(0);
        }

        [Fact]
        public async Task Handle_NullQueryFromRepository_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns((IQueryable<Trip>)null!);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().BeEmpty();
            result.Value.Trips.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_StatusFilterNoMatches_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: "completed");
            var user = CreateUser();
            var trips = CreateTrips(userId, 3, "planning"); // All trips are planning, not completed

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().BeEmpty();
        }

        #endregion

        #region User Not Found Tests

        [Fact]
        public async Task Handle_UserNotFound_StillReturnsTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var trips = CreateTrips(userId, 3);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false)) // Default to non-premium when user not found
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(3);
            // When user not found, should default to non-premium limit
            result.Value.TripLimit.Should().Be(5);
        }

        #endregion

        #region Status Filter Tests

        [Theory]
        [InlineData("planning")]
        [InlineData("PLANNING")]
        [InlineData("Planning")]
        public async Task Handle_StatusFilterCaseInsensitive_ReturnsFilteredTrips(string status)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: status);
            var user = CreateUser();
            var trips = CreateTrips(userId, 3, "planning");

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_WithOngoingStatus_ReturnsOngoingTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: "ongoing");
            var user = CreateUser();
            
            var planningTrips = CreateTrips(userId, 2, "planning");
            var ongoingTrips = CreateTrips(userId, 2, "ongoing");
            var completedTrips = CreateTrips(userId, 1, "completed");
            var allTrips = planningTrips.Concat(ongoingTrips).Concat(completedTrips).ToList();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(allTrips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(2);
            result.Value.Trips.Items.Should().OnlyContain(t => t.Status.ToLower() == "ongoing");
        }

        [Fact]
        public async Task Handle_NoStatusFilter_ReturnsAllTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: null);
            var user = CreateUser();
            
            var planningTrips = CreateTrips(userId, 2, "planning");
            var ongoingTrips = CreateTrips(userId, 2, "ongoing");
            var completedTrips = CreateTrips(userId, 1, "completed");
            var allTrips = planningTrips.Concat(ongoingTrips).Concat(completedTrips).ToList();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(allTrips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_EmptyStatusFilter_ReturnsAllTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, status: "");
            var user = CreateUser();
            var trips = CreateTrips(userId, 3);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.Items.Should().HaveCount(3);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_FirstPage_ReturnsCorrectPaginationInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, pageNumber: 1, pageSize: 2);
            var user = CreateUser();
            var trips = CreateTrips(userId, 5);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.PageNumber.Should().Be(1);
            result.Value.Trips.PageSize.Should().Be(2);
            result.Value.Trips.TotalCount.Should().Be(5);
            result.Value.Trips.TotalPages.Should().Be(3);
            result.Value.Trips.HasPreviousPage.Should().BeFalse();
            result.Value.Trips.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LastPage_ReturnsCorrectPaginationInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId, pageNumber: 3, pageSize: 2);
            var user = CreateUser();
            var trips = CreateTrips(userId, 5);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Trips.PageNumber.Should().Be(3);
            result.Value.Trips.HasPreviousPage.Should().BeTrue();
            result.Value.Trips.HasNextPage.Should().BeFalse();
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task Handle_TripsOrderedByCreatedDateDescending()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser();
            
            var trip1 = CreateTrip(userId, title: "Trip 1");
            var trip2 = CreateTrip(userId, title: "Trip 2");
            var trip3 = CreateTrip(userId, title: "Trip 3");
            
            // Set different created dates using reflection
            typeof(Trip).GetProperty("CreatedDate")!.SetValue(trip1, DateTime.UtcNow.AddDays(-3));
            typeof(Trip).GetProperty("CreatedDate")!.SetValue(trip2, DateTime.UtcNow.AddDays(-1));
            typeof(Trip).GetProperty("CreatedDate")!.SetValue(trip3, DateTime.UtcNow);
            
            var trips = new List<Trip> { trip1, trip2, trip3 };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(5);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            var items = result.Value.Trips.Items;
            items.Should().HaveCount(3);
            // Most recent should be first
            items[0].Title.Should().Be("Trip 3");
            items[1].Title.Should().Be("Trip 2");
            items[2].Title.Should().Be("Trip 1");
        }

        #endregion

        #region Trip Limit Tests

        [Fact]
        public async Task Handle_ReturnsCorrectTripCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = CreateValidQuery(userId);
            var user = CreateUser();
            var trips = CreateTrips(userId, 3);

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _tripLimitCheckerMock
                .Setup(x => x.GetTripCountAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(7); // Different from actual trips returned

            _tripLimitCheckerMock
                .Setup(x => x.GetTripLimit(false))
                .Returns(10);

            _tripRepositoryMock
                .Setup(x => x.GetTripsByUserIdQuery(userId))
                .Returns(trips.ToAsyncQueryable());

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.NumberOfTripCreated.Should().Be(7);
            result.Value.TripLimit.Should().Be(10);
        }

        #endregion
    }
}
