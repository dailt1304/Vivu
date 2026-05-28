using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.UpdateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateTrip
{
    public class UpdateTripCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<UpdateTripCommandHandler>> _loggerMock;
        private readonly UpdateTripCommandHandler _handler;

        public UpdateTripCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<UpdateTripCommandHandler>>();

            _handler = new UpdateTripCommandHandler(
                _tripRepositoryMock.Object,
                _tripDayRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Helper Methods

        private static UpdateTripCommand CreateValidCommand(Guid tripId)
        {
            return new UpdateTripCommand
            {
                TripId = tripId,
                Title = "Updated Trip",
                Description = "Updated Description",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(5),
                Status = "planning",
                CoverUrl = "https://example.com/cover.jpg",
                TripSize = 4
            };
        }

        private static Trip CreateTrip(Guid userId, Guid? tripId = null)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Original Trip",
                description: "Original Description",
                startDate: DateTime.UtcNow.AddDays(2),
                endDate: DateTime.UtcNow.AddDays(6),
                isPublic: false
            );

            if (tripId.HasValue)
            {
                typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId.Value);
            }

            return trip;
        }

        private static Domain.Entities.TripMember CreateTripMember(Guid tripId, Guid userId, string role = "organizer")
        {
            return new Domain.Entities.TripMember
            {
                TripId = tripId,
                UserId = userId,
                OwnerId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };
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
                CreatedAt = trip.CreatedDate
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessWithUpdatedTrip()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId);
            var trip = CreateTrip(userId, tripId);
            var member = CreateTripMember(tripId, userId);
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);
            
            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be(command.Title);
            result.Value.Description.Should().Be(command.Description);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateWithPartialData_UpdatesOnlyProvidedFields()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UpdateTripCommand
            {
                TripId = tripId,
                Title = "New Title Only",
                Status = "ongoing"
            };
            var trip = CreateTrip(userId, tripId);
            var member = CreateTripMember(tripId, userId);
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Title.Should().Be("New Title Only");
            result.Value.Status.Should().Be("ongoing");
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _tripRepositoryMock.Verify(
                x => x.GetTripWithMembersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_MalformedUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Trip Not Found Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task Handle_UserNotOwnerOrOrganizer_ReturnsFailure()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId);
            var trip = CreateTrip(ownerId, tripId);
            var member = CreateTripMember(tripId, ownerId, "organizer");
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(otherUserId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UserIsViewer_ReturnsFailure()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var viewerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId);
            var trip = CreateTrip(ownerId, tripId);
            
            var ownerMember = CreateTripMember(tripId, ownerId, "organizer");
            var viewerMember = CreateTripMember(tripId, viewerId, "viewer");
            trip.TripMembers.Add(ownerMember);
            trip.TripMembers.Add(viewerMember);

            _currentUserMock.Setup(x => x.Id).Returns(viewerId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        [Fact]
        public async Task Handle_UserIsOrganizer_CanUpdate()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var organizerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId);
            var trip = CreateTrip(ownerId, tripId);
            
            var ownerMember = CreateTripMember(tripId, ownerId, "organizer");
            var organizerMember = CreateTripMember(tripId, organizerId, "organizer");
            trip.TripMembers.Add(ownerMember);
            trip.TripMembers.Add(organizerMember);

            _currentUserMock.Setup(x => x.Id).Returns(organizerId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task Handle_InvalidDateRange_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UpdateTripCommand
            {
                TripId = tripId,
                Title = "Test Trip",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(2), // End before start
                Status = "planning"
            };
            var trip = CreateTrip(userId, tripId);
            var member = CreateTripMember(tripId, userId);
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.DateInvalid);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SameDateForStartAndEnd_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var sameDate = DateTime.UtcNow.AddDays(5);
            var command = new UpdateTripCommand
            {
                TripId = tripId,
                Title = "Test Trip",
                StartDate = sameDate,
                EndDate = sameDate,
                Status = "planning"
            };
            var trip = CreateTrip(userId, tripId);
            var member = CreateTripMember(tripId, userId);
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.DateInvalid);
        }

        #endregion

        #region Status Update Tests

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        [InlineData("completed")]
        public async Task Handle_UpdateStatus_UpdatesSuccessfully(string newStatus)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new UpdateTripCommand
            {
                TripId = tripId,
                Title = "Test Trip",
                Status = newStatus
            };
            var trip = CreateTrip(userId, tripId);
            var member = CreateTripMember(tripId, userId);
            trip.TripMembers.Add(member);

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripRepositoryMock
                .Setup(x => x.GetTripWithMembersAsync(tripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);

            _tripDayRepositoryMock
                .Setup(x => x.GetByTripIdAsync(tripId))
                .ReturnsAsync(new List<TripDay>());

            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .ReturnsAsync((TripDay td) => td);

            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns((Trip t) => CreateTripDto(t));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Status.Should().Be(newStatus);
        }

        #endregion
    }
}
