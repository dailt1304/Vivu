using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.Trips.Commands.CreateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Trips.Commands.CreateTrip
{
    public class CreateTripCommandHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IInviteCodeGenerator> _inviteCodeGeneratorMock;
        private readonly Mock<ITripLimitChecker> _tripLimitCheckerMock;
        private readonly Mock<ILogger<CreateTripCommandHandler>> _loggerMock;
        private readonly CreateTripCommandHandler _handler;

        public CreateTripCommandHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _currentUserMock = new Mock<ICurrentUser>();
            _inviteCodeGeneratorMock = new Mock<IInviteCodeGenerator>();
            _tripLimitCheckerMock = new Mock<ITripLimitChecker>();
            _loggerMock = new Mock<ILogger<CreateTripCommandHandler>>();

            _handler = new CreateTripCommandHandler(
                _tripRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _tripDayRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _inviteCodeGeneratorMock.Object,
                _tripLimitCheckerMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSuccessWithTripDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            var expectedTripDto = CreateExpectedTripDto(userId);

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(expectedTripDto);
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Title.Should().Be(command.Title);
            result.Value.UserId.Should().Be(userId);

            // Verify interactions
            _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>()), Times.Once);
            _tripMemberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripMember>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesTripWithCorrectData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            command.Description = "Test Description";
            command.StartDate = DateTime.UtcNow.AddDays(1);
            command.EndDate = DateTime.UtcNow.AddDays(5);
            command.TripSize = 4;
            command.IsPublic = true;

            Trip capturedTrip = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTrip.Should().NotBeNull();
            capturedTrip.UserId.Should().Be(userId);
            capturedTrip.Title.Should().Be(command.Title);
            capturedTrip.Description.Should().Be(command.Description);
            capturedTrip.TripSize.Should().Be(command.TripSize);
            capturedTrip.IsPublic.Should().Be(command.IsPublic);
            capturedTrip.Status.Should().Be("planning");
            capturedTrip.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesTripMemberForOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            TripMember capturedTripMember = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripMemberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripMember>()))
                .Callback<TripMember>(tm => capturedTripMember = tm)
                .ReturnsAsync((TripMember tm) => tm);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripMember.Should().NotBeNull();
            capturedTripMember.UserId.Should().Be(userId);
            capturedTripMember.OwnerId.Should().Be(userId);
            capturedTripMember.Role.Should().Be("owner");
            capturedTripMember.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task Handle_WithGenerateInviteCode_GeneratesAndSetsInviteCode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            command.GenerateInviteCode = true;
            var expectedInviteCode = "ABC123XYZ";

            Trip capturedTrip = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _inviteCodeGeneratorMock
                .Setup(x => x.Generate(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedInviteCode);

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTrip.InviteCode.Should().Be(expectedInviteCode);
            _inviteCodeGeneratorMock.Verify(x => x.Generate(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutGenerateInviteCode_DoesNotGenerateInviteCode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            command.GenerateInviteCode = false;

            Trip capturedTrip = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTrip.InviteCode.Should().BeNull();
            _inviteCodeGeneratorMock.Verify(x => x.Generate(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidCommand_LogsInformationOnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Trip created successfully")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Invalid User

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = CreateValidCommand();
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>()), Times.Never);
            _tripMemberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = CreateValidCommand();
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidGuidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var command = CreateValidCommand();
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid-format");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_LogsWarning()
        {
            // Arrange
            var command = CreateValidCommand();
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid or missing user ID")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Trip Limit Reached

        [Fact]
        public async Task Handle_TripLimitReached_ReturnsTripLimitReachedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();

            SetupCurrentUser(userId);
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.TripLimitReached);

            _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>()), Times.Never);
            _tripMemberRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripMember>()), Times.Never);
        }

        [Fact]
        public async Task Handle_TripLimitReached_LogsWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();

            SetupCurrentUser(userId);
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Trip limit reached")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_WithNullOptionalFields_CreatesTrip()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateTripCommand
            {
                Title = "Test Trip",
                Description = null,
                CoverUrl = null,
                StartDate = null,
                EndDate = null,
                TripSize = null,
                IsPublic = false,
                GenerateInviteCode = false
            };

            Trip capturedTrip = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedTrip.Description.Should().BeNull();
            capturedTrip.CoverUrl.Should().BeNull();
            capturedTrip.StartDate.Should().BeNull();
            capturedTrip.EndDate.Should().BeNull();
            capturedTrip.TripSize.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithMaxLengthTitle_CreatesTrip()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            command.Title = new string('A', 200); // Max length

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithPublicTrip_SetsIsPublicTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();
            command.IsPublic = true;

            Trip capturedTrip = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTrip.IsPublic.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_TripMemberTripIdMatchesTripId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = CreateValidCommand();

            Trip capturedTrip = null!;
            TripMember capturedTripMember = null!;

            SetupCurrentUser(userId);
            SetupTripLimitChecker(true);
            SetupMapper(CreateExpectedTripDto(userId));
            SetupUnitOfWork();

            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync((Trip t) => t);

            _tripMemberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripMember>()))
                .Callback<TripMember>(tm => capturedTripMember = tm)
                .ReturnsAsync((TripMember tm) => tm);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTripMember.TripId.Should().Be(capturedTrip.Id);
        }

        #endregion

        #region Helper Methods

        private CreateTripCommand CreateValidCommand()
        {
            return new CreateTripCommand
            {
                Title = "My Test Trip",
                Description = "A great trip description",
                CoverUrl = "https://example.com/cover.jpg",
                CityId = Guid.NewGuid(),
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(14),
                TripSize = 5,
                IsPublic = false,
                GenerateInviteCode = false
            };
        }

        private TripDto CreateExpectedTripDto(Guid userId)
        {
            return new TripDto
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CityId = Guid.NewGuid(),
                Title = "My Test Trip",
                Description = "A great trip description",
                CoverUrl = "https://example.com/cover.jpg",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(14),
                TripSize = 5,
                Status = "planning",
                IsPublic = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private void SetupTripLimitChecker(bool canCreate)
        {
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(canCreate);
        }

        private void SetupMapper(TripDto tripDto)
        {
            _mapperMock
                .Setup(x => x.Map<TripDto>(It.IsAny<Trip>()))
                .Returns(tripDto);
        }

        private void SetupUnitOfWork()
        {
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        #endregion
    }
}
