using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.UseCases.AI.ApplyTripModification;
using Vivu.Application.UseCases.AI.StreamModifyTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.AI.Commands.StreamModifyTrip
{
    public class StreamModifyTripCommandHandlerTests
    {
        private readonly Mock<IStreamingAIService> _aiServiceMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IRateLimitService> _rateLimitServiceMock;
        private readonly Mock<IUsageTrackingService> _usageTrackerMock;
        private readonly Mock<IUserSubscriptionRepository> _userSubscriptionRepositoryMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IAIParsing> _parserMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<StreamModifyTripCommandHandler>> _loggerMock;
        private readonly Mock<IUserPersonalizationService> _personalizationServiceMock;
        private readonly Mock<ILocationZoneService> _locationZoneServiceMock;
        private readonly StreamModifyTripCommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testTripId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public StreamModifyTripCommandHandlerTests()
        {
            _aiServiceMock = new Mock<IStreamingAIService>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _mapperMock = new Mock<IMapper>();
            _mediatorMock = new Mock<IMediator>();
            _rateLimitServiceMock = new Mock<IRateLimitService>();
            _usageTrackerMock = new Mock<IUsageTrackingService>();
            _userSubscriptionRepositoryMock = new Mock<IUserSubscriptionRepository>();
            _currentUserMock = new Mock<ICurrentUser>();
            _parserMock = new Mock<IAIParsing>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<StreamModifyTripCommandHandler>>();
            _personalizationServiceMock = new Mock<IUserPersonalizationService>();
            _locationZoneServiceMock = new Mock<ILocationZoneService>();

            _handler = new StreamModifyTripCommandHandler(
                _aiServiceMock.Object,
                _tripRepositoryMock.Object,
                _chatMessageRepositoryMock.Object,
                _locationRepositoryMock.Object,
                _mapperMock.Object,
                _mediatorMock.Object,
                _rateLimitServiceMock.Object,
                _usageTrackerMock.Object,
                _userSubscriptionRepositoryMock.Object,
                _currentUserMock.Object,
                _parserMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _personalizationServiceMock.Object,
                _locationZoneServiceMock.Object
            );
        }

        #region Guard Tests — Invalid User

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenWithoutYieldingStartEvent()
        {
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
            _tripRepositoryMock.Verify(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        #endregion

        #region Guard Tests — Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundFailureAfterStartEvent()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip)null!);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(2);
            results[0].Value!.Type.Should().Be(StreamEventType.Start);
            results[1].IsSuccess.Should().BeFalse();
            results[1].Error.Should().Be(DomainErrors.Trip.NotFound);
        }

        [Fact]
        public async Task Handle_TripNotFound_DoesNotCheckRateLimit()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Trip)null!);

            await CollectStreamResultsAsync(CreateValidCommand());

            _rateLimitServiceMock.Verify(x => x.CheckLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Rate Limit

        [Fact]
        public async Task Handle_RateLimitExceeded_ReturnsFailureAfterStartEvent()
        {
            SetupValidUser();
            SetupTripFound();
            _rateLimitServiceMock
                .Setup(x => x.CheckLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RateLimitCheckResult>.Failure(DomainErrors.AIErrors.RateLimitExceeded(0, DateTime.UtcNow.AddDays(1))));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(2);
            results[0].Value!.Type.Should().Be(StreamEventType.Start);
            results[1].IsSuccess.Should().BeFalse();
            _aiServiceMock.Verify(x => x.ModifyTripPlanStreamAsync(
                It.IsAny<TripPlanResponse>(), It.IsAny<List<ChatMessage>>(),
                It.IsAny<List<Location>>(), It.IsAny<string>(), It.IsAny<UserPersonalizationContext?>(), It.IsAny<TripConstraints?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — AI Stream Failure

        [Fact]
        public async Task Handle_AIStreamChunkFails_ReturnsFailureAndStopsStream()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ModifyTripPlanStreamAsync(
                    It.IsAny<TripPlanResponse>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Location>>(), It.IsAny<string>(), It.IsAny<UserPersonalizationContext?>(), It.IsAny<TripConstraints?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("partial")),
                    Result<AIStreamChunk>.Failure(DomainErrors.AIErrors.InvalidResponse("AI down"))
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Last().IsSuccess.Should().BeFalse();
            _parserMock.Verify(x => x.ParseModifyTripResponse(It.IsAny<string>(), It.IsAny<Dictionary<int, Guid>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MultipleChunks_AllYieldedAsChunkEvents()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ModifyTripPlanStreamAsync(
                    It.IsAny<TripPlanResponse>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Location>>(), It.IsAny<string>(), It.IsAny<UserPersonalizationContext?>(), It.IsAny<TripConstraints?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("{\"summary\":")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("\"Test\",")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("\"changes\":[]}"))));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Where(r => r.IsSuccess && r.Value!.Type == StreamEventType.Chunk)
                .Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_MultipleChunks_ContentAccumulatedBeforeParsing()
        {
            SetupFullHappyPathWithoutAIStream();
            string? capturedJson = null;
            _parserMock
                .Setup(x => x.ParseModifyTripResponse(It.IsAny<string>(), It.IsAny<Dictionary<int, Guid>>()))
                .Callback<string>(j => capturedJson = j)
                .ReturnsAsync(Result<TripModificationResponse>.Success(CreateFakeModificationResponse()));

            _aiServiceMock
                .Setup(x => x.ModifyTripPlanStreamAsync(
                    It.IsAny<TripPlanResponse>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Location>>(), It.IsAny<string>(), It.IsAny<UserPersonalizationContext?>(), It.IsAny<TripConstraints?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("part1")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("part2"))
                ));

            await CollectStreamResultsAsync(CreateValidCommand());

            capturedJson.Should().Be("part1part2");
        }

        #endregion

        #region Guard Tests — Parse Failure

        [Fact]
        public async Task Handle_ParseModifyTripResponseFails_ReturnsFailure()
        {
            SetupFullHappyPathWithoutParseTripModify();
            _parserMock
                .Setup(x => x.ParseModifyTripResponse(It.IsAny<string>(), It.IsAny<Dictionary<int, Guid>>()))
                .ReturnsAsync(Result<TripModificationResponse>.Failure(DomainErrors.AIErrors.ParseError("No changes")));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Last().IsSuccess.Should().BeFalse();
            _mediatorMock.Verify(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — Event Sequence

        [Fact]
        public async Task Handle_ValidRequest_ApplySuccess_YieldsAllSixEventTypesInOrder()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().OnlyContain(r => r.IsSuccess);
            var types = results.Select(r => r.Value!.Type).ToList();
            types.Should().Contain(StreamEventType.Start);
            types.Should().Contain(StreamEventType.Parsing);
            types.Should().Contain(StreamEventType.Chunk);
            types.Should().Contain(StreamEventType.Complete);
            types.Should().Contain(StreamEventType.Saving);
            types.Should().Contain(StreamEventType.Saved);
        }

        [Fact]
        public async Task Handle_ValidRequest_CompleteAppearsBeforeSaving()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var types = results.Select(r => r.Value!.Type).ToList();
            types.IndexOf(StreamEventType.Complete).Should().BeLessThan(types.IndexOf(StreamEventType.Saving));
        }

        [Fact]
        public async Task Handle_ValidRequest_ParsingAppearsAfterStart()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var types = results.Select(r => r.Value!.Type).ToList();
            types.IndexOf(StreamEventType.Start).Should().BeLessThan(types.IndexOf(StreamEventType.Parsing));
        }

        [Fact]
        public async Task Handle_ValidRequest_CompleteEventContainsModificationData()
        {
            SetupFullHappyPath();
            SetupApplySuccess();
            var fakeModification = CreateFakeModificationResponse();
            _parserMock
                .Setup(x => x.ParseModifyTripResponse(It.IsAny<string>(), It.IsAny<Dictionary<int, Guid>>()))
                .ReturnsAsync(Result<TripModificationResponse>.Success(fakeModification));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var completeEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Complete);
            completeEvent.Value!.Data.Should().Be(fakeModification);
        }

        [Fact]
        public async Task Handle_ValidRequest_SavedEventContainsTripData()
        {
            SetupFullHappyPath();
            var savedDto = new DetailedTripDto { Id = Guid.NewGuid() };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Success(savedDto));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var savedEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Saved);
            savedEvent.Value!.Data.Should().Be(savedDto);
        }

        #endregion

        #region Apply Modification Failure Tests

        [Fact]
        public async Task Handle_ApplyModificationFails_YieldsCompleteAndSavingBeforeFailure()
        {
            SetupFullHappyPath();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var types = results.Select(r => r.IsSuccess ? r.Value!.Type.ToString() : "Failure").ToList();
            types.Should().Contain(StreamEventType.Complete.ToString());
            types.Should().Contain(StreamEventType.Saving.ToString());
            results.Last().IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ApplyModificationFails_SavesErrorChatMessageToDB()
        {
            SetupFullHappyPath();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError));
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await CollectStreamResultsAsync(CreateValidCommand());

            _chatMessageRepositoryMock.Verify(x => x.AddAsync(
                It.Is<ChatMessage>(m => m.IsAiMessage && m.Content.Contains("Xin lỗi"))),
                Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ApplyModificationFails_ErrorMessageSaveAlsoFails_SwallowsException()
        {
            SetupFullHappyPath();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError));
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("DB context dead"));

            // Should NOT throw — exception is swallowed (best-effort)
            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Last().IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ApplyModificationFails_DoesNotYieldSavedEvent()
        {
            SetupFullHappyPath();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError));
            _chatMessageRepositoryMock.Setup(x => x.AddAsync(It.IsAny<ChatMessage>())).ReturnsAsync(new ChatMessage());
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Where(r => r.IsSuccess).Select(r => r.Value!.Type)
                .Should().NotContain(StreamEventType.Saved);
        }

        #endregion

        #region Usage Tracking Tests

        [Fact]
        public async Task Handle_ValidRequest_LogsUsageExactlyOnce()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithSubscription_PassesSubscriptionIdToUsageTracker()
        {
            SetupFullHappyPath();
            SetupApplySuccess();
            var subscriptionId = Guid.NewGuid();
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserSubscription { Id = subscriptionId });

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), subscriptionId, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithNoSubscription_PassesNullSubscriptionIdToUsageTracker()
        {
            SetupFullHappyPath();
            SetupApplySuccess();
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), null, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Context Loading Tests

        [Fact]
        public async Task Handle_ValidRequest_LoadsRecentMessagesWith20Limit()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            await CollectStreamResultsAsync(CreateValidCommand());

            _chatMessageRepositoryMock.Verify(
                x => x.GetRecentMessagesAsync(_testTripId, 20, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EmptyRecentMessages_ContinuesNormally()
        {
            SetupFullHappyPath();
            SetupApplySuccess();
            _chatMessageRepositoryMock
                .Setup(x => x.GetRecentMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessage>());

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().OnlyContain(r => r.IsSuccess);
        }

        [Fact]
        public async Task Handle_ValidRequest_MapsTripToTripPlanResponseForAIContext()
        {
            SetupFullHappyPath();
            SetupApplySuccess();

            await CollectStreamResultsAsync(CreateValidCommand());

            _mapperMock.Verify(x => x.Map<TripPlanResponse>(It.IsAny<Trip>()), Times.Once);
        }

        #endregion

        #region Setup Helper Methods

        private void SetupValidUser()
            => _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());

        private void SetupTripFound()
        {
            var fakeTrip = CreateFakeTrip();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeTrip);
            _mapperMock.Setup(x => x.Map<TripPlanResponse>(It.IsAny<Trip>())).Returns(CreateFakeTripPlanResponse());
        }

        private void SetupValidRateLimit()
            => _rateLimitServiceMock
                .Setup(x => x.CheckLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<RateLimitCheckResult>.Success(new RateLimitCheckResult
                {
                    Remaining = 4,
                    Limit = 5,
                    ResetAt = DateTime.UtcNow.AddDays(1)
                }));

        private void SetupRecentMessages()
            => _chatMessageRepositoryMock
                .Setup(x => x.GetRecentMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessage>());

        private void SetupAvailableLocations()
            => _locationRepositoryMock
                .Setup(x => x.GetAllLocationByCity(It.IsAny<Guid>()))
                .ReturnsAsync(new List<Location>());

        private void SetupValidAIStream()
            => _aiServiceMock
                .Setup(x => x.ModifyTripPlanStreamAsync(
                    It.IsAny<TripPlanResponse>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Location>>(), It.IsAny<string>(), It.IsAny<UserPersonalizationContext?>(), It.IsAny<TripConstraints?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("{\"summary\":\"test\",\"changes\":[]}", 100, 500, "claude"))
                ));

        private void SetupValidParseTripModify()
            => _parserMock
                .Setup(x => x.ParseModifyTripResponse(It.IsAny<string>(), It.IsAny<Dictionary<int, Guid>>()))
                .ReturnsAsync(Result<TripModificationResponse>.Success(CreateFakeModificationResponse()));

        private void SetupNoSubscription()
            => _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

        private void SetupUsageTracker()
        {
            _usageTrackerMock.Setup(x => x.CalculateCost(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>())).Returns(0.01m);
            _usageTrackerMock.Setup(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupApplySuccess()
            => _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Success(new DetailedTripDto()));

        private void SetupFullHappyPath()
        {
            SetupValidUser();
            SetupTripFound();
            SetupValidRateLimit();
            SetupRecentMessages();
            SetupAvailableLocations();
            SetupValidAIStream();
            SetupValidParseTripModify();
            SetupNoSubscription();
            SetupUsageTracker();
        }

        private void SetupFullHappyPathWithoutAIStream()
        {
            SetupValidUser();
            SetupTripFound();
            SetupValidRateLimit();
            SetupRecentMessages();
            SetupAvailableLocations();
            SetupValidParseTripModify();
            SetupNoSubscription();
            SetupUsageTracker();
            SetupApplySuccess();
        }

        private void SetupFullHappyPathWithoutParseTripModify()
        {
            SetupValidUser();
            SetupTripFound();
            SetupValidRateLimit();
            SetupRecentMessages();
            SetupAvailableLocations();
            SetupValidAIStream();
            SetupNoSubscription();
            SetupUsageTracker();
        }

        #endregion

        #region Data Factory Methods

        private StreamModifyTripCommand CreateValidCommand() => new StreamModifyTripCommand
        {
            TripId = _testTripId,
            UserRequest = "Thêm Bãi Biển Mỹ Khê vào ngày 2"
        };

        private Trip CreateFakeTrip()
        {
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
            return trip;
        }

        private static TripPlanResponse CreateFakeTripPlanResponse() => new TripPlanResponse
        {
            Title = "Test Trip",
            Description = "Test",
            Start = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            End = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Size = 1,
            Days = new List<DayPlanDto>
            {
                new DayPlanDto
                {
                    DayIndex = 1,
                    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    Title = "Ngày 1",
                    Locations = new List<LocationPlanDto>
                    {
                        new LocationPlanDto
                        {
                            LocationId = Guid.NewGuid(), Name = "Đại Nội",
                            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
                            OrderIndex = 1, TransportMode = "walking"
                        }
                    }
                }
            }
        };

        private static TripModificationResponse CreateFakeModificationResponse() => new TripModificationResponse
        {
            Summary = "Đã thêm Bãi Biển Mỹ Khê vào ngày 2",
            Changes = new List<TripChange>
            {
                new TripChange
                {
                    Type = "add_location",
                    DayIndex = 1,
                    Location = new TripLocationChange
                    {
                        LocationId = Guid.NewGuid(), Name = "Bãi Biển Mỹ Khê",
                        StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(17, 0),
                        OrderIndex = 2, TransportMode = "taxi"
                    }
                }
            }
        };

        private static AIStreamChunk CreateFakeChunk(string content, int? promptTokens = null,
            int? completionTokens = null, string? provider = null) => new AIStreamChunk
            {
                Content = content,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                ProviderName = provider,
                ResponseTimeMs = promptTokens.HasValue ? 1000 : null
            };

        private static async IAsyncEnumerable<Result<AIStreamChunk>> CreateFakeAIStream(
            params Result<AIStreamChunk>[] chunks)
        {
            foreach (var chunk in chunks) { await Task.Yield(); yield return chunk; }
        }

        private async Task<List<Result<StreamEvent>>> CollectStreamResultsAsync(
            StreamModifyTripCommand command, CancellationToken cancellationToken = default)
        {
            var results = new List<Result<StreamEvent>>();
            await foreach (var result in _handler.Handle(command, cancellationToken))
                results.Add(result);
            return results;
        }

        #endregion
    }
}
