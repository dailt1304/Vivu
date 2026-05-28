using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Blogs.Commands.CreateComment;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.CreateComment
{
    public class CreateCommentCommandHandlerTests
    {
        private readonly Mock<IBlogRepository>        _blogRepositoryMock;
        private readonly Mock<IBlogCommentRepository> _commentRepositoryMock;
        private readonly Mock<IUnitOfWork>            _unitOfWorkMock;
        private readonly Mock<ICurrentUser>           _currentUserMock;
        private readonly Mock<IUserRepository>        _userRepositoryMock;
        private readonly Mock<IPublisher>             _publisherMock;
        private readonly Mock<ILogger<CreateCommentCommandHandler>> _loggerMock;
        private readonly CreateCommentCommandHandler  _handler;

        public CreateCommentCommandHandlerTests()
        {
            _blogRepositoryMock    = new Mock<IBlogRepository>();
            _commentRepositoryMock = new Mock<IBlogCommentRepository>();
            _unitOfWorkMock        = new Mock<IUnitOfWork>();
            _currentUserMock       = new Mock<ICurrentUser>();
            _userRepositoryMock    = new Mock<IUserRepository>();
            _publisherMock         = new Mock<IPublisher>();
            _loggerMock            = new Mock<ILogger<CreateCommentCommandHandler>>();

            _handler = new CreateCommentCommandHandler(
                _blogRepositoryMock.Object,
                _commentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _userRepositoryMock.Object,
                _publisherMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog(string status = "published")
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(), tripId: null,
                title: "Test Blog", slug: "test-blog",
                coverImageUrl: null, shortDescription: "desc",
                travelDateStart: null, travelDateEnd: null,
                totalCost: null, groupSize: null);
            blog.Status = status;
            if (status == "published") blog.PublishedAt = DateTime.UtcNow;
            return blog;
        }

        private void SetupUser(string? id = null)
            => _currentUserMock.Setup(u => u.Id).Returns(id ?? Guid.NewGuid().ToString());

        private void SetupBlog(Blog? blog, Guid? id = null)
        {
            var resolvedId = id ?? blog?.Id ?? Guid.NewGuid();
            _blogRepositoryMock
                .Setup(r => r.GetByIdAsync(resolvedId))
                .ReturnsAsync(blog);
        }

        private static CreateCommentCommand MakeCommand(Guid? blogId = null, string content = "Nice post!")
            => new CreateCommentCommand { BlogId = blogId ?? Guid.NewGuid(), Content = content };

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_WhenCurrentUserInvalid_ReturnsInvalidToken(string? userId)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId);
            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserInvalid_DoesNotCallBlogRepository()
        {
            _currentUserMock.Setup(u => u.Id).Returns((string?)null);
            await _handler.Handle(MakeCommand(), CancellationToken.None);
            _blogRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Blog Validation

        [Fact]
        public async Task Handle_WhenBlogNotFound_ReturnsNotFoundError()
        {
            var blogId = Guid.NewGuid();
            SetupUser();
            SetupBlog(null, blogId);
            var result = await _handler.Handle(MakeCommand(blogId), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotFound.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogIsDeleted_ReturnsHasDeletedError()
        {
            var blog = CreateBlog("deleted");
            SetupUser();
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.HasDeleted.Code);
        }

        [Theory]
        [InlineData("draft")]
        [InlineData("archived")]
        [InlineData("pending")]
        public async Task Handle_WhenBlogNotPublished_ReturnsNotPublishedError(string status)
        {
            var blog = CreateBlog(status);
            SetupUser();
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotPublished.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogNotPublished_DoesNotSaveChanges()
        {
            var blog = CreateBlog("draft");
            SetupUser();
            SetupBlog(blog);
            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task Handle_WithValidRequest_ReturnsSuccess()
        {
            var blog = CreateBlog();
            SetupUser();
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id, "Great blog!"), CancellationToken.None);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithValidRequest_ReturnsCorrectContent()
        {
            var blog    = CreateBlog();
            var content = "This is my comment.";
            SetupUser();
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id, content), CancellationToken.None);
            result.Value!.Content.Should().Be(content);
            result.Value.BlogId.Should().Be(blog.Id);
        }

        [Fact]
        public async Task Handle_WithValidRequest_IncrementsBlogCommentCount()
        {
            var blog = CreateBlog();
            blog.CommentCount = 3;
            SetupUser();
            SetupBlog(blog);
            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            blog.CommentCount.Should().Be(4);
        }

        [Fact]
        public async Task Handle_WithValidRequest_CallsAddAsyncOnCommentRepository()
        {
            var blog = CreateBlog();
            SetupUser();
            SetupBlog(blog);
            await _handler.Handle(MakeCommand(blog.Id, "Hello"), CancellationToken.None);
            _commentRepositoryMock.Verify(
                r => r.AddAsync(It.Is<BlogComment>(c => c.BlogId == blog.Id && c.Content == "Hello")),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidRequest_SavesChanges()
        {
            var blog = CreateBlog();
            SetupUser();
            SetupBlog(blog);
            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithValidRequest_SetsCorrectUserId()
        {
            var userId = Guid.NewGuid();
            var blog   = CreateBlog();
            SetupUser(userId.ToString());
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            result.Value!.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task Handle_WithValidRequest_LikeCountIsZero()
        {
            var blog = CreateBlog();
            SetupUser();
            SetupBlog(blog);
            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);
            result.Value!.LikeCount.Should().Be(0);
        }

        #endregion
    }
}
