using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Trips;
using Vivu.Application.UseCases.AI.CreateTrip;
using Vivu.Application.UseCases.AI.StreamGenerateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;
using static Vivu.Domain.Errors.DomainErrors;
using LocationEntity = Vivu.Domain.Entities.Location;

namespace Vivu.Application.Tests.UseCases.AI.Commands.StreamGenerateTrip
{
    public class StreamGenerateTripCommandHandlerTests
    {
        private readonly Mock<IAIParsing> _parserMock;
        private readonly Mock<IRateLimitService> _rateLimitServiceMock;
        private readonly Mock<IStreamingAIService> _aiServiceMock;
        private readonly Mock<ICityRepository> _cityRepositoryMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly Mock<ILocationZoneService> _zoneServiceMock;
        private readonly Mock<ITripItineraryOptimizer> _optimizerMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IUserSubscriptionRepository> _userSubscriptionRepositoryMock;
        private readonly Mock<IUsageTrackingService> _usageTrackerMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IUserPersonalizationService> _userPersonalizationServiceMock;
        private readonly Mock<IPromptBuilder> _promptBuilder;
        private readonly Mock<ILocationCategoryRepository> _categoryRepositoryMock;
        private readonly Mock<ITripLimitChecker> _tripLimitCheckerMock;
        private readonly Mock<ILogger<StreamGenerateTripCommandHandler>> _loggerMock;
        private readonly StreamGenerateTripCommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testCityId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public StreamGenerateTripCommandHandlerTests()
        {
            _parserMock = new Mock<IAIParsing>();
            _rateLimitServiceMock = new Mock<IRateLimitService>();
            _aiServiceMock = new Mock<IStreamingAIService>();
            _cityRepositoryMock = new Mock<ICityRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _zoneServiceMock = new Mock<ILocationZoneService>();
            _optimizerMock = new Mock<ITripItineraryOptimizer>();
            _categoryRepositoryMock = new Mock<ILocationCategoryRepository>();
            _userPersonalizationServiceMock = new Mock<IUserPersonalizationService>();
            _promptBuilder = new Mock<IPromptBuilder>();
            _mediatorMock = new Mock<IMediator>();
            _userSubscriptionRepositoryMock = new Mock<IUserSubscriptionRepository>();
            _usageTrackerMock = new Mock<IUsageTrackingService>();
            _currentUserMock = new Mock<ICurrentUser>();
            _tripLimitCheckerMock = new Mock<ITripLimitChecker>();
            _loggerMock = new Mock<ILogger<StreamGenerateTripCommandHandler>>();

            _locationRepositoryMock
                .Setup(x => x.GetAllLocationByCity(It.IsAny<Guid>()))
                .ReturnsAsync(new List<LocationEntity>());

            _zoneServiceMock
                .Setup(x => x.SelectLocationsForTrip(
                    It.IsAny<List<LocationEntity>>(),
                    It.IsAny<int>(),
                    It.IsAny<List<string>>(),
                    It.IsAny<TripConstraints?>(),
                    It.IsAny<UserPersonalizationContext>()))
                .Returns(new List<LocationEntity>());

            _optimizerMock
                .Setup(x => x.Optimize(It.IsAny<TripPlanResponse>(), It.IsAny<List<LocationEntity>>()))
                .Returns<TripPlanResponse, List<LocationEntity>>((plan, _) => plan);

            _handler = new StreamGenerateTripCommandHandler(
                _aiServiceMock.Object,
                _parserMock.Object,
                _promptBuilder.Object,
                _rateLimitServiceMock.Object,
                _tripLimitCheckerMock.Object,
                _categoryRepositoryMock.Object,
                _cityRepositoryMock.Object,
                _locationRepositoryMock.Object,
                _zoneServiceMock.Object,
                _optimizerMock.Object,
                _mediatorMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object,
                _userSubscriptionRepositoryMock.Object,
                _userPersonalizationServiceMock.Object,
                _usageTrackerMock.Object
            );
        }

        #region Guard Tests — Invalid User

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns("not-a-valid-guid");

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_DoesNotCallAnyDownstreamService()
        {
            _currentUserMock.Setup(x => x.Id).Returns("bad-id");

            await CollectStreamResultsAsync(CreateValidCommand());

            _tripLimitCheckerMock.Verify(x => x.CanCreateTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _aiServiceMock.Verify(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Trip Limit

        [Fact]
        public async Task Handle_TripLimitReached_ReturnsTripLimitFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].IsSuccess.Should().BeFalse();
            results[0].Error.Should().Be(DomainErrors.Trip.TripLimitReached);
        }

        [Fact]
        public async Task Handle_TripLimitReached_LogsWarning()
        {
            _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await CollectStreamResultsAsync(CreateValidCommand());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Trip limit reached")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_TripLimitReached_DoesNotCallAIServiceOrParser()
        {
            _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await CollectStreamResultsAsync(CreateValidCommand());

            _aiServiceMock.Verify(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — AI Stream Chunk Failure

        [Fact]
        public async Task Handle_AIStreamChunkFails_ReturnsFailureAndStopsStream()
        {
            SetupValidUser();
            SetupTripLimitNotReached();
            SetupValidCity();
            SetupPromptBuilder();
            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("partial content")),
                    Result<AIStreamChunk>.Failure(AIErrors.InvalidResponse("AI service unavailable"))
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().Contain(r => !r.IsSuccess);
            results.Last().IsSuccess.Should().BeFalse();
            _parserMock.Verify(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AIStreamChunkFails_DoesNotSaveTrip()
        {
            SetupValidUser();
            SetupTripLimitNotReached();
            SetupValidCity();
            SetupPromptBuilder();
            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Failure(AIErrors.InvalidResponse("fail"))
                ));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            _mediatorMock.Verify(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Parse Trip Plan Response Failure

        [Fact]
        public async Task Handle_ParseTripPlanResponseFails_ReturnsFailure()
        {
            SetupFullHappyPathWithoutParseTripPlan();
            _parserMock
                .Setup(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()))
                .ReturnsAsync(Result<TripPlanResponse>.Failure(AIErrors.ParseError("Missing title")));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Last().IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ParseTripPlanResponseFails_DoesNotCallMediator()
        {
            SetupFullHappyPathWithoutParseTripPlan();
            _parserMock
                .Setup(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()))
                .ReturnsAsync(Result<TripPlanResponse>.Failure(AIErrors.ParseError("No days")));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            _mediatorMock.Verify(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — AutoSave False

        [Fact]
        public async Task Handle_AutoSaveFalse_YieldsStartParsingChunksCompleteInOrder()
        {
            SetupFullHappyPath();

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            results.Should().OnlyContain(r => r.IsSuccess);
            var types = results.Select(r => r.Value!.Type).ToList();
            types[0].Should().Be(StreamEventType.Start);
            types.Should().Contain(StreamEventType.Chunk);
            types.Last().Should().Be(StreamEventType.Complete);
        }

        [Fact]
        public async Task Handle_AutoSaveFalse_DoesNotYieldSavingOrSavedEvents()
        {
            SetupFullHappyPath();

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            results.Select(r => r.Value!.Type).Should().NotContain(StreamEventType.Saving);
            results.Select(r => r.Value!.Type).Should().NotContain(StreamEventType.Saved);
        }

        [Fact]
        public async Task Handle_AutoSaveFalse_DoesNotCallMediator()
        {
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _mediatorMock.Verify(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — AutoSave True

        [Fact]
        public async Task Handle_AutoSaveTrue_SaveSucceeds_YieldsAllSixEventTypes()
        {
            SetupFullHappyPath();
            SetupMediatorSuccess();

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            results.Should().OnlyContain(r => r.IsSuccess);
            var types = results.Select(r => r.Value!.Type).ToList();
            types.Should().Contain(StreamEventType.Start);
            types.Should().Contain(StreamEventType.Chunk);
            types.Should().Contain(StreamEventType.Complete);
            types.Should().Contain(StreamEventType.Saving);
            types.Should().Contain(StreamEventType.Saved);
        }

        [Fact]
        public async Task Handle_AutoSaveTrue_CompleteEventAppearsBeforeSavingEvent()
        {
            SetupFullHappyPath();
            SetupMediatorSuccess();

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            var types = results.Select(r => r.Value!.Type).ToList();
            var completeIndex = types.IndexOf(StreamEventType.Complete);
            var savingIndex = types.IndexOf(StreamEventType.Saving);
            completeIndex.Should().BeLessThan(savingIndex);
        }

        [Fact]
        public async Task Handle_AutoSaveTrue_SaveFails_YieldsFailureAfterComplete()
        {
            SetupFullHappyPath();
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Failure(DomainErrors.Trip.DatabaseError));

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            results.Should().Contain(r => r.IsSuccess && r.Value!.Type == StreamEventType.Complete);
            results.Last().IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_AutoSaveTrue_SaveSucceeds_SavedEventContainsTripData()
        {
            SetupFullHappyPath();
            var savedDto = new DetailedTripDto { Id = Guid.NewGuid() };
            _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Success(savedDto));

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: true));

            var savedEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Saved);
            savedEvent.Value!.Data.Should().Be(savedDto);
        }

        #endregion

        #region Chunk Accumulation Tests

        [Fact]
        public async Task Handle_MultipleChunks_AllYieldedAsChunkEvents()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("{\"title\":")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("\"Test Trip\",")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("\"days\":[]}"))));

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            var chunkEvents = results.Where(r => r.IsSuccess && r.Value!.Type == StreamEventType.Chunk).ToList();
            chunkEvents.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_MultipleChunks_ContentAccumulatedForParsing()
        {
            SetupFullHappyPathWithoutAIStream();
            string capturedJson = null!;
            _parserMock
                .Setup(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()))
                .Callback<string>(json => capturedJson = json)
                .ReturnsAsync(Result<TripPlanResponse>.Success(CreateFakeTripPlanResponse()));

            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("part1")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("part2"))
                ));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            capturedJson.Should().Be("part1part2");
        }

        [Fact]
        public async Task Handle_ValidRequest_CompleteEventContainsTripPlanData()
        {
            SetupFullHappyPath();
            var fakeTripPlan = CreateFakeTripPlanResponse();
            _parserMock
                .Setup(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()))
                .ReturnsAsync(Result<TripPlanResponse>.Success(fakeTripPlan));

            var results = await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            var completeEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Complete);
            completeEvent.Value!.Data.Should().Be(fakeTripPlan);
        }

        #endregion

        #region Token & Usage Tracking Tests

        [Fact]
        public async Task Handle_ChunkWithTokenData_LogsUsageWithCorrectValues()
        {
            SetupFullHappyPathWithoutAIStream();
            const int promptTokens = 150;
            const int completionTokens = 800;
            const string providerName = "claude";
            const int responseTimeMs = 1200;

            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(new AIStreamChunk
                    {
                        Content = "{}",
                        PromptTokens = promptTokens,
                        CompletionTokens = completionTokens,
                        ProviderName = providerName,
                        ResponseTimeMs = responseTimeMs
                    })
                ));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(),
                providerName, promptTokens, completionTokens, responseTimeMs,
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ChunksWithNullTokenData_LogsUsageWithZeroTokens()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(new AIStreamChunk
                    {
                        Content = "{}",
                        PromptTokens = null,
                        CompletionTokens = null,
                        ProviderName = null,
                        ResponseTimeMs = null
                    })
                ));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), 0, 0, 0,
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_MultipleChunksWithTokens_UsesLastNonNullTokenValues()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(new AIStreamChunk { Content = "p1", PromptTokens = 100, CompletionTokens = 200 }),
                    Result<AIStreamChunk>.Success(new AIStreamChunk { Content = "p2", PromptTokens = 150, CompletionTokens = 400 })
                ));

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                150, 400,
                It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithActiveSubscription_PassesSubscriptionIdToUsageTracker()
        {
            SetupFullHappyPath();
            var subscriptionId = Guid.NewGuid();
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserSubscription { Id = subscriptionId });

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

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
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), null, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_LogsUsageExactlyOnce()
        {
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand(autoSave: false));

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task Handle_CancellationRequestedBeforeTripLimit_ThrowsOperationCanceledException()
        {
            _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());
            _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in _handler.Handle(CreateValidCommand(), CancellationToken.None)) { }
            });
        }

        #endregion

        #region Setup Helper Methods

        private void SetupValidUser()
            => _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());

        private void SetupTripLimitNotReached()
            => _tripLimitCheckerMock
                .Setup(x => x.CanCreateTripAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        private void SetupValidCity()
            => _cityRepositoryMock.Setup(x => x.GetByIdAsync(_testCityId))
                .ReturnsAsync(new City { Id = _testCityId, Name = "Huế" });

        private void SetupPromptBuilder()
            => _promptBuilder
                .Setup(x => x.BuildUserPromptFromForm(It.IsAny<StreamGenerateTripCommand>(), It.IsAny<string>()))
                .Returns("Tạo lịch trình Huế từ 01/01/2026 đến 04/01/2026");

        private void SetupValidAIStream()
            => _aiServiceMock
                .Setup(x => x.GenerateTripPlanStreamAsync(It.IsAny<TripPlanDto>(), It.IsAny<List<LocationEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("{\"title\":\"Test Trip\"}", promptTokens: 100, completionTokens: 500, provider: "claude"))
                ));

        private void SetupValidParseTripPlan()
            => _parserMock
                .Setup(x => x.ParseTripPlanResponse(It.IsAny<string>(), It.IsAny<List<LocationEntity>>()))
                .ReturnsAsync(Result<TripPlanResponse>.Success(CreateFakeTripPlanResponse()));

        private void SetupNoActiveSubscription()
            => _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

        private void SetupUsageTracker()
        {
            _usageTrackerMock
                .Setup(x => x.CalculateCost(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(0.01m);
            _usageTrackerMock
                .Setup(x => x.LogUsageAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupMediatorSuccess()
            => _mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<Result<DetailedTripDto>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<DetailedTripDto>.Success(new DetailedTripDto()));

        private void SetupFullHappyPath()
        {
            SetupValidUser();
            SetupTripLimitNotReached();
            SetupValidCity();
            SetupPromptBuilder();
            SetupValidAIStream();
            SetupValidParseTripPlan();
            SetupNoActiveSubscription();
            SetupUsageTracker();
        }

        private void SetupFullHappyPathWithoutAIStream()
        {
            SetupValidUser();
            SetupTripLimitNotReached();
            SetupValidCity();
            SetupPromptBuilder();
            SetupValidParseTripPlan();
            SetupNoActiveSubscription();
            SetupUsageTracker();
        }

        private void SetupFullHappyPathWithoutParseTripPlan()
        {
            SetupValidUser();
            SetupTripLimitNotReached();
            SetupValidCity();
            SetupPromptBuilder();
            SetupValidAIStream();
            SetupNoActiveSubscription();
            SetupUsageTracker();
        }

        #endregion

        #region Data Factory Methods

        private StreamGenerateTripCommand CreateValidCommand(bool autoSave = false) => new StreamGenerateTripCommand
        {
            CityId = _testCityId,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            AutoSave = autoSave,
            GenerateInviteCode = true
        };

        private TripPlanDto CreateFakeTripPlanDto() => new TripPlanDto
        {
            UserId = _testUserId,
            CityId = Guid.NewGuid(),
            Destination = "Huế",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            GroupSize = 1,
            Preferences = new List<string>(),
            Budget = null
        };

        private static TripPlanResponse CreateFakeTripPlanResponse() => new TripPlanResponse
        {
            Title = "3 Ngày Khám Phá Huế",
            Description = "Hành trình khám phá cố đô Huế tuyệt vời",
            Start = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            End = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            Size = 1,
            Days = new List<DayPlanDto>
            {
                new DayPlanDto
                {
                    DayIndex = 1,
                    Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    Title = "Ngày 1: Khám Phá Đại Nội",
                    Locations = new List<LocationPlanDto>
                    {
                        new LocationPlanDto
                        {
                            LocationId = Guid.NewGuid(),
                            Name = "Đại Nội Huế",
                            Description = "Thăm quan hoàng thành",
                            StartTime = new TimeOnly(9, 0),
                            EndTime = new TimeOnly(11, 30),
                            TransportMode = "walking",
                            OrderIndex = 1
                        }
                    }
                }
            }
        };

        private static AIStreamChunk CreateFakeChunk(
            string content,
            int? promptTokens = null,
            int? completionTokens = null,
            string? provider = null) => new AIStreamChunk
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
            foreach (var chunk in chunks)
            {
                await Task.Yield();
                yield return chunk;
            }
        }

        private async Task<List<Result<StreamEvent>>> CollectStreamResultsAsync(
            StreamGenerateTripCommand command,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Result<StreamEvent>>();
            await foreach (var result in _handler.Handle(command, cancellationToken))
                results.Add(result);
            return results;
        }

        #endregion
    }
}