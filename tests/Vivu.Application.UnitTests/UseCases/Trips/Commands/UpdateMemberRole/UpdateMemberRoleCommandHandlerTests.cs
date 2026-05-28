using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateMemberRole
{
    public class UpdateMemberRoleCommandHandlerTests
    {
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<UpdateMemberRoleCommandHandler>> _loggerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly UpdateMemberRoleCommandHandler _handler;

        public UpdateMemberRoleCommandHandlerTests()
        {
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<UpdateMemberRoleCommandHandler>>();
            _mapperMock = new Mock<IMapper>();

            _handler = new UpdateMemberRoleCommandHandler(
                _tripMemberRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object,
                _mapperMock.Object
            );
        }

        #region Helper Methods

        private static UpdateMemberRoleCommand CreateValidCommand(Guid tripId, Guid memberUserId, TripRole newRole = TripRole.Editor)
        {
            return new UpdateMemberRoleCommand
            {
                TripId = tripId,
                MemberUserId = memberUserId,
                NewRole = newRole
            };
        }

        private static Domain.Entities.TripMember CreateTripMember(Guid tripId, Guid userId, Guid ownerId, string role = "viewer")
        {
            var user = Domain.Entities.User.Create(
                email: "test@example.com",
                passwordHash: "hash"
            );
            typeof(Domain.Entities.User).GetProperty("Id")!.SetValue(user, userId);

            var userProfile = new Domain.Entities.UserProfile
            {
                UserId = userId,
                FullName = "Test User"
            };
            typeof(Domain.Entities.User).GetProperty("UserProfile")!.SetValue(user, userProfile);

            return new Domain.Entities.TripMember
            {
                TripId = tripId,
                UserId = userId,
                OwnerId = ownerId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                User = user
            };
        }

        private static TripMemberDto CreateTripMemberDto(Domain.Entities.TripMember member)
        {
            return new TripMemberDto
            {
                UserId = member.UserId,
                TripId = member.TripId,
                Role = member.Role,
                FullName = member.User.UserProfile?.FullName ?? "",
                Email = member.User.Email,
                JoinedAt = member.JoinedAt
            };
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessWithUpdatedMember()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Editor);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "viewer");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns((Domain.Entities.TripMember m) => CreateTripMemberDto(m));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(memberUserId);
            result.Value.Role.Should().Be("editor");

            _tripMemberRepositoryMock.Verify(x => x.Update(It.IsAny<Domain.Entities.TripMember>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateToViewer_UpdatesRoleSuccessfully()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Viewer);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "editor");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns((Domain.Entities.TripMember m) => CreateTripMemberDto(m));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            member.Role.Should().Be("viewer");
        }

        [Fact]
        public async Task Handle_UpdateToEditor_UpdatesRoleSuccessfully()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Editor);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "viewer");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns((Domain.Entities.TripMember m) => CreateTripMemberDto(m));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            member.Role.Should().Be("editor");
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task Handle_UserNotAuthenticated_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsFailure()
        {
            // Arrange
            var command = CreateValidCommand(Guid.NewGuid(), Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Handle_TripNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, Guid.NewGuid());

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync((Guid?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_UserIsNotOwner_ReturnsAccessDenied()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, Guid.NewGuid());

            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        #endregion

        #region Role Change Validation Tests

        [Fact]
        public async Task Handle_ChangeRoleToOwner_ReturnsInvalidRoleChange()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Owner);

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.InvalidRoleChange);
        }

        #endregion

        #region Member Not Found Tests

        [Fact]
        public async Task Handle_MemberNotFound_ReturnsFailure()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId);

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.TripMember)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.TripMember.NotFound);
        }

        #endregion

        #region Repository Interaction Tests

        [Fact]
        public async Task Handle_ValidRequest_CallsRepositoryMethods()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId);
            var member = CreateTripMember(tripId, memberUserId, ownerId);

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns(CreateTripMemberDto(member));

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _tripMemberRepositoryMock.Verify(x => x.GetOwnerIdByTripId(tripId), Times.Once);
            _tripMemberRepositoryMock.Verify(
                x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()), 
                Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.Update(member), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesMemberRoleToLowerCase()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Editor);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "viewer");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns((Domain.Entities.TripMember m) => CreateTripMemberDto(m));

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            member.Role.Should().Be("editor");
            member.Role.Should().NotBe("Editor");
            member.Role.Should().NotBe("EDITOR");
        }

        #endregion

        #region Mapping Tests

        [Fact]
        public async Task Handle_ValidRequest_MapsToTripMemberDto()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId);
            var member = CreateTripMember(tripId, memberUserId, ownerId);
            var expectedDto = CreateTripMemberDto(member);

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(member))
                .Returns(expectedDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value.Should().Be(expectedDto);
            _mapperMock.Verify(x => x.Map<TripMemberDto>(member), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_OwnerUpdatingOwnRole_ReturnsSuccess()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, ownerId, TripRole.Editor);
            var member = CreateTripMember(tripId, ownerId, ownerId, "owner");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, ownerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns(CreateTripMemberDto(member));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            member.Role.Should().Be("editor");
        }

        [Fact]
        public async Task Handle_ChangingFromEditorToViewer_ReturnsSuccess()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Viewer);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "editor");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns(CreateTripMemberDto(member));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            member.Role.Should().Be("viewer");
        }

        [Fact]
        public async Task Handle_ChangingSameRole_ReturnsSuccess()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var memberUserId = Guid.NewGuid();
            var command = CreateValidCommand(tripId, memberUserId, TripRole.Editor);
            var member = CreateTripMember(tripId, memberUserId, ownerId, "editor");

            _currentUserMock.Setup(x => x.Id).Returns(ownerId.ToString());
            _tripMemberRepositoryMock
                .Setup(x => x.GetOwnerIdByTripId(tripId))
                .ReturnsAsync(ownerId);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<TripMemberDto>(It.IsAny<Domain.Entities.TripMember>()))
                .Returns(CreateTripMemberDto(member));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            member.Role.Should().Be("editor");
        }

        #endregion
    }
}
