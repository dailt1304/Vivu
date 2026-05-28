using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.AI.StreamChatWithAI;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;
using static Vivu.Domain.Errors.DomainErrors;

namespace Vivu.Application.Tests.UseCases.AI.Commands.StreamChatWithAI
{
    public class StreamChatWithAICommandHandlerTests
    {
        private readonly Mock<IAIClientFactory> _clientFactoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ITripRepository> _tripRepositoryMock;
        private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
        private readonly Mock<IStreamingAIService> _aiServiceMock;
        private readonly Mock<IUserSubscriptionRepository> _userSubscriptionRepositoryMock;
        private readonly Mock<IUsageTrackingService> _usageTrackerMock;
        private readonly Mock<IRateLimitService> _rateLimitServiceMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<StreamChatWithAICommandHandler>> _loggerMock;
        private readonly StreamChatWithAICommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testTripId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public StreamChatWithAICommandHandlerTests()
        {
            _clientFactoryMock = new Mock<IAIClientFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tripRepositoryMock = new Mock<ITripRepository>();
            _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
            _aiServiceMock = new Mock<IStreamingAIService>();
            _userSubscriptionRepositoryMock = new Mock<IUserSubscriptionRepository>();
            _usageTrackerMock = new Mock<IUsageTrackingService>();
            _rateLimitServiceMock = new Mock<IRateLimitService>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<StreamChatWithAICommandHandler>>();

            _handler = new StreamChatWithAICommandHandler(
                _clientFactoryMock.Object,
                _unitOfWorkMock.Object,
                _tripRepositoryMock.Object,
                _chatMessageRepositoryMock.Object,
                _aiServiceMock.Object,
                _userSubscriptionRepositoryMock.Object,
                _usageTrackerMock.Object,
                _rateLimitServiceMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
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
            _tripRepositoryMock.Verify(
                x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_NullUserId_ReturnsInvalidTokenFailure()
        {
            _currentUserMock.Setup(x => x.Id).Returns((string)null!);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(1);
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_DoesNotCallAnyDownstreamService()
        {
            _currentUserMock.Setup(x => x.Id).Returns("bad-id");

            await CollectStreamResultsAsync(CreateValidCommand());

            _aiServiceMock.Verify(
                x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _chatMessageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — Trip Not Found

        [Fact]
        public async Task Handle_TripNotFound_ReturnsNotFoundFailureAfterStartEvent()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Trip)null!);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().HaveCount(2);
            results[0].IsSuccess.Should().BeTrue();
            results[0].Value!.Type.Should().Be(StreamEventType.Start);
            results[1].IsSuccess.Should().BeFalse();
            results[1].Error.Should().Be(DomainErrors.Trip.NotFound);
        }

        [Fact]
        public async Task Handle_TripNotFound_DoesNotCallAIService()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Trip)null!);

            await CollectStreamResultsAsync(CreateValidCommand());

            _aiServiceMock.Verify(
                x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_TripNotFound_DoesNotSaveAnything()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Trip)null!);

            await CollectStreamResultsAsync(CreateValidCommand());

            _chatMessageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Guard Tests — AI Stream Failure

        [Fact]
        public async Task Handle_AIStreamChunkFails_ReturnsFailureAndStopsStream()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("partial")),
                    Result<AIStreamChunk>.Failure(AIErrors.InvalidResponse("AI unavailable"))
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Last().IsSuccess.Should().BeFalse();
            _chatMessageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AIStreamChunkFails_DoesNotLogUsage()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Failure(AIErrors.InvalidResponse("fail"))
                ));

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path — Event Sequence

        [Fact]
        public async Task Handle_ValidRequest_YieldsStartThenChunksThenComplete()
        {
            SetupFullHappyPath();

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().OnlyContain(r => r.IsSuccess);
            var types = results.Select(r => r.Value!.Type).ToList();
            types.First().Should().Be(StreamEventType.Start);
            types.Should().Contain(StreamEventType.Chunk);
            types.Last().Should().Be(StreamEventType.Complete);
        }

        [Fact]
        public async Task Handle_ValidRequest_DoesNotYieldParsingOrSavingEvents()
        {
            // Chat handler has simpler flow: no Parsing, Saving, Saved events
            SetupFullHappyPath();

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var types = results.Select(r => r.Value!.Type);
            types.Should().NotContain(StreamEventType.Parsing);
            types.Should().NotContain(StreamEventType.Saving);
            types.Should().NotContain(StreamEventType.Saved);
        }

        [Fact]
        public async Task Handle_MultipleChunks_AllYieldedAsChunkEvents()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("Xin chào! ")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("Tôi có thể ")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("giúp gì cho bạn?"))
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var chunkEvents = results.Where(r => r.IsSuccess && r.Value!.Type == StreamEventType.Chunk).ToList();
            chunkEvents.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_ValidRequest_CompleteEventContainsFullAccumulatedResponse()
        {
            SetupFullHappyPathWithoutAIStream();
            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("Hello ")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("World"))
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var completeEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Complete);
            var json = JsonSerializer.Serialize(completeEvent.Value!.Data);
            json.Should().Contain("Hello World");
        }

        #endregion

        #region Side Effects — Chat Message Saved

        [Fact]
        public async Task Handle_ValidRequest_SavesAIChatMessageWithCorrectMessageType()
        {
            SetupFullHappyPath();
            ChatMessage? capturedMessage = null;
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => capturedMessage = m)
                .ReturnsAsync(new ChatMessage());

            await CollectStreamResultsAsync(CreateValidCommand());

            capturedMessage.Should().NotBeNull();
            capturedMessage!.IsAiMessage.Should().BeTrue();
            capturedMessage.MessageType.Should().Be("ai_response");
        }

        [Fact]
        public async Task Handle_ValidRequest_SavedChatMessageContentMatchesAccumulatedChunks()
        {
            SetupFullHappyPathWithoutAIStream();
            ChatMessage? capturedMessage = null;
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => capturedMessage = m)
                .ReturnsAsync(new ChatMessage());
            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("chunk1")),
                    Result<AIStreamChunk>.Success(CreateFakeChunk("chunk2"))
                ));

            await CollectStreamResultsAsync(CreateValidCommand());

            capturedMessage!.Content.Should().Be("chunk1chunk2");
        }

        [Fact]
        public async Task Handle_ValidRequest_SavedChatMessageBelongsToCorrectTrip()
        {
            SetupFullHappyPath();
            ChatMessage? capturedMessage = null;
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => capturedMessage = m)
                .ReturnsAsync(new ChatMessage());

            await CollectStreamResultsAsync(CreateValidCommand());

            capturedMessage!.TripId.Should().Be(_testTripId);
            capturedMessage.SenderId.Should().Be(_testUserId);
        }

        [Fact]
        public async Task Handle_ValidRequest_CallsSaveChangesExactlyOnce()
        {
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand());

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_SaveChangesCalledAfterCompleteResponse()
        {
            // Verify order: stream finishes → save → then yield Complete
            SetupFullHappyPath();
            var saveOrder = new List<string>();

            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(_ => saveOrder.Add("AddAsync"))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => saveOrder.Add("SaveChanges"))
                .ReturnsAsync(1);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            saveOrder.Should().ContainInOrder("AddAsync", "SaveChanges");
            // Complete comes after save
            results.Last().Value!.Type.Should().Be(StreamEventType.Complete);
        }

        #endregion

        #region Context Loading Tests

        [Fact]
        public async Task Handle_ValidRequest_LoadsRecentMessagesWith10Limit()
        {
            // Chat handler uses limit 10 (not 20 like modify trip)
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand());

            _chatMessageRepositoryMock.Verify(
                x => x.GetRecentMessagesAsync(_testTripId, 10, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_PassesUserMessageToAIService()
        {
            SetupFullHappyPath();
            const string userMessage = "Cho tôi biết thêm về Đại Nội";

            await CollectStreamResultsAsync(new StreamChatWithAICommand
            {
                TripId = _testTripId,
                UserMessage = userMessage
            });

            _aiServiceMock.Verify(
                x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    userMessage, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_EmptyRecentMessages_ContinuesNormally()
        {
            SetupFullHappyPath();
            _chatMessageRepositoryMock
                .Setup(x => x.GetRecentMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessage>());

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().OnlyContain(r => r.IsSuccess);
        }

        #endregion

        #region Rate Limit Tests — Commented Out

        [Fact]
        public async Task Handle_RateLimitIsNotEnforced_CheckLimitNeverCalled()
        {
            // Rate limit is commented out in the handler — verify it stays that way
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand());

            _rateLimitServiceMock.Verify(
                x => x.CheckLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Usage Tracking Tests

        [Fact]
        public async Task Handle_ValidRequest_LogsUsageExactlyOnce()
        {
            SetupFullHappyPath();

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ChunkWithTokenData_LogsUsageWithCorrectTokens()
        {
            SetupFullHappyPathWithoutAIStream();
            const int promptTokens = 200;
            const int completionTokens = 350;
            const string provider = "claude";

            _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(new AIStreamChunk
                    {
                        Content = "Xin chào!",
                        PromptTokens = promptTokens,
                        CompletionTokens = completionTokens,
                        ProviderName = provider,
                        ResponseTimeMs = 800
                    })
                ));

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(),
                provider, promptTokens, completionTokens, 800,
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithActiveSubscription_PassesSubscriptionIdToUsageTracker()
        {
            SetupFullHappyPath();
            var subscriptionId = Guid.NewGuid();
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserSubscription { Id = subscriptionId });

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), subscriptionId, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithNoSubscription_PassesNullSubscriptionIdToUsageTracker()
        {
            SetupFullHappyPath();
            _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

            await CollectStreamResultsAsync(CreateValidCommand());

            _usageTrackerMock.Verify(x => x.LogUsageAsync(
                It.IsAny<Guid>(), null, It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task Handle_CancellationRequestedDuringTripFetch_ThrowsOperationCanceledException()
        {
            SetupValidUser();
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
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

        private void SetupTripFound()
        {
            var fakeTrip = Domain.Entities.Trip.Create(
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
            _tripRepositoryMock
                .Setup(x => x.GetTripByIdWithDetailsAsync(_testTripId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeTrip);
        }

        private void SetupRecentMessages()
            => _chatMessageRepositoryMock
                .Setup(x => x.GetRecentMessagesAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ChatMessage>());

        private void SetupValidAIStream()
            => _aiServiceMock
                .Setup(x => x.ChatMessageStreamAsync(
                    It.IsAny<Domain.Entities.Trip>(), It.IsAny<List<ChatMessage>>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeAIStream(
                    Result<AIStreamChunk>.Success(CreateFakeChunk("Xin chào!", 100, 200, "claude"))
                ));

        private void SetupNoSubscription()
            => _userSubscriptionRepositoryMock
                .Setup(x => x.GetUserActiveSubscription(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserSubscription?)null);

        private void SetupUsageTracker()
        {
            _usageTrackerMock
                .Setup(x => x.CalculateCost(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
                .Returns(0.005m);
            _usageTrackerMock
                .Setup(x => x.LogUsageAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<decimal>(), It.IsAny<int>(), It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupSave()
        {
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        private void SetupFullHappyPath()
        {
            SetupValidUser();
            SetupTripFound();
            SetupRecentMessages();
            SetupValidAIStream();
            SetupNoSubscription();
            SetupUsageTracker();
            SetupSave();
        }

        private void SetupFullHappyPathWithoutAIStream()
        {
            SetupValidUser();
            SetupTripFound();
            SetupRecentMessages();
            SetupNoSubscription();
            SetupUsageTracker();
            SetupSave();
        }

        #endregion

        #region Data Factory Methods

        private StreamChatWithAICommand CreateValidCommand() => new StreamChatWithAICommand
        {
            TripId = _testTripId,
            UserMessage = "Cho tôi biết thêm về các địa điểm trong lịch trình"
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
                ResponseTimeMs = promptTokens.HasValue ? 800 : null
            };

        private static async IAsyncEnumerable<Result<AIStreamChunk>> CreateFakeAIStream(
            params Result<AIStreamChunk>[] chunks)
        {
            foreach (var chunk in chunks) { await Task.Yield(); yield return chunk; }
        }

        private async Task<List<Result<StreamEvent>>> CollectStreamResultsAsync(
            StreamChatWithAICommand command,
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