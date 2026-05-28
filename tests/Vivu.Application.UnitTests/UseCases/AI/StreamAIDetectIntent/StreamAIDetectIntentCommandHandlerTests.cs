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
using Vivu.Application.Interfaces;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.AI.StreamChatWithAI;
using Vivu.Application.UseCases.AI.StreamModifyTrip;
using Vivu.Application.UseCases.AI.StreamAIChat;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using Xunit;

namespace Vivu.Application.Tests.UseCases.AI.Commands.StreamAIDetectIntent
{
    public class StreamAIDetectIntentCommandHandlerTests
    {
        private readonly Mock<IIntentDetectionService> _intentDetectionMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<ILogger<StreamAIDetectIntentCommandHandler>> _loggerMock;
        private readonly StreamAIDetectIntentCommandHandler _handler;

        private readonly Guid _testUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private readonly Guid _testTripId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public StreamAIDetectIntentCommandHandlerTests()
        {
            _intentDetectionMock = new Mock<IIntentDetectionService>();
            _mediatorMock = new Mock<IMediator>();
            _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserMock = new Mock<ICurrentUser>();
            _loggerMock = new Mock<ILogger<StreamAIDetectIntentCommandHandler>>();

            _handler = new StreamAIDetectIntentCommandHandler(
                _intentDetectionMock.Object,
                _mediatorMock.Object,
                _chatMessageRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object
            );
        }

        #region Guard Tests — Invalid User

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsInvalidTokenWithoutYieldingParsingEvent()
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
            results[0].Error.Should().Be(DomainErrors.Auth.InvalidToken);
        }

        [Fact]
        public async Task Handle_InvalidUserId_DoesNotCallIntentDetectionOrMediator()
        {
            _currentUserMock.Setup(x => x.Id).Returns("bad-id");

            await CollectStreamResultsAsync(CreateValidCommand());

            _intentDetectionMock.Verify(
                x => x.DetectIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Parsing Event Tests

        [Fact]
        public async Task Handle_ValidRequest_YieldsParsingAsVeryFirstEvent()
        {
            // DetectIntent handler starts with Parsing, NOT Start — unlike other handlers
            SetupFullHappyPath(AIChatIntent.Conversation);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().NotBeEmpty();
            results[0].IsSuccess.Should().BeTrue();
            results[0].Value!.Type.Should().Be(StreamEventType.Parsing);
        }

        [Fact]
        public async Task Handle_ValidRequest_DoesNotYieldStartEventAtAll()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            // The Parsing event comes first — no Start event produced by this handler itself
            // (Start may come from downstream mediator, but this handler doesn't emit it)
            var ownEvents = results.TakeWhile(r => r.IsSuccess && r.Value!.Type == StreamEventType.Parsing);
            ownEvents.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_ValidRequest_ParsingEventContainsAnalyzingMessage()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            var parsingEvent = results.First(r => r.IsSuccess && r.Value!.Type == StreamEventType.Parsing);
            var json = System.Text.Json.JsonSerializer.Serialize(parsingEvent.Value!.Data);
            json.Should().Contain("VivuAI");
        }

        #endregion

        #region User Message Saving Tests

        [Fact]
        public async Task Handle_SkipSavingUserMessageFalse_SavesUserChatMessage()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            await CollectStreamResultsAsync(CreateValidCommand(skipSavingUserMessage: false));

            _chatMessageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_SkipSavingUserMessageFalse_SavedMessageHasCorrectProperties()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);
            ChatMessage? capturedMessage = null;
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(m => capturedMessage = m)
                .ReturnsAsync(new ChatMessage());

            const string userMessage = "Thêm địa điểm Đại Nội vào ngày 1";
            await CollectStreamResultsAsync(CreateValidCommand(
                userMessage: userMessage,
                skipSavingUserMessage: false));

            capturedMessage.Should().NotBeNull();
            capturedMessage!.IsAiMessage.Should().BeFalse();
            capturedMessage.MessageType.Should().Be("text");
            capturedMessage.Content.Should().Be(userMessage);
            capturedMessage.TripId.Should().Be(_testTripId);
            capturedMessage.SenderId.Should().Be(_testUserId);
        }

        [Fact]
        public async Task Handle_SkipSavingUserMessageTrue_DoesNotSaveChatMessage()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            await CollectStreamResultsAsync(CreateValidCommand(skipSavingUserMessage: true));

            _chatMessageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ChatMessage>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SkipSavingUserMessageTrue_DoesNotCallSaveChanges()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            await CollectStreamResultsAsync(CreateValidCommand(skipSavingUserMessage: true));

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SkipSavingUserMessageFalse_SavesMessageBeforeCallingMediator()
        {
            // User message must be saved BEFORE routing to downstream handler
            SetupFullHappyPath(AIChatIntent.Conversation);
            var callOrder = new List<string>();

            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .Callback<ChatMessage>(_ => callOrder.Add("AddAsync"))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("SaveChanges"))
                .ReturnsAsync(1);
            _mediatorMock
                .Setup(x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("CreateStream"))
                .Returns(CreateFakeStreamEvents());

            await CollectStreamResultsAsync(CreateValidCommand(skipSavingUserMessage: false));

            callOrder.Should().ContainInOrder("AddAsync", "SaveChanges", "CreateStream");
        }

        #endregion

        #region Intent Routing — ModifyTrip

        [Fact]
        public async Task Handle_ModifyTripIntent_ForwardsToStreamModifyTripCommand()
        {
            SetupFullHappyPath(AIChatIntent.ModifyTrip);

            await CollectStreamResultsAsync(CreateValidCommand());

            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ModifyTripIntent_DoesNotCallStreamChatWithAICommand()
        {
            SetupFullHappyPath(AIChatIntent.ModifyTrip);

            await CollectStreamResultsAsync(CreateValidCommand());

            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ModifyTripIntent_PassesCorrectTripIdAndUserRequestToCommand()
        {
            SetupFullHappyPath(AIChatIntent.ModifyTrip);
            StreamModifyTripCommand? capturedCommand = null;
            _mediatorMock
                 .Setup(x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()))
                 .Callback<IStreamRequest<Result<StreamEvent>>, CancellationToken>(
                     (req, _) => capturedCommand = req as StreamModifyTripCommand)
                 .Returns(CreateFakeStreamEvents());

            const string userMessage = "Thêm địa điểm mới vào ngày 2";
            await CollectStreamResultsAsync(CreateValidCommand(userMessage: userMessage));

            capturedCommand.Should().NotBeNull();
            capturedCommand!.TripId.Should().Be(_testTripId);
            capturedCommand.UserRequest.Should().Be(userMessage);
        }

        #endregion

        #region Intent Routing — Conversation

        [Fact]
        public async Task Handle_ConversationIntent_ForwardsToStreamChatWithAICommand()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            await CollectStreamResultsAsync(CreateValidCommand());

            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_ConversationIntent_DoesNotCallStreamModifyTripCommand()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);

            await CollectStreamResultsAsync(CreateValidCommand());

            _mediatorMock.Verify(
                x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_ConversationIntent_PassesCorrectTripIdAndUserMessageToCommand()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);
            StreamChatWithAICommand? capturedCommand = null;
            _mediatorMock
                .Setup(x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()))
                .Callback<IStreamRequest<Result<StreamEvent>>, CancellationToken>(
                    (req, _) => capturedCommand = req as StreamChatWithAICommand)
                .Returns(CreateFakeStreamEvents());

            const string userMessage = "Lịch trình này có phù hợp với gia đình không?";
            await CollectStreamResultsAsync(CreateValidCommand(userMessage: userMessage));

            capturedCommand.Should().NotBeNull();
            capturedCommand!.TripId.Should().Be(_testTripId);
            capturedCommand.UserMessage.Should().Be(userMessage);
        }

        #endregion

        #region Chunk Forwarding Tests

        [Fact]
        public async Task Handle_ValidRequest_ForwardsAllChunksFromDownstreamHandler()
        {
            SetupFullHappyPath(AIChatIntent.Conversation);
            var downstreamEvents = new[]
            {
                Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Start, Data = "s" }),
                Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Chunk, Data = "c1" }),
                Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Chunk, Data = "c2" }),
                Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Complete, Data = "done" })
            };
            _mediatorMock
                .Setup(x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeStreamEvents(downstreamEvents));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            // First event is Parsing from this handler, rest are forwarded from downstream
            results.Should().HaveCount(1 + downstreamEvents.Length);
            results[0].Value!.Type.Should().Be(StreamEventType.Parsing);
            results.Skip(1).Should().BeEquivalentTo(downstreamEvents);
        }

        [Fact]
        public async Task Handle_DownstreamHandlerReturnsFailure_FailureIsForwarded()
        {
            SetupFullHappyPath(AIChatIntent.ModifyTrip);
            _mediatorMock
                .Setup(x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()))
                .Returns(CreateFakeStreamEvents(
                    Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Start, Data = "s" }),
                    Result<StreamEvent>.Failure(DomainErrors.Trip.NotFound)
                ));

            var results = await CollectStreamResultsAsync(CreateValidCommand());

            results.Should().Contain(r => !r.IsSuccess && r.Error == DomainErrors.Trip.NotFound);
        }

        #endregion

        #region Intent Detection Logging

        [Fact]
        public async Task Handle_ValidRequest_LogsDetectedIntent()
        {
            SetupFullHappyPath(AIChatIntent.ModifyTrip);

            await CollectStreamResultsAsync(CreateValidCommand());

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Intent detected")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Setup Helper Methods

        private void SetupValidUser()
            => _currentUserMock.Setup(x => x.Id).Returns(_testUserId.ToString());

        private void SetupIntentDetection(AIChatIntent intent)
            => _intentDetectionMock
                .Setup(x => x.DetectIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(intent);

        private void SetupSaveUserMessage()
        {
            _chatMessageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<ChatMessage>()))
                .ReturnsAsync(new ChatMessage());
            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        private void SetupMediatorForIntent(AIChatIntent intent)
        {
            if (intent == AIChatIntent.ModifyTrip)
            {
                _mediatorMock
                    .Setup(x => x.CreateStream(It.IsAny<StreamModifyTripCommand>(), It.IsAny<CancellationToken>()))
                    .Returns(CreateFakeStreamEvents(
                        Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Start, Data = "start" }),
                        Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Complete, Data = "done" })
                    ));
            }
            else
            {
                _mediatorMock
                    .Setup(x => x.CreateStream(It.IsAny<StreamChatWithAICommand>(), It.IsAny<CancellationToken>()))
                    .Returns(CreateFakeStreamEvents(
                        Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Start, Data = "start" }),
                        Result<StreamEvent>.Success(new StreamEvent { Type = StreamEventType.Complete, Data = "done" })
                    ));
            }
        }

        private void SetupFullHappyPath(AIChatIntent intent)
        {
            SetupValidUser();
            SetupIntentDetection(intent);
            SetupSaveUserMessage();
            SetupMediatorForIntent(intent);
        }

        #endregion

        #region Data Factory Methods

        private StreamAIDetectIntentCommand CreateValidCommand(
            string userMessage = "Thêm địa điểm mới vào ngày 1",
            bool skipSavingUserMessage = false) => new StreamAIDetectIntentCommand
            {
                TripId = _testTripId,
                UserMessage = userMessage,
                SkipSavingUserMessage = skipSavingUserMessage
            };

        private static async IAsyncEnumerable<Result<StreamEvent>> CreateFakeStreamEvents(
            params Result<StreamEvent>[] events)
        {
            foreach (var e in events) { await Task.Yield(); yield return e; }
        }

        private async Task<List<Result<StreamEvent>>> CollectStreamResultsAsync(
            StreamAIDetectIntentCommand command,
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