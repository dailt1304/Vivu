using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.UseCases.Trips.Queries.GetTripById;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;
using EntityTripMember = global::Vivu.Domain.Entities.TripMember;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetTripById
{
    public class GetTripByIdQueryHandlerTests
    {
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetTripByIdQueryHandler>> _loggerMock;
        private readonly GetTripByIdQueryHandler _handler;

        public GetTripByIdQueryHandlerTests()
        {
            _tripRepositoryMock = new Mock<ITripRepository>();
            _mapperMock         = new Mock<IMapper>();
            _loggerMock         = new Mock<ILogger<GetTripByIdQueryHandler>>();

            _handler = new GetTripByIdQueryHandler(
                _tripRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Trip CreateTrip(Guid ownerId, Guid? tripId = null,
            ICollection<EntityTripMember>? members = null)
        {
            var trip = Trip.Create(
                userId: ownerId,
                title: "Test Trip",
                description: "Test Description",
                startDate: DateTime.UtcNow.AddDays(1),
                endDate: DateTime.UtcNow.AddDays(5));

            if (tripId.HasValue)
                typeof(Trip).GetProperty("Id")!.SetValue(trip, tripId.Value);

            if (members != null)
                typeof(Trip).GetProperty("TripMembers")!.SetValue(trip, members);

            return trip;
        }

        private void SetupTrip(Trip? trip, Guid tripId)
        {
            var resolvedId = tripId;
            _tripRepositoryMock
                .Setup(r => r.GetTripByIdWithDetailsAsync(resolvedId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);
        }

        private static DetailedTripDto CreateDetailedDto(Trip trip) => new DetailedTripDto
        {
            Id          = trip.Id,
            UserId      = trip.UserId,
            Title       = trip.Title,
            Description = trip.Description,
            Status      = trip.Status,
        };

        private static GetTripByIdQuery MakeQuery(Guid tripId, Guid requestUserId) => new GetTripByIdQuery
        {
            TripId        = tripId,
            RequestUserId = requestUserId
        };

        #endregion

        #region Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundError()
        {
            var tripId = Guid.NewGuid();
            SetupTrip(null, tripId);

            var result = await _handler.Handle(
                MakeQuery(tripId, Guid.NewGuid()), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.NotFoundById(tripId).Code);
        }

        [Fact]
        public async Task Handle_TripNotFound_MapperNeverCalled()
        {
            var tripId = Guid.NewGuid();
            SetupTrip(null, tripId);

            await _handler.Handle(MakeQuery(tripId, Guid.NewGuid()), CancellationToken.None);

            _mapperMock.Verify(m => m.Map<DetailedTripDto>(It.IsAny<Trip>()), Times.Never);
        }

        #endregion

        #region Access Denied

        [Fact]
        public async Task Handle_UserNeitherOwnerNorMember_ReturnsAccessDeniedError()
        {
            var ownerId  = Guid.NewGuid();
            var tripId   = Guid.NewGuid();
            var randomId = Guid.NewGuid();

            var trip = CreateTrip(ownerId, tripId, new List<EntityTripMember>());
            SetupTrip(trip, tripId);

            var result = await _handler.Handle(MakeQuery(tripId, randomId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Trip.AccessDenied.Code);
        }

        [Fact]
        public async Task Handle_UserNeitherOwnerNorMember_MapperNeverCalled()
        {
            var ownerId  = Guid.NewGuid();
            var tripId   = Guid.NewGuid();
            var randomId = Guid.NewGuid();

            var trip = CreateTrip(ownerId, tripId, new List<EntityTripMember>());
            SetupTrip(trip, tripId);

            await _handler.Handle(MakeQuery(tripId, randomId), CancellationToken.None);

            _mapperMock.Verify(m => m.Map<DetailedTripDto>(It.IsAny<Trip>()), Times.Never);
        }

        #endregion

        #region Happy Path — Owner

        [Fact]
        public async Task Handle_RequestUserIsOwner_ReturnsSuccess()
        {
            var ownerId = Guid.NewGuid();
            var tripId  = Guid.NewGuid();

            var trip = CreateTrip(ownerId, tripId, new List<EntityTripMember>());
            SetupTrip(trip, tripId);

            var dto = CreateDetailedDto(trip);
            _mapperMock.Setup(m => m.Map<DetailedTripDto>(trip)).Returns(dto);

            var result = await _handler.Handle(MakeQuery(tripId, ownerId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(dto);
        }

        [Fact]
        public async Task Handle_RequestUserIsOwner_MapperCalledOnce()
        {
            var ownerId = Guid.NewGuid();
            var tripId  = Guid.NewGuid();

            var trip = CreateTrip(ownerId, tripId, new List<EntityTripMember>());
            SetupTrip(trip, tripId);
            _mapperMock.Setup(m => m.Map<DetailedTripDto>(trip)).Returns(CreateDetailedDto(trip));

            await _handler.Handle(MakeQuery(tripId, ownerId), CancellationToken.None);

            _mapperMock.Verify(m => m.Map<DetailedTripDto>(trip), Times.Once);
        }

        [Fact]
        public async Task Handle_RequestUserIsOwner_ReturnsDtoWithCorrectTripId()
        {
            var ownerId = Guid.NewGuid();
            var tripId  = Guid.NewGuid();

            var trip = CreateTrip(ownerId, tripId, new List<EntityTripMember>());
            SetupTrip(trip, tripId);

            var dto = CreateDetailedDto(trip);
            _mapperMock.Setup(m => m.Map<DetailedTripDto>(trip)).Returns(dto);

            var result = await _handler.Handle(MakeQuery(tripId, ownerId), CancellationToken.None);

            result.Value!.Id.Should().Be(tripId);
            result.Value.UserId.Should().Be(ownerId);
        }

        #endregion

        #region Happy Path — Member

        [Fact]
        public async Task Handle_RequestUserIsMember_ReturnsSuccess()
        {
            var ownerId  = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var tripId   = Guid.NewGuid();

            var members = new List<EntityTripMember>
            {
                EntityTripMember.Create(tripId, memberId, ownerId, "member")
            };
            var trip = CreateTrip(ownerId, tripId, members);
            SetupTrip(trip, tripId);

            var dto = CreateDetailedDto(trip);
            _mapperMock.Setup(m => m.Map<DetailedTripDto>(trip)).Returns(dto);

            var result = await _handler.Handle(MakeQuery(tripId, memberId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RequestUserIsMemberNotOwner_ReturnsSuccess()
        {
            var ownerId  = Guid.NewGuid();
            var memberId = Guid.NewGuid();
            var tripId   = Guid.NewGuid();

            // ownerId != memberId, so isOwner = false, but isMember = true
            var members = new List<EntityTripMember>
            {
                EntityTripMember.Create(tripId, memberId, ownerId, "editor")
            };
            var trip = CreateTrip(ownerId, tripId, members);
            SetupTrip(trip, tripId);

            _mapperMock.Setup(m => m.Map<DetailedTripDto>(trip)).Returns(CreateDetailedDto(trip));

            var result = await _handler.Handle(MakeQuery(tripId, memberId), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Repository Interaction

        [Fact]
        public async Task Handle_Always_CallsRepositoryWithCorrectTripId()
        {
            var tripId = Guid.NewGuid();
            SetupTrip(null, tripId);

            await _handler.Handle(MakeQuery(tripId, Guid.NewGuid()), CancellationToken.None);

            _tripRepositoryMock.Verify(
                r => r.GetTripByIdWithDetailsAsync(tripId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}
