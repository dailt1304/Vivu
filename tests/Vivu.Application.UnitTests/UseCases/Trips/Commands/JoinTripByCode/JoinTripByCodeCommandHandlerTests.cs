using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.Trips.Commands.JoinTripByCode;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.JoinTripByCode
{
    public class JoinTripByCodeCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _memberRepositoryMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITripHubService> _triphubMock;
        private readonly Mock<IPublisher> _publisherMock;
        private readonly Mock<ILogger<JoinTripByCodeCommandHandler>> _loggerMock;
        private readonly JoinTripByCodeCommandHandler _handler;

        public JoinTripByCodeCommandHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _memberRepositoryMock = new Mock<ITripMemberRepository>();
            _currentUserMock = new Mock<ICurrentUser>();
            _triphubMock = new Mock<ITripHubService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _mapperMock = new Mock<IMapper>();
            _publisherMock = new Mock<IPublisher>();
            _loggerMock = new Mock<ILogger<JoinTripByCodeCommandHandler>>();

            _handler = new JoinTripByCodeCommandHandler(
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _tripRepositoryMock.Object,
                _memberRepositoryMock.Object,
                _userRepositoryMock.Object,
                _mapperMock.Object,
                _publisherMock.Object,
                _triphubMock.Object,
                _loggerMock.Object
            );
        }

        #region Success Scenarios

        [Fact]
        public async Task Handle_ValidInviteCodeAndUser_ReturnsSuccessWithTripMemberDto()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var tripMemberDto = CreateTripMemberDto();

            SetupSuccessfulJoin(user.Id, command.InviteCode, user, trip, tripMemberDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().Be(tripMemberDto);

            // Verify all dependencies called
            _tripRepositoryMock.Verify(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>()), Times.Once);
            _memberRepositoryMock.Verify(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>()), Times.Once);
            _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesTripMemberWithCorrectData()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var tripMemberDto = CreateTripMemberDto();
            Domain.Entities.TripMember capturedMember = null;

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.TripMember)null);

            _memberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()))
                .Callback<Domain.Entities.TripMember>(m => capturedMember = m)
                .ReturnsAsync(new Domain.Entities.TripMember());

            _mapperMock.Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>())).Returns(tripMemberDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedMember.Should().NotBeNull();
            capturedMember!.TripId.Should().Be(trip.Id);
            capturedMember.UserId.Should().Be(user.Id);
            capturedMember.OwnerId.Should().Be(trip.UserId);
            capturedMember.Role.Should().Be("viewer");
        }

        [Fact]
        public async Task Handle_ValidRequest_AssociatesUserAndTripToMember()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var tripMemberDto = CreateTripMemberDto();
            Domain.Entities.TripMember capturedMember = null;

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.TripMember)null);

            _memberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()))
                .Callback<Domain.Entities.TripMember>(m => capturedMember = m)
                .ReturnsAsync(new Domain.Entities.TripMember());

            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Callback<object>(obj => 
                {
                    var m = (Domain.Entities.TripMember)obj;

                    m.User.Should().Be(user);
                    m.Trip.Should().Be(trip);
                })
                .Returns(tripMemberDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mapperMock.Verify(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NewMember_SetsRoleAsViewer()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var tripMemberDto = CreateTripMemberDto();
            Domain.Entities.TripMember capturedMember = null;

            SetupSuccessfulJoin(user.Id, command.InviteCode, user, trip, tripMemberDto);

            _memberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()))
                .Callback<Domain.Entities.TripMember>(m => capturedMember = m)
                .ReturnsAsync(new Domain.Entities.TripMember());

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedMember.Should().NotBeNull();
            capturedMember!.Role.Should().Be("viewer");
        }

        #endregion

        #region Failure Scenarios - Authentication

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenFailure()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("ABC123");

            _currentUserMock.Setup(x => x.Id).Returns((string)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            // Verify no operations performed
            _tripRepositoryMock.Verify(x => x.GetByInviteCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenFailure()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("ABC123");

            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidGuidUserId_ReturnsInvalidTokenFailure()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("ABC123");

            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_UserNotFoundInDatabase_ReturnsUserNotFoundFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new JoinTripByCodeCommand("ABC123");

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.User.NotFound);

            // Verify no trip operations
            _tripRepositoryMock.Verify(x => x.GetByInviteCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Failure Scenarios - Trip

        [Fact]
        public async Task Handle_InvalidInviteCode_ReturnsInvalidInviteCodeFailure()
        {
            var command = new JoinTripByCodeCommand("INVALID123");
            var user = CreateUser();

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync((Trip)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.InvalidInviteCode);

            // Verify no member added
            _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Failure Scenarios - Membership

        [Fact]
        public async Task Handle_UserAlreadyMember_ReturnsAlreadyMemberFailure()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var existingMember = CreateTripMember(trip.Id, user.Id);

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existingMember);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AlreadyMember);

            // Verify no new member added
            _memberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_CancellationRequested_ThrowsOperationCanceledException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new JoinTripByCodeCommand("ABC123");
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ThrowsAsync(new OperationCanceledException());

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(command, cancellationTokenSource.Token));
        }

        [Theory]
        [InlineData("ABC123")]
        [InlineData("XYZ789")]
        [InlineData("CODE-2024")]
        public async Task Handle_VariousInviteCodes_HandlesCorrectly(string inviteCode)
        {
            var command = new JoinTripByCodeCommand(inviteCode);
            var user = CreateUser();
            var trip = CreateTrip();
            trip.InviteCode = inviteCode;
            var tripMemberDto = CreateTripMemberDto();

            SetupSuccessfulJoin(user.Id, inviteCode, user, trip, tripMemberDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _tripRepositoryMock.Verify(x => x.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DatabaseSaveThrowsException_PropagatesException()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.TripMember)null);
            _memberRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>())).ReturnsAsync(new Domain.Entities.TripMember()); ;

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task Handle_InvalidUserId_LogsWarning()
        {
            // Arrange
            var command = new JoinTripByCodeCommand("ABC123");
            _currentUserMock.Setup(x => x.Id).Returns((string)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Invalid or missing user ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Verification Tests

        [Fact]
        public async Task Handle_SuccessfulJoin_CallsAllRepositoriesInCorrectOrder()
        {
            var command = new JoinTripByCodeCommand("ABC123");
            var user = CreateUser();
            var trip = CreateTrip();
            var tripMemberDto = CreateTripMemberDto();
            var callSequence = new List<string>();

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(user.Id))
                .Callback(() => callSequence.Add("UserRepository"))
                .ReturnsAsync(user);

            _tripRepositoryMock
                .Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>()))
                .Callback(() => callSequence.Add("TripRepository"))
                .ReturnsAsync(trip);

            _memberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(trip.Id, user.Id, It.IsAny<CancellationToken>()))
                .Callback(() => callSequence.Add("CheckMembership"))
                .ReturnsAsync((Domain.Entities.TripMember)null);

            _memberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>()))
                .Callback(() => callSequence.Add("AddMember"))
                .ReturnsAsync(new Domain.Entities.TripMember()); ;

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callSequence.Add("SaveChanges"))
                .ReturnsAsync(1);

            _mapperMock.Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>())).Returns(tripMemberDto);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            callSequence.Should().ContainInOrder(
                "UserRepository",
                "TripRepository",
                "CheckMembership",
                "AddMember",
                "SaveChanges"
            );
        }

        [Fact]
        public async Task Handle_FailureScenario_DoesNotSaveToDatabase()
        {
            var command = new JoinTripByCodeCommand("INVALID");
            var user = CreateUser();

            _currentUserMock.Setup(x => x.Id).Returns(user.Id.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(command.InviteCode, It.IsAny<CancellationToken>())).ReturnsAsync((Trip)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Helper Methods

        private void SetupSuccessfulJoin(Guid userId, string inviteCode, User user, Trip trip, TripMemberDto tripMemberDto)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
            _tripRepositoryMock.Setup(x => x.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>())).ReturnsAsync(trip);
            _memberRepositoryMock.Setup(x => x.GetByTripAndUserAsync(trip.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync((Domain.Entities.TripMember)null);
            _memberRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TripMember>())).ReturnsAsync(new Domain.Entities.TripMember()); ;
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mapperMock.Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>())).Returns(tripMemberDto);
        }


        private User CreateUser()
        {
            var user = User.Create(
                email: "user@example.com",
                passwordHash: "$2a$11$hashedpassword",
                fullName: "Test User",
                avatarUrl: "https://example.com/avatar.jpg"
            );

            user.IsEmailVerified = true;
            user.Status = "active";
            user.CreatedDate = DateTime.UtcNow.AddMonths(-1);
            user.LastLoginAt = DateTime.UtcNow.AddDays(-1);

            user.UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    UserId = user.Id, 
                    RoleId = Guid.NewGuid(),
                    Role = new Role
                    {
                        Id = Guid.NewGuid(),
                        RoleName = "User"
                    }
                }
            };

            return user;
        }

        private Trip CreateTrip()
        {
            return Trip.Create(
                userId: Guid.NewGuid(),
                title: "Test Trip",
                inviteCode: "ABC123",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(10)
            );
        }

        private Domain.Entities.TripMember CreateTripMember(Guid tripId, Guid userId)
        {
            return new Domain.Entities.TripMember
            {
                TripId = tripId,
                UserId = userId,
                Role = "viewer",
                JoinedAt = DateTime.UtcNow
            };
        }

        private TripMemberDto CreateTripMemberDto()
        {
            return new TripMemberDto
            {
                TripId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Role = "viewer",
                JoinedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
