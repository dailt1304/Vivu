using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.TripMember.Command.RemoveMemberFromTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.TripMember.Command.RemoveMemberFromTrip
{
    public class RemoveMemberFromTripCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ITripMemberRepository> _memberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITripHubService> _triphubMock;
        private readonly Mock<IPublisher> _publisherMock;
        private readonly Mock<ILogger<RemoveMemberFromTripCommandHandler>> _loggerMock;
        private readonly RemoveMemberFromTripCommandHandler _handler;

        public RemoveMemberFromTripCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _memberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _triphubMock = new Mock<ITripHubService>();
            _publisherMock = new Mock<IPublisher>();
            _loggerMock = new Mock<ILogger<RemoveMemberFromTripCommandHandler>>();

            _handler = new RemoveMemberFromTripCommandHandler(
                _currentUserMock.Object,
                _tripRepositoryMock.Object,
                _userRepositoryMock.Object,
                _memberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _publisherMock.Object,
                _triphubMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithValidRequest_ShouldRemoveMemberSuccessfully()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );

            var member = User.Create(
                email: "member@test.com",
                passwordHash: "hashed_password",
                fullName: "Test Member"
            );

            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: memberId,
                ownerId: ownerId,
                role: "member"
            );

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(member.Id))
                .ReturnsAsync(member);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, member.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tripMember);
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Contain("Test Member");
            result.Value.Should().Contain("Test Trip");
            _memberRepositoryMock.Verify(x => x.Remove(tripMember), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Authentication & Authorization Tests

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNull_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns((string)null!);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsWhitespace_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns("   ");
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsInvalidGuid_ShouldReturnInvalidTokenError()
        {
            // Arrange
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_WhenOwnerNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );
            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.NotFound);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIsNotOwner_ShouldReturnNotOwnerError()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var actualOwnerId = Guid.NewGuid();
            var trip = Trip.Create(
                userId: actualOwnerId,
                title: "Test Trip"
            );
            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(currentUserId.ToString());
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(actualOwnerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.NotOwner);
        }

        [Fact]
        public async Task Handle_WhenOwnerTriesToRemoveThemselves_ShouldReturnCannotRemoveSelfError()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );
            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = ownerId // Same as owner
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.CannotRemoveSelf);
        }

        #endregion

        #region Entity Not Found Tests

        [Fact]
        public async Task Handle_WhenTripNotFound_ShouldReturnTripNotFoundError()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = new RemoveMemberFromTripCommand
            {
                TripId = tripId,
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFound);
        }

        [Fact]
        public async Task Handle_WhenMemberUserNotFound_ShouldReturnUserNotFoundError()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var memberId = Guid.NewGuid();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = memberId
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(memberId))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("User.NotFound");
            result.Error.Message.Should().Contain(memberId.ToString());
        }

        [Fact]
        public async Task Handle_WhenTripMemberRelationshipNotFound_ShouldReturnNotFoundMemberError()
        {
            // Arrange
            var ownerId = Guid.NewGuid();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );

            var member = User.Create(
                email: "member@test.com",
                passwordHash: "hashed_password",
                fullName: "Test Member"
            );

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(member.Id))
                .ReturnsAsync(member);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, member.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.TripMember)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.NotFoundMember);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ShouldCallRepositoriesInCorrectOrder()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var callOrder = new List<string>();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );

            var member = User.Create(
                email: "member@test.com",
                passwordHash: "hashed_password",
                fullName: "Test Member"
            );

            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: member.Id,
                ownerId: ownerId,
                role: "member"
            );

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip)
                .Callback(() => callOrder.Add("GetTripById"));
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId)
                .Callback(() => callOrder.Add("GetOwnerIdByTripId"));
            _userRepositoryMock.Setup(x => x.GetByIdAsync(member.Id))
                .ReturnsAsync(member)
                .Callback(() => callOrder.Add("GetUserById"));
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, member.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tripMember)
                .Callback(() => callOrder.Add("GetTripMember"));
            _memberRepositoryMock.Setup(x => x.Remove(tripMember))
                .Callback(() => callOrder.Add("Remove"));
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback(() => callOrder.Add("SaveChanges"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            callOrder.Should().ContainInOrder(
                "GetTripById",
                "GetOwnerIdByTripId",
                "GetUserById",
                "GetTripMember",
                "Remove",
                "SaveChanges"
            );
        }

        [Fact]
        public async Task Handle_ShouldNotSaveChanges_WhenValidationFails()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(command.TripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _memberRepositoryMock.Verify(x => x.Remove(It.IsAny<Domain.Entities.TripMember>()), Times.Never);
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_ShouldLogWarning_WhenUserNotAuthenticated()
        {
            // Arrange
            var command = new RemoveMemberFromTripCommand
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns((string)null!);
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("user not authenticated")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldLogWarning_WhenOwnerNotFound()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );
            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = Guid.NewGuid()
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync((Guid?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("owner not found in this trip")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_WithCancellationToken_ShouldPassTokenToRepositories()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip"
            );

            var member = User.Create(
                email: "member@test.com",
                passwordHash: "hashed_password",
                fullName: "Test Member"
            );

            var tripMember = Domain.Entities.TripMember.Create(
                tripId: trip.Id,
                userId: member.Id,
                ownerId: ownerId,
                role: "member"
            );

            var command = new RemoveMemberFromTripCommand
            {
                TripId = trip.Id,
                UserId = member.Id
            };

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _memberRepositoryMock.Setup(x => x.GetOwnerIdByTripId(trip.Id))
                .ReturnsAsync(ownerId);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
            _userRepositoryMock.Setup(x => x.GetByIdAsync(member.Id))
                .ReturnsAsync(member);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, member.Id, cancellationToken))
                .ReturnsAsync(tripMember);

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _memberRepositoryMock.Verify(
                x => x.GetByTripAndUserAsync(trip.Id, member.Id, cancellationToken),
                Times.Once);
            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(cancellationToken),
                Times.Once);
        }

        #endregion
    }
}
