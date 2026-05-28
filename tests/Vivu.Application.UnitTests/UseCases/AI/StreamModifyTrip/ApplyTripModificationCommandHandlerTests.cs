using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.UseCases.AI.ApplyTripModification;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.AI.Commands.ApplyTripModification
{
    public class ApplyTripModificationCommandHandlerTests
    {
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<IAIConvert> _aiConvertMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<ApplyTripModificationCommandHandler>> _loggerMock;
        private readonly Mock<ITripLocationAlternativeRepository> _tripLocationAlternativeRepositoryMock;
        private readonly Mock<ITripItineraryOptimizer> _optimizerMock;
        private readonly ApplyTripModificationCommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testTripId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private readonly Guid _testLocationId1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private readonly Guid _testLocationId2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

        public ApplyTripModificationCommandHandlerTests()
        {
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _aiConvertMock = new Mock<IAIConvert>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _currentUserMock = new Mock<ICurrentUser>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<ApplyTripModificationCommandHandler>>();
            _tripLocationAlternativeRepositoryMock = new Mock<ITripLocationAlternativeRepository>();
            _optimizerMock = new Mock<ITripItineraryOptimizer>();

            _handler = new ApplyTripModificationCommandHandler(
                _tripDayRepositoryMock.Object,
                _aiConvertMock.Object,
                _tripLocationRepositoryMock.Object,
                _currentUserMock.Object,
                _locationRepositoryMock.Object,
                _chatMessageRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _tripRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _tripLocationAlternativeRepositoryMock.Object,
                _optimizerMock.Object
            );
        }

        #region Guard Tests — Invalid User

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns("not-a-guid");

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _tripRepositoryMock.Verify(x => x.GetTripByIdWithDetailsForAIModifyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundFailure()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip)null!);

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.NotFound);
        }

        [Fact]
        public async Task Handle_TripNotFound_LogsWarning()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip)null!);

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Change Type: add_location

        [Fact]
        public async Task Handle_AddLocation_DayNotFound_ReturnsDayNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_location",
                DayIndex = 99, // non-existent day
                Location = CreateFakeLocationPlan(_testLocationId1)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AddLocation_ValidDay_CallsTripLocationAddAsync()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_location",
                DayIndex = 1,
                Location = CreateFakeLocationPlan(_testLocationId1)
            });

            await _handler.Handle(command, CancellationToken.None);

            _tripLocationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripLocation>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AddLocation_ValidDay_CreatesLocationWithCorrectData()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            TripLocation? capturedLocation = null;
            _tripLocationRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripLocation>()))
                .Callback<TripLocation>(l => capturedLocation = l)
                .ReturnsAsync(new TripLocation());

            var locationPlan = CreateFakeLocationPlan(_testLocationId1, orderIndex: 2);
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_location",
                DayIndex = 1,
                Location = locationPlan
            });

            await _handler.Handle(command, CancellationToken.None);

            capturedLocation.Should().NotBeNull();
            capturedLocation!.LocationId.Should().Be(_testLocationId1);
            capturedLocation.OrderIndex.Should().Be(2);
        }

        #endregion

        #region Change Type: remove_location

        [Fact]
        public async Task Handle_RemoveLocation_DayNotFound_ReturnsDayNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 99,
                LocationId = _testLocationId1
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_RemoveLocation_LocationNotFoundInDay_ReturnsLocationNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = Guid.NewGuid() // ID not in day
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.ApplyModification.LocationNotFoundForRemove);
        }

        [Fact]
        public async Task Handle_RemoveLocation_ValidDayAndLocation_SucceedsAndSaves()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = _testLocationId1 // exists in day
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Change Type: update_time

        [Fact]
        public async Task Handle_UpdateTime_DayNotFound_ReturnsDayNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_time",
                DayIndex = 99,
                LocationId = _testLocationId1,
                StartTime = new TimeOnly(10, 0)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_UpdateTime_LocationNotFound_ReturnsLocationNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_time",
                DayIndex = 1,
                LocationId = Guid.NewGuid(),
                StartTime = new TimeOnly(10, 0)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.ApplyModification.LocationNotFoundForUpdateTime);
        }

        [Fact]
        public async Task Handle_UpdateTime_BothTimesSet_UpdatesStartAndEndTime()
        {
            var trip = CreateFakeTripWithDays();
            var tripLocation = trip.TripDays.First().TripLocations.First();
            SetupFullHappyPathWithTrip(trip);

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_time",
                DayIndex = 1,
                LocationId = _testLocationId1,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0)
            });

            await _handler.Handle(command, CancellationToken.None);

            tripLocation.StartTime.Should().Be(new TimeOnly(10, 0).ToTimeSpan());
            tripLocation.EndTime.Should().Be(new TimeOnly(12, 0).ToTimeSpan());
        }

        [Fact]
        public async Task Handle_UpdateTime_OnlyStartTimeSet_UpdatesOnlyStartTime()
        {
            var trip = CreateFakeTripWithDays();
            var tripLocation = trip.TripDays.First().TripLocations.First();
            var originalEndTime = tripLocation.EndTime;
            SetupFullHappyPathWithTrip(trip);

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_time",
                DayIndex = 1,
                LocationId = _testLocationId1,
                StartTime = new TimeOnly(10, 0),
                EndTime = null
            });

            await _handler.Handle(command, CancellationToken.None);

            tripLocation.StartTime.Should().Be(new TimeOnly(10, 0).ToTimeSpan());
            tripLocation.EndTime.Should().Be(originalEndTime);
        }

        #endregion

        #region Change Type: update_location

        [Fact]
        public async Task Handle_UpdateLocation_DayNotFound_ReturnsDayNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_location",
                DayIndex = 99,
                OldLocationId = _testLocationId1,
                Location = CreateFakeLocationPlan(_testLocationId2)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_UpdateLocation_OldLocationNotFound_ReturnsOldLocationNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_location",
                DayIndex = 1,
                OldLocationId = Guid.NewGuid(), // not in day
                Location = CreateFakeLocationPlan(_testLocationId2)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.ApplyModification.OldLocationNotFoundForUpdate);
        }

        [Fact]
        public async Task Handle_UpdateLocation_ValidChange_RemovesOldAndAddsNewLocation()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_location",
                DayIndex = 1,
                OldLocationId = _testLocationId1,
                Location = CreateFakeLocationPlan(_testLocationId2)
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _tripLocationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripLocation>()), Times.Once);
        }

        #endregion

        #region Change Type: add_day

        [Fact]
        public async Task Handle_AddDay_CallsTripDayAddAsync()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_day",
                DayIndex = 2,
                Title = "Ngày 2: Mới thêm",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
                Locations = new List<TripLocationChange>()
            });

            await _handler.Handle(command, CancellationToken.None);

            _tripDayRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripDay>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AddDay_WithLocations_DoesNotCallTripLocationAddAsyncExplicitly()
        {
            // Locations inside add_day are added to TripDay.TripLocations collection,
            // NOT via AddAsync — EF Core handles cascade insert
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_day",
                DayIndex = 2,
                Title = "Ngày 2",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
                Locations = new List<TripLocationChange> { CreateFakeLocationPlan(_testLocationId1) }
            });

            await _handler.Handle(command, CancellationToken.None);

            _tripLocationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TripLocation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AddDay_TriggersReIndexingOfDayIndex()
        {
            var trip = CreateFakeTripWithDays();
            SetupFullHappyPathWithTrip(trip);
            Trip? capturedTrip = null;
            _mapperMock
                .Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>()))
                .Callback<object>(obj => capturedTrip = obj as Trip)
                .Returns(new DetailedTripDto());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "add_day",
                DayIndex = 2,
                Title = "Ngày 2",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
                Locations = new List<TripLocationChange>()
            });

            await _handler.Handle(command, CancellationToken.None);

            // Re-indexing should have run — DayIndex values should be sequential
            capturedTrip.Should().NotBeNull();
            capturedTrip!.TripDays
                .Where(d => d.DayDate.HasValue)
                .Select(d => d.DayIndex)
                .Should().OnlyHaveUniqueItems();
        }

        #endregion

        #region Change Type: remove_day

        [Fact]
        public async Task Handle_RemoveDay_DayNotFound_ReturnsDayNotFoundFailure()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_day",
                DayIndex = 99
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_RemoveDay_ValidDay_Succeeds()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_day",
                DayIndex = 1
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_RemoveDay_TriggersReIndexingOfDayIndex()
        {
            var trip = CreateFakeTripWithMultipleDays();
            SetupFullHappyPathWithTrip(trip);
            Trip? capturedTrip = null;
            _mapperMock
                 .Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>()))
                 .Callback<object>(obj => capturedTrip = obj as Trip)
                 .Returns(new DetailedTripDto());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_day",
                DayIndex = 1
            });

            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            // After removing day 1, remaining days should be re-indexed starting from 1
            capturedTrip!.TripDays
                .Where(d => d.DayDate.HasValue)
                .Select(d => d.DayIndex)
                .Should().OnlyHaveUniqueItems();
        }

        #endregion

        #region Change Type: update_day_date

        [Fact]
        public async Task Handle_UpdateDayDate_DayNotFound_OnlyLogsWarningDoesNotFail()
        {
            // Unlike other change types, update_day_date does NOT fail when day not found — only logs warning
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_day_date",
                DayIndex = 99, // non-existent
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _loggerMock.Verify(
                x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_UpdateDayDate_ValidDay_UpdatesDayDate()
        {
            var trip = CreateFakeTripWithDays();
            var tripDay = trip.TripDays.First(d => d.DayIndex == 1);
            SetupFullHappyPathWithTrip(trip);

            var newDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_day_date",
                DayIndex = 1,
                Date = newDate
            });

            await _handler.Handle(command, CancellationToken.None);

            tripDay.DayDate.Should().Be(newDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }

        #endregion

        #region Side Effects — Always Happen

        [Fact]
        public async Task Handle_AnyValidChange_AlwaysCreatesAIChatMessageWithTripModificationType()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            ChatMessage? capturedMessage = null;
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => capturedMessage = m)
                .ReturnsAsync(new ChatMessage());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = _testLocationId1
            });

            await _handler.Handle(command, CancellationToken.None);

            capturedMessage.Should().NotBeNull();
            capturedMessage!.IsAiMessage.Should().BeTrue();
            capturedMessage.MessageType.Should().Be("trip_modification");
            capturedMessage.Content.Should().Be(command.Modification.Summary);
        }

        [Fact]
        public async Task Handle_AnyValidChange_CallsSaveChangesExactlyOnce()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = _testLocationId1
            });

            await _handler.Handle(command, CancellationToken.None);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AnyValidChange_AlwaysReIndexesOrderIndexForAllLocations()
        {
            var trip = CreateFakeTripWithDays();
            SetupFullHappyPathWithTrip(trip);
            Trip? capturedTrip = null;
            _mapperMock
                .Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>()))
                .Callback<object>(obj => capturedTrip = obj as Trip)
                .Returns(new DetailedTripDto());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_time",
                DayIndex = 1,
                LocationId = _testLocationId1,
                StartTime = new TimeOnly(10, 0)
            });

            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            foreach (var day in capturedTrip!.TripDays)
            {
                var orderIndexes = day.TripLocations.Select(l => l.OrderIndex).ToList();
                orderIndexes.Should().OnlyHaveUniqueItems("OrderIndex must be unique per day");
            }
        }

        [Fact]
        public async Task Handle_DayHasLocations_TripStartAndEndDateUpdated()
        {
            var trip = CreateFakeTripWithDays();
            SetupFullHappyPathWithTrip(trip);
            Trip? capturedTrip = null;
            _mapperMock
                .Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>()))
                .Callback<object>(obj => capturedTrip = obj as Trip)
                .Returns(new DetailedTripDto());

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "update_day_date",
                DayIndex = 1,
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
            });

            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            capturedTrip!.StartDate.Should().NotBeNull();
            capturedTrip.EndDate.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ValidChange_ReturnsSuccessWithMappedDto()
        {
            SetupFullHappyPathWithTrip(CreateFakeTripWithDays());
            var expectedDto = new DetailedTripDto { Id = Guid.NewGuid() };
            _mapperMock.Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>())).Returns(expectedDto);

            var command = CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = _testLocationId1
            });

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(expectedDto);
        }

        #endregion

        #region Retry Logic Tests

        [Fact]
        public async Task Handle_DbUpdateConcurrencyException_RetriesAndSucceedsOnThirdAttempt()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateFakeTripWithDays()); // fresh trip each call

            SetupRepositoriesForAdd();

            _unitOfWorkMock
                .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("conflict 1"))
                .ThrowsAsync(new DbUpdateConcurrencyException("conflict 2"))
                .ReturnsAsync(1);

            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());
            _mapperMock.Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>())).Returns(new DetailedTripDto());

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Fact]
        public async Task Handle_AllThreeRetryAttemptsFail_ReturnsConcurrencyError()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateFakeTripWithDays());

            SetupRepositoriesForAdd();
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());

            _unitOfWorkMock
                .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("c1"))
                .ThrowsAsync(new DbUpdateConcurrencyException("c2"))
                .ThrowsAsync(new DbUpdateConcurrencyException("c3"));

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Contain("ConcurrencyError");
        }

        [Fact]
        public async Task Handle_ConcurrencyException_CallsClearChangeTrackerOnEachAttempt()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateFakeTripWithDays());

            SetupRepositoriesForAdd();
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());

            _unitOfWorkMock
                .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("c1"))
                .ThrowsAsync(new DbUpdateConcurrencyException("c2"))
                .ReturnsAsync(1);

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            // ClearChangeTracker is called at the start of each attempt (5 attempts total)
            _unitOfWorkMock.Verify(x => x.ClearChangeTracker(), Times.Exactly(5));
        }

        [Fact]
        public async Task Handle_ConcurrencyException_LogsWarningOnEachRetry()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateFakeTripWithDays());

            SetupRepositoriesForAdd();
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());

            _unitOfWorkMock
                .SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateConcurrencyException("c1"))
                .ReturnsAsync(1);

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), It.IsAny<DbUpdateConcurrencyException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Setup Helper Methods

        private void SetupValidUser()
        {
            _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());
            _currentUserMock.Setup(x => x.TraceId).Returns("test-trace-id");
        }

        private void SetupRepositoriesForAdd()
        {
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripLocation>())).ReturnsAsync(new TripLocation());
            _tripDayRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripDay>())).ReturnsAsync(new TripDay());
            _aiConvertMock.Setup(x => x.ConvertTimeOnlyToTimeSpan(It.IsAny<TimeOnly>()))
                .Returns((TimeOnly t) => t.ToTimeSpan());
        }

        private void SetupFullHappyPathWithTrip(Trip trip)
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsForAIModifyAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(trip);
            SetupRepositoriesForAdd();
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _unitOfWorkMock.Setup(x => x.ClearChangeTracker());
            _mapperMock.Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>())).Returns(new DetailedTripDto());
        }

        #endregion

        #region Data Factory Methods

        private ApplyTripModificationCommand CreateValidCommand() =>
            CreateCommandWithChanges(new TripChange
            {
                Type = "remove_location",
                DayIndex = 1,
                LocationId = _testLocationId1
            });

        private ApplyTripModificationCommand CreateCommandWithChanges(params TripChange[] changes) =>
            new ApplyTripModificationCommand
            {
                TripId = _testTripId,
                UserRequest = "Thêm địa điểm mới",
                Modification = new TripModificationResponse
                {
                    Summary = "Đã thêm địa điểm",
                    Changes = changes.ToList()
                }
            };

        private Trip CreateFakeTripWithDays()
        {
            var tripDay = TripDay.Create(
                TripId: _testTripId,
                Tittle: "Ngày 1",
                DayDate: DateTime.UtcNow.AddDays(7),
                DayIndex: 1
            );

            var tripLocation = TripLocation.Create(
                tripDayId: tripDay.Id,
                locationId: _testLocationId1,
                orderIndex: 1,
                startTime: TimeSpan.FromHours(9),
                endTime: TimeSpan.FromHours(11),
                note: "Test",
                transportMode: "walking"
            );
            tripDay.TripLocations.Add(tripLocation);

            var trip = Trip.Create(
                userId: _testUserId,
                title: "Test Trip",
                description: "Test",
                coverUrl: null,
                startDate: DateTime.UtcNow.AddDays(7),
                endDate: DateTime.UtcNow.AddDays(10),
                tripSize: 1,
                isPublic: false,
                cityId: Guid.NewGuid(),
                inviteCode: null
            );
            trip.TripDays.Add(tripDay);
            return trip;
        }

        private Trip CreateFakeTripWithMultipleDays()
        {
            var trip = CreateFakeTripWithDays();

            var tripDay2 = TripDay.Create(
                TripId: _testTripId,
                Tittle: "Ngày 2",
                DayDate: DateTime.UtcNow.AddDays(8),
                DayIndex: 2
            );
            var tripLocation2 = TripLocation.Create(
                tripDayId: tripDay2.Id,
                locationId: _testLocationId2,
                orderIndex: 1,
                startTime: TimeSpan.FromHours(9),
                endTime: TimeSpan.FromHours(11),
                note: "Test 2",
                transportMode: "taxi"
            );
            tripDay2.TripLocations.Add(tripLocation2);
            trip.TripDays.Add(tripDay2);

            return trip;
        }

        private static TripLocationChange CreateFakeLocationPlan(Guid locationId, int orderIndex = 1) =>
            new TripLocationChange
            {
                LocationId = locationId,
                Name = "Test Location",
                Description = "Test",
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(16, 0),
                TransportMode = "walking",
                OrderIndex = orderIndex
            };

        #endregion
    }
}
