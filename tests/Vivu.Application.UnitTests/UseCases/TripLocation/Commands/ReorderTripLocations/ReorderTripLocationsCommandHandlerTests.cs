using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.ReorderTripLocations
{
    public class ReorderTripLocationsCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ReorderTripLocationHandler>> _loggerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly ReorderTripLocationHandler _handler;

        public ReorderTripLocationsCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ReorderTripLocationHandler>>();
            _currentUserMock = new Mock<ICurrentUser>();

            _handler = new ReorderTripLocationHandler(
                _tripDayRepositoryMock.Object,
                _tripLocationRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _currentUserMock.Object);
        }

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_UserIdIsNull_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region TripDay Validation Tests

        [Fact]
        public async Task Handle_TripDayNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync((TripDay?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.TripDay.NotFoundById(tripDayId));
        }

        #endregion

        #region Access Control Tests

        [Fact]
        public async Task Handle_TripOwnerNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_UserNotTripOwner_ReturnsAccessDeniedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        #endregion

        #region TripLocation Validation Tests

        [Fact]
        public async Task Handle_NoLocationsFoundForTripDay_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(new List<Domain.Entities.TripLocation>());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.TripLocation.NotFound);
        }

        [Fact]
        public async Task Handle_InvalidTripLocationIds_ReturnsInvalidIdsError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();
            var invalidId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation1.Id, tripLocation2.Id, invalidId }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.TripLocation.InvalidIds);
        }

        [Fact]
        public async Task Handle_MismatchLocationCount_ReturnsInvalidCountError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation1.Id } // Missing tripLocation2
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(DomainErrors.TripLocation.InvalidCount);
        }

        #endregion

        #region Successful Reordering Tests

        [Fact]
        public async Task Handle_ValidCommand_ReordersAndReturnsLocations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();
            var locationId3 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocation3 = Domain.Entities.TripLocation.Create(tripDayId, locationId3, 2);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2, tripLocation3 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation3.Id, tripLocation1.Id, tripLocation2.Id }
            };

            var responses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation3.Id, OrderIndex = 1 },
                new TripLocationResponse { Id = tripLocation1.Id, OrderIndex = 2 },
                new TripLocationResponse { Id = tripLocation2.Id, OrderIndex = 3 }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(responses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(3);
            result.Value[0].Id.Should().Be(tripLocation3.Id);
            result.Value[1].Id.Should().Be(tripLocation1.Id);
            result.Value[2].Id.Should().Be(tripLocation2.Id);
        }

        [Fact]
        public async Task Handle_ValidCommand_UpdatesOrderIndicesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };

            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation2.Id, tripLocation1.Id }
            };

            var responses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation2.Id, OrderIndex = 1 },
                new TripLocationResponse { Id = tripLocation1.Id, OrderIndex = 2 }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(responses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            tripLocation2.OrderIndex.Should().Be(1);
            tripLocation1.OrderIndex.Should().Be(2);
            _tripLocationRepositoryMock.Verify(x => x.Update(tripLocation2), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.Update(tripLocation1), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_CallsRepositoriesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation1.Id }
            };

            var responses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation1.Id, OrderIndex = 1 }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(responses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripDayRepositoryMock.Verify(x => x.GetByIdAsync(tripDayId), Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.GetOwnerIdByTripId(tripId), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.GetTripLocationsByTripDayIdAsync(tripDayId), Times.Once);
            _tripLocationRepositoryMock.Verify(x => x.Update(It.IsAny<Domain.Entities.TripLocation>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SingleLocation_MaintainsOrderIndexOne()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation = Domain.Entities.TripLocation.Create(tripDayId, locationId, 5);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation.Id }
            };

            var responses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation.Id, OrderIndex = 1 }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(responses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            tripLocation.OrderIndex.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ValidCommand_MapsResponseCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation2.Id, tripLocation1.Id }
            };

            var expectedResponses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation2.Id, OrderIndex = 1, LocationName = "Location 2" },
                new TripLocationResponse { Id = tripLocation1.Id, OrderIndex = 2, LocationName = "Location 1" }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(expectedResponses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(expectedResponses);
            _mapperMock.Verify(x => x.Map<List<TripLocationResponse>>(
                It.Is<List<Domain.Entities.TripLocation>>(list => 
                    list.Count == 2 && 
                    list[0].Id == tripLocation2.Id && 
                    list[1].Id == tripLocation1.Id)), 
                Times.Once);
        }

        [Fact]
        public async Task Handle_ReversesOrderOfThreeLocations_UpdatesCorrectly()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripDayId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var locationId1 = Guid.NewGuid();
            var locationId2 = Guid.NewGuid();
            var locationId3 = Guid.NewGuid();

            var tripDay = new TripDay { Id = tripDayId, TripId = tripId };
            var tripLocation1 = Domain.Entities.TripLocation.Create(tripDayId, locationId1, 0);
            var tripLocation2 = Domain.Entities.TripLocation.Create(tripDayId, locationId2, 1);
            var tripLocation3 = Domain.Entities.TripLocation.Create(tripDayId, locationId3, 2);
            var tripLocations = new List<Domain.Entities.TripLocation> { tripLocation1, tripLocation2, tripLocation3 };

            var command = new ReorderTripLocationsCommand
            {
                TripDayId = tripDayId,
                OrderedTripLocationIds = new List<Guid> { tripLocation3.Id, tripLocation2.Id, tripLocation1.Id }
            };

            var responses = new List<TripLocationResponse>
            {
                new TripLocationResponse { Id = tripLocation3.Id, OrderIndex = 1 },
                new TripLocationResponse { Id = tripLocation2.Id, OrderIndex = 2 },
                new TripLocationResponse { Id = tripLocation1.Id, OrderIndex = 3 }
            };

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripDayRepositoryMock.Setup(x => x.GetByIdAsync(tripDayId))
                .ReturnsAsync(tripDay);
            _tripMemberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(userId);
            _tripLocationRepositoryMock.Setup(x => x.GetTripLocationsByTripDayIdAsync(tripDayId))
                .ReturnsAsync(tripLocations);
            _mapperMock.Setup(x => x.Map<List<TripLocationResponse>>(It.IsAny<List<Domain.Entities.TripLocation>>()))
                .Returns(responses);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            tripLocation3.OrderIndex.Should().Be(1);
            tripLocation2.OrderIndex.Should().Be(2);
            tripLocation1.OrderIndex.Should().Be(3);
            result.Value[0].Id.Should().Be(tripLocation3.Id);
            result.Value[1].Id.Should().Be(tripLocation2.Id);
            result.Value[2].Id.Should().Be(tripLocation1.Id);
        }

        #endregion
    }
}
