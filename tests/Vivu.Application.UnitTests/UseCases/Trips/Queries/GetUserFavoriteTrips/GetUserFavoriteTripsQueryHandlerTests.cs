using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.TripFavourites;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetUserFavoriteTrips
{
    public class GetUserFavoriteTripsQueryHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripFavoriteRepository> _tripFavoriteRepositoryMock;
        private readonly Mock<IFilterTripFavourite> _filterTripFavouriteMock;
        private readonly Mock<ILogger<GetUserFavoriteTripsQueryHandler>> _loggerMock;
        private readonly GetUserFavoriteTripsQueryHandler _handler;

        public GetUserFavoriteTripsQueryHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _tripFavoriteRepositoryMock = new Mock<ITripFavoriteRepository>();
            _filterTripFavouriteMock = new Mock<IFilterTripFavourite>();
            _loggerMock = new Mock<ILogger<GetUserFavoriteTripsQueryHandler>>();

            // AutoMapper is not needed — handler uses manual projection via LINQ Select
            _handler = new GetUserFavoriteTripsQueryHandler(
                mapper: null!,
                logger: _loggerMock.Object,
                filterTripFavourite: _filterTripFavouriteMock.Object,
                tripFavoriteRepository: _tripFavoriteRepositoryMock.Object,
                currentUserService: _currentUserMock.Object
            );
        }

        #region Helpers

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private static GetUserFavoriteTripsQuery CreateQuery(int pageNumber = 1, int pageSize = 10, string? sortColumn = null, bool sortDescending = false)
        {
            return new GetUserFavoriteTripsQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortColumn = sortColumn,
                SortDescending = sortDescending
            };
        }

        private static TripFavorite CreateFavoriteWithTrip(Guid userId, string tripTitle = "Test Trip", bool isPublic = true)
        {
            var owner = User.Create("owner@test.com", "hash", "Trip Owner");
            var trip = Trip.Create(
                userId: owner.Id,
                title: tripTitle,
                description: "A test trip",
                isPublic: isPublic
            );
            trip.User = owner;

            var favorite = new TripFavorite
            {
                UserId = userId,
                TripId = trip.Id,
                CreatedAt = DateTime.UtcNow,
                Trip = trip
            };
            return favorite;
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNull_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _tripFavoriteRepositoryMock.Verify(x => x.GetUserTripFavourite(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsEmpty_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsInvalidGuid_ShouldReturnInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Empty Result Tests

        [Fact]
        public async Task Handle_WhenUserHasNoFavorites_ShouldReturnEmptyPaginatedList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery();

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(new List<TripFavorite>().ToAsyncQueryable());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
            _filterTripFavouriteMock.Verify(
                x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), It.IsAny<GetUserFavoriteTripsQuery>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUserHasNoFavorites_ShouldReturnCorrectPaginationInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery(pageNumber: 1, pageSize: 10);

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(new List<TripFavorite>().ToAsyncQueryable());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WhenUserHasFavorites_ShouldReturnPaginatedTrips()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery(pageNumber: 1, pageSize: 10);

            var favorites = new List<TripFavorite>
            {
                CreateFavoriteWithTrip(userId, "Trip A"),
                CreateFavoriteWithTrip(userId, "Trip B"),
                CreateFavoriteWithTrip(userId, "Trip C")
            };
            var asyncFavorites = favorites.ToAsyncQueryable();

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(asyncFavorites);
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task Handle_ShouldMapTripTitlesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery();

            var favorites = new List<TripFavorite>
            {
                CreateFavoriteWithTrip(userId, "My Favourite Trip")
            };
            var asyncFavorites = favorites.ToAsyncQueryable();

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(asyncFavorites);
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().ContainSingle();
            result.Value.Items[0].Title.Should().Be("My Favourite Trip");
        }

        [Fact]
        public async Task Handle_ShouldSetOwnerIdCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery();

            var owner = User.Create("owner@test.com", "hash", "Trip Owner");
            var trip = Trip.Create(userId: owner.Id, title: "Test Trip", isPublic: true);
            trip.User = owner;

            var favorites = new List<TripFavorite>
            {
                new TripFavorite { UserId = userId, TripId = trip.Id, CreatedAt = DateTime.UtcNow, Trip = trip }
            };

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(favorites.ToAsyncQueryable());
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items[0].OwnerId.Should().Be(owner.Id);
        }

        [Fact]
        public async Task Handle_WhenOwnerHasNoProfile_ShouldUseEmailAsOwnerName()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery();

            var owner = User.Create("owner@test.com", "hash");
            owner.UserProfile = null; // explicitly nullify to test email fallback
            var trip = Trip.Create(userId: owner.Id, title: "Test Trip", isPublic: true);
            trip.User = owner;

            var favorites = new List<TripFavorite>
            {
                new TripFavorite { UserId = userId, TripId = trip.Id, CreatedAt = DateTime.UtcNow, Trip = trip }
            };

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(favorites.ToAsyncQueryable());
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items[0].OwnerName.Should().Be("owner@test.com");
            result.Value.Items[0].OwnerAvatar.Should().BeNull();
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_WithPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            var favorites = new List<TripFavorite>
            {
                CreateFavoriteWithTrip(userId, "Trip 1"),
                CreateFavoriteWithTrip(userId, "Trip 2"),
                CreateFavoriteWithTrip(userId, "Trip 3"),
                CreateFavoriteWithTrip(userId, "Trip 4"),
                CreateFavoriteWithTrip(userId, "Trip 5")
            };

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(favorites.ToAsyncQueryable());
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(5);
            result.Value.Items.Should().HaveCount(2);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
        }

        [Fact]
        public async Task Handle_LastPage_ShouldReturnRemainingItems()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery(pageNumber: 2, pageSize: 3);

            var favorites = new List<TripFavorite>
            {
                CreateFavoriteWithTrip(userId, "Trip 1"),
                CreateFavoriteWithTrip(userId, "Trip 2"),
                CreateFavoriteWithTrip(userId, "Trip 3"),
                CreateFavoriteWithTrip(userId, "Trip 4")
            };

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(favorites.ToAsyncQueryable());
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.TotalCount.Should().Be(4);
            result.Value.Items.Should().HaveCount(1);
        }

        #endregion

        #region Sorting Tests

        [Fact]
        public async Task Handle_ShouldCallApplySorting_WhenFavoritesExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery(sortColumn: "title", sortDescending: false);

            var favorites = new List<TripFavorite>
            {
                CreateFavoriteWithTrip(userId, "Trip A")
            };

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(favorites.ToAsyncQueryable());
            _filterTripFavouriteMock
                .Setup(x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query))
                .Returns((IQueryable<TripFavorite> q, GetUserFavoriteTripsQuery r) => q);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _filterTripFavouriteMock.Verify(
                x => x.ApplySorting(It.IsAny<IQueryable<TripFavorite>>(), query),
                Times.Once);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ShouldQueryRepositoryWithCorrectUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var query = CreateQuery();

            _tripFavoriteRepositoryMock
                .Setup(x => x.GetUserTripFavourite(userId))
                .Returns(new List<TripFavorite>().ToAsyncQueryable());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _tripFavoriteRepositoryMock.Verify(x => x.GetUserTripFavourite(userId), Times.Once);
        }

        #endregion
    }
}
