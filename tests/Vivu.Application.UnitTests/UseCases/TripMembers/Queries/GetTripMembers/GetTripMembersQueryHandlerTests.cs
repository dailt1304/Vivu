using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.TripMembers.Queries.GetTripMembers
{
    public class GetTripMembersQueryHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<GetTripMembersQueryHandler>> _loggerMock;
        private readonly GetTripMembersQueryHandler _handler;

        public GetTripMembersQueryHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _mapperMock = new Mock<IMapper>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<GetTripMembersQueryHandler>>();

            _handler = new GetTripMembersQueryHandler(
                _tripRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _mapperMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Happy Path Tests

        [Fact]
        public async Task Handle_ValidQuery_ReturnsSuccessWithPaginatedMembers()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 3);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, members[0]);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_OwnerAccessingMembers_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 2);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripMember?)null); // Owner is not in TripMembers
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_MemberAccessingMembers_ReturnsSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, ownerId);
            var currentUserMember = TripMember.Create(tripId, userId, ownerId, "viewer");
            var members = CreateTripMembers(tripId, ownerId, 2);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, currentUserMember);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ValidQuery_LogsInformation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 1);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, members[0]);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Get trip members attempt")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully fetched")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Failure Tests - Authentication

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var query = CreateValidQuery(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);

            _tripRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmptyUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var query = CreateValidQuery(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns(string.Empty);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidGuidUserId_ReturnsInvalidTokenError()
        {
            // Arrange
            var query = CreateValidQuery(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns("invalid-guid");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_LogsWarning()
        {
            // Arrange
            var query = CreateValidQuery(Guid.NewGuid());
            _currentUserMock.Setup(x => x.Id).Returns((string?)null);

            // Act
            await _handler.Handle(query, CancellationToken.None);

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

        #region Failure Tests - Trip Validation

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);

            SetupCurrentUser(userId);
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFoundById(tripId));
        }

        [Fact]
        public async Task Handle_DeletedTrip_ReturnsTripDeletedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            trip.IsDeleted = true;

            SetupCurrentUser(userId);
            SetupTripRepository(trip);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.TripDeleted(tripId));
        }

        [Fact]
        public async Task Handle_NonMemberNonOwner_ReturnsAccessDeniedError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, ownerId);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripMember?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.AccessDenied);
        }

        [Fact]
        public async Task Handle_TripNotFound_LogsWarning()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);

            SetupCurrentUser(userId);
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(tripId))
                .ReturnsAsync((Trip?)null);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Trip not found")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_CustomPageSize_ReturnsPaginatedResults()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = new GetTripMembersQuery
            {
                TripId = tripId,
                PageNumber = 1,
                PageSize = 5
            };
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 5);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, members[0]);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageSize.Should().Be(5);
            result.Value.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_SecondPage_ReturnsCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = new GetTripMembersQuery
            {
                TripId = tripId,
                PageNumber = 2,
                PageSize = 10
            };
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 3);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, members[0]);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PageNumber.Should().Be(2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Handle_EmptyMemberList_ReturnsEmptyPaginatedList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            var members = new List<TripMember>();

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TripMember?)null);
            SetupTripMemberQuery(members);
            SetupMapper(new List<TripMemberDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_SingleMember_ReturnsOneMember()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var query = CreateValidQuery(tripId);
            var trip = CreateTrip(tripId, userId);
            var members = CreateTripMembers(tripId, userId, 1);
            var memberDtos = CreateTripMemberDtos(members);

            SetupCurrentUser(userId);
            SetupTripRepository(trip);
            SetupTripMemberRepository(tripId, userId, members[0]);
            SetupTripMemberQuery(members);
            SetupMapper(memberDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.TotalCount.Should().Be(1);
        }

        #endregion

        #region Helper Methods

        private GetTripMembersQuery CreateValidQuery(Guid tripId)
        {
            return new GetTripMembersQuery
            {
                TripId = tripId,
                PageNumber = 1,
                PageSize = 10
            };
        }

        private Trip CreateTrip(Guid tripId, Guid userId)
        {
            var trip = Trip.Create(
                userId: userId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(14),
                isPublic: false,
                inviteCode: null
            );

            typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId);
            return trip;
        }

        private List<TripMember> CreateTripMembers(Guid tripId, Guid ownerId, int count)
        {
            var members = new List<TripMember>();
            for (int i = 0; i < count; i++)
            {
                var member = TripMember.Create(tripId, Guid.NewGuid(), ownerId, "viewer");
                members.Add(member);
            }
            return members;
        }

        private List<TripMemberDto> CreateTripMemberDtos(List<TripMember> members)
        {
            return members.Select(m => new TripMemberDto
            {
                UserId = m.UserId,
                TripId = m.TripId,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                FullName = "Test User",
                Email = "test@example.com"
            }).ToList();
        }

        private void SetupCurrentUser(Guid userId)
        {
            _currentUserMock.Setup(x => x.Id).Returns(userId.ToString());
        }

        private void SetupTripRepository(Trip trip)
        {
            _tripRepositoryMock
                .Setup(x => x.GetByIdAsync(trip.Id))
                .ReturnsAsync(trip);
        }

        private void SetupTripMemberRepository(Guid tripId, Guid userId, TripMember? member)
        {
            _tripMemberRepositoryMock
                .Setup(x => x.GetByTripAndUserAsync(tripId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
        }

        private void SetupTripMemberQuery(List<TripMember> members)
        {
            _tripMemberRepositoryMock
                .Setup(x => x.GetMembersByTripIdQuery(It.IsAny<Guid>()))
                .Returns(members.BuildMock());
        }

        private void SetupMapper(List<TripMemberDto> memberDtos)
        {
            for (int i = 0; i < memberDtos.Count; i++)
            {
                var index = i;
                _mapperMock
                    .Setup(x => x.Map<TripMemberDto>(It.IsAny<TripMember>()))
                    .Returns((TripMember m) => memberDtos.FirstOrDefault(dto => dto.UserId == m.UserId) ?? memberDtos[0]);
            }
        }

        #endregion
    }
}
