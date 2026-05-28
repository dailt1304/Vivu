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
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.AI.CreateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.Tests.UseCases.AI.Commands.AICreateTrip
{
    public class AICreateTripCommandHandlerTests
    {
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IInviteCodeGenerator> _inviteCodeGeneratorMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ITripLocationRepository> _tripLocationRepositoryMock;
        private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
        private readonly Mock<IAIConvert> _aiConvertMock;
        private readonly Mock<ITripDayRepository> _tripDayRepositoryMock;
        private readonly Mock<ITripLocationAlternativeRepository> _tripLocationAlternativeRepositoryMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<ITripMemberRepository> _tripMemberRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILogger<AICreateTripCommandHandler>> _loggerMock;
        private readonly AICreateTripCommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testLocationId1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private readonly Guid _testLocationId2 = Guid.Parse("10000000-0000-0000-0000-000000000002");

        public AICreateTripCommandHandlerTests()
        {
            _currentUserMock = new Mock<ICurrentUser>();
            _inviteCodeGeneratorMock = new Mock<IInviteCodeGenerator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _tripLocationRepositoryMock = new Mock<ITripLocationRepository>();
            _tripLocationAlternativeRepositoryMock = new Mock<ITripLocationAlternativeRepository>();
            _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
            _aiConvertMock = new Mock<IAIConvert>();
            _tripDayRepositoryMock = new Mock<ITripDayRepository>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _tripMemberRepositoryMock = new Mock<ITripMemberRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _loggerMock = new Mock<ILogger<AICreateTripCommandHandler>>();

            _handler = new AICreateTripCommandHandler(
                _currentUserMock.Object,
                _inviteCodeGeneratorMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _tripLocationRepositoryMock.Object,
                _tripLocationAlternativeRepositoryMock.Object,
                _chatMessageRepositoryMock.Object,
                _aiConvertMock.Object,
                _tripDayRepositoryMock.Object,
                _tripRepositoryMock.Object,
                _tripMemberRepositoryMock.Object,
                _locationRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region Guard Tests — Invalid User

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_DoesNotCallAnyRepository()
        {
            _currentUserMock.Setup(x => x.Id).Returns("bad-id");

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _locationRepositoryMock.Verify(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
            _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Location Validation

        [Fact]
        public async Task Handle_AllLocationIdsInvalid_ReturnsInvalidLocationsFailure()
        {
            SetupValidUser();
            _locationRepositoryMock
                .Setup(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid>());

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_SomeLocationIdsInvalid_ReturnsFailureWithMissingIds()
        {
            SetupValidUser();
            var locationId3 = Guid.Parse("10000000-0000-0000-0000-000000000003");
            _locationRepositoryMock
                .Setup(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid> { _testLocationId1 }); // only first ID valid, second is missing

            var command = new AICreateTripCommand
            {
                UserPrompt = "Đi Huế 2 ngày",
                cityId = Guid.NewGuid(),
                GenerateInviteCode = true,
                TripPlan = new TripPlanResponse
                {
                    Title = "2 Ngày ở Huế",
                    Description = "Test",
                    Start = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    End = DateOnly.FromDateTime(DateTime.Today.AddDays(9)),
                    Size = 1,
                    Days = new List<DayPlanDto>
                    {
                        CreateDayPlan(1, new List<Guid> { _testLocationId1, locationId3 })
                    }
                }
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_InvalidLocationIds_DoesNotCreateTrip()
        {
            SetupValidUser();
            _locationRepositoryMock
                .Setup(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid>());

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _tripRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Trip>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — Invite Code

        [Fact]
        public async Task Handle_GenerateInviteCodeTrue_CallsInviteCodeGenerator()
        {
            SetupFullHappyPath();
            _inviteCodeGeneratorMock
                .Setup(x => x.Generate(It.IsAny<CancellationToken>()))
                .ReturnsAsync("INVITE-CODE-ABC");

            await _handler.Handle(CreateValidCommand(generateInviteCode: true), CancellationToken.None);

            _inviteCodeGeneratorMock.Verify(x => x.Generate(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_GenerateInviteCodeFalse_DoesNotCallInviteCodeGenerator()
        {
            SetupFullHappyPath();

            await _handler.Handle(CreateValidCommand(generateInviteCode: false), CancellationToken.None);

            _inviteCodeGeneratorMock.Verify(x => x.Generate(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — Entity Creation Verification

        [Fact]
        public async Task Handle_ValidRequest_CreatesTripWithCorrectData()
        {
            SetupFullHappyPath();
            Trip? capturedTrip = null;
            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync(CreateValidTrip());

            var command = CreateValidCommand();
            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            capturedTrip!.Title.Should().Be(command.TripPlan.Title);
            capturedTrip.UserId.Should().Be(_testUserId);
            capturedTrip.IsPublic.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ValidRequest_AlwaysCreatesIdeasTripDayWithDayIndexZero()
        {
            SetupFullHappyPath();
            var createdDays = new List<TripDay>();
            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(d => createdDays.Add(d))
                .ReturnsAsync(new TripDay());

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            createdDays.Should().Contain(d => d.DayIndex == 0 && d.Title == "Ideas");
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesTripMemberWithOwnerRole()
        {
            SetupFullHappyPath();
            TripMember? capturedMember = null;
            _tripMemberRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripMember>()))
                .Callback<TripMember>(m => capturedMember = m)
                .ReturnsAsync(new TripMember());

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            capturedMember.Should().NotBeNull();
            capturedMember!.Role.Should().Be("owner");
            capturedMember.UserId.Should().Be(_testUserId);
            capturedMember.OwnerId.Should().Be(_testUserId);
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesTwoChatMessages()
        {
            SetupFullHappyPath();
            var createdMessages = new List<ChatMessage>();
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => createdMessages.Add(m))
                .ReturnsAsync(new ChatMessage());

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            createdMessages.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_ValidRequest_CreatesUserPromptMessageAndAIResponseMessage()
        {
            SetupFullHappyPath();
            var createdMessages = new List<ChatMessage>();
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => createdMessages.Add(m))
                .ReturnsAsync(new ChatMessage());

            var command = CreateValidCommand();
            await _handler.Handle(command, CancellationToken.None);

            createdMessages.Should().Contain(m => m.IsAiMessage == false && m.Content == command.UserPrompt);
            createdMessages.Should().Contain(m => m.IsAiMessage == true && m.MessageType == "trip_plan");
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsSaveChangesExactlyOnce()
        {
            SetupFullHappyPath();

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Happy Path — Trip Days & Locations

        [Fact]
        public async Task Handle_MultipleDayTripPlan_CreatesCorrectNumberOfTripDays()
        {
            SetupFullHappyPath();
            var createdDays = new List<TripDay>();
            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(d => createdDays.Add(d))
                .ReturnsAsync(new TripDay());

            var command = CreateValidCommand(dayCount: 3);
            await _handler.Handle(command, CancellationToken.None);

            // 1 "Ideas" day + 3 actual days
            createdDays.Should().HaveCount(4);
        }

        [Fact]
        public async Task Handle_TripPlanWithLocations_CreatesCorrectNumberOfTripLocations()
        {
            SetupFullHappyPath();
            var createdLocations = new List<TripLocation>();
            _tripLocationRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripLocation>()))
                .Callback<TripLocation>(l => createdLocations.Add(l))
                .ReturnsAsync(new TripLocation());

            // 2 days, each with 2 locations = 4 TripLocations total
            var command = CreateValidCommandWithMultipleLocations();
            await _handler.Handle(command, CancellationToken.None);

            createdLocations.Should().HaveCount(4);
        }

        [Fact]
        public async Task Handle_TripPlanDays_CreatedInDayIndexOrder()
        {
            SetupFullHappyPath();
            var createdDays = new List<TripDay>();
            _tripDayRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<TripDay>()))
                .Callback<TripDay>(d => createdDays.Add(d))
                .ReturnsAsync(new TripDay());

            await _handler.Handle(CreateValidCommand(dayCount: 3), CancellationToken.None);

            var actualDays = createdDays.Where(d => d.DayIndex > 0).ToList();
            actualDays.Select(d => d.DayIndex).Should().BeInAscendingOrder();
        }

        #endregion

        #region Happy Path — Date Handling

        [Fact]
        public async Task Handle_StartDateIsMinValue_TriCreatedWithNullStartDate()
        {
            SetupFullHappyPath();
            Trip? capturedTrip = null;
            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync(CreateValidTrip());

            var command = CreateValidCommand(startDate: DateOnly.MinValue);
            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            capturedTrip!.StartDate.Should().BeNull();
        }

        [Fact]
        public async Task Handle_EndDateIsMinValue_TripCreatedWithNullEndDate()
        {
            SetupFullHappyPath();
            Trip? capturedTrip = null;
            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync(CreateValidTrip());

            var command = CreateValidCommand(endDate: DateOnly.MinValue);
            await _handler.Handle(command, CancellationToken.None);

            capturedTrip.Should().NotBeNull();
            capturedTrip!.EndDate.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ValidDates_TripCreatedWithCorrectUtcDates()
        {
            SetupFullHappyPath();
            Trip? capturedTrip = null;
            _tripRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Trip>()))
                .Callback<Trip>(t => capturedTrip = t)
                .ReturnsAsync(CreateValidTrip());

            var startDate = new DateOnly(2025, 6, 1);
            var endDate = new DateOnly(2025, 6, 4);
            var command = CreateValidCommand(startDate: startDate, endDate: endDate);
            await _handler.Handle(command, CancellationToken.None);

            capturedTrip!.StartDate.Should().NotBeNull();
            capturedTrip.StartDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }

        #endregion

        #region Happy Path — Return Value

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccessWithMappedDto()
        {
            SetupFullHappyPath();
            var expectedDto = new DetailedTripDto { Id = Guid.NewGuid() };
            _mapperMock.Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>())).Returns(expectedDto);

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(expectedDto);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsGetTripWithDetailsAfterSave()
        {
            SetupFullHappyPath();

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _tripRepositoryMock.Verify(
                x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Handle_DbUpdateException_ReturnsDatabaseError()
        {
            SetupValidUser();
            SetupAllLocationsValid();
            _inviteCodeGeneratorMock.Setup(x => x.Generate(It.IsAny<CancellationToken>())).ReturnsAsync("CODE");
            SetupRepositoriesForAdd();
            _aiConvertMock.Setup(x => x.ConvertTimeOnlyToTimeSpan(It.IsAny<TimeOnly>())).Returns(TimeSpan.Zero);
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB constraint violation"));

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.DatabaseError);
        }

        [Fact]
        public async Task Handle_UnexpectedException_ReturnsUnexpectedError()
        {
            SetupValidUser();
            SetupAllLocationsValid();
            _inviteCodeGeneratorMock.Setup(x => x.Generate(It.IsAny<CancellationToken>())).ReturnsAsync("CODE");
            SetupRepositoriesForAdd();
            _aiConvertMock.Setup(x => x.ConvertTimeOnlyToTimeSpan(It.IsAny<TimeOnly>())).Returns(TimeSpan.Zero);
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

            var result = await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be(DomainErrors.Trip.UnexpectedError);
        }

        [Fact]
        public async Task Handle_DbUpdateException_LogsError()
        {
            SetupValidUser();
            SetupAllLocationsValid();
            _inviteCodeGeneratorMock.Setup(x => x.Generate(It.IsAny<CancellationToken>())).ReturnsAsync("CODE");
            SetupRepositoriesForAdd();
            _aiConvertMock.Setup(x => x.ConvertTimeOnlyToTimeSpan(It.IsAny<TimeOnly>())).Returns(TimeSpan.Zero);
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("DB error"));

            await _handler.Handle(CreateValidCommand(), CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<DbUpdateException>(),
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

        private void SetupAllLocationsValid()
        {
            _locationRepositoryMock
                .Setup(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<Guid> ids, CancellationToken _) => ids);
        }

        private void SetupRepositoriesForAdd()
        {
            _tripRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Trip>())).ReturnsAsync(CreateValidTrip());
            _tripMemberRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripMember>())).ReturnsAsync((TripMember)null!);
            _tripDayRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripDay>())).ReturnsAsync((TripDay)null!);
            _tripLocationRepositoryMock.Setup(x => x.AddAsync(It.IsAny<TripLocation>())).ReturnsAsync((TripLocation)null!);
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync((ChatMessage)null!);
        }

        private void SetupFullHappyPath()
        {
            SetupValidUser();
            SetupAllLocationsValid();
            _inviteCodeGeneratorMock.Setup(x => x.Generate(It.IsAny<CancellationToken>())).ReturnsAsync("VIVU-TEST-001");
            SetupRepositoriesForAdd();
            _aiConvertMock.Setup(x => x.ConvertTimeOnlyToTimeSpan(It.IsAny<TimeOnly>())).Returns(TimeSpan.FromHours(9));
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateValidTrip());
            _mapperMock.Setup(x => x.Map<DetailedTripDto>(It.IsAny<Trip>())).Returns(new DetailedTripDto());
        }

        #endregion

        #region Data Factory Methods

        private AICreateTripCommand CreateValidCommand(
            bool generateInviteCode = true,
            int dayCount = 1,
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var resolvedStart = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            var resolvedEnd = endDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(10));

            return new AICreateTripCommand
            {
                UserPrompt = "Lên kế hoạch đi chơi 3 ngày 2 đêm ở Huế",
                cityId = Guid.NewGuid(),
                GenerateInviteCode = generateInviteCode,
                TripPlan = CreateFakeTripPlanResponse(dayCount, resolvedStart, resolvedEnd)
            };
        }

        private AICreateTripCommand CreateValidCommandWithMultipleLocations()
        {
            var locationId3 = Guid.Parse("10000000-0000-0000-0000-000000000003");
            var locationId4 = Guid.Parse("10000000-0000-0000-0000-000000000004");

            _locationRepositoryMock
                .Setup(x => x.ValidateAllLocationId(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid> { _testLocationId1, _testLocationId2, locationId3, locationId4 });

            return new AICreateTripCommand
            {
                UserPrompt = "Đi Huế 2 ngày",
                cityId = Guid.NewGuid(),
                GenerateInviteCode = true,
                TripPlan = new TripPlanResponse
                {
                    Title = "2 Ngày ở Huế",
                    Description = "Test",
                    Start = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    End = DateOnly.FromDateTime(DateTime.Today.AddDays(9)),
                    Size = 1,
                    Days = new List<DayPlanDto>
                    {
                        CreateDayPlan(1, new List<Guid> { _testLocationId1, _testLocationId2 }),
                        CreateDayPlan(2, new List<Guid> { locationId3, locationId4 })
                    }
                }
            };
        }

        private TripPlanResponse CreateFakeTripPlanResponse(
            int dayCount = 1,
            DateOnly? startDate = null,
            DateOnly? endDate = null)
        {
            var start = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7));
            var end = endDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(10));

            return new TripPlanResponse
            {
                Title = "3 Ngày Khám Phá Huế",
                Description = "Hành trình khám phá cố đô Huế",
                Start = start,
                End = end,
                Size = 1,
                Days = Enumerable.Range(1, dayCount)
                    .Select(i => CreateDayPlan(i, new List<Guid> { _testLocationId1 }))
                    .ToList()
            };
        }

        private Trip CreateValidTrip() => Trip.Create(
            _testUserId,
            "Test Trip",
            "Test Description");

        private static DayPlanDto CreateDayPlan(int dayIndex, List<Guid> locationIds) => new DayPlanDto
        {
            DayIndex = dayIndex,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(6 + dayIndex)),
            Title = $"Ngày {dayIndex}",
            Locations = locationIds.Select((id, idx) => new LocationPlanDto
            {
                LocationId = id,
                Name = $"Địa điểm {idx + 1}",
                Description = $"Mô tả địa điểm {idx + 1}",
                StartTime = new TimeOnly(9 + idx, 0),
                EndTime = new TimeOnly(11 + idx, 0),
                TransportMode = "walking",
                OrderIndex = idx + 1
            }).ToList()
        };

        #endregion
    }
}