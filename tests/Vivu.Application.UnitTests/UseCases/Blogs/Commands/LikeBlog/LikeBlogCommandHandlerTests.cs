using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Blogs.Commands.LikeBlog;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.LikeBlog
{
    public class LikeBlogCommandHandlerTests
    {
        private readonly Mock<IBlogRepository>  _blogRepositoryMock;
        private readonly Mock<IBlogLikeRepository> _blogLikeRepositoryMock;
        private readonly Mock<IUnitOfWork>      _unitOfWorkMock;
        private readonly Mock<ICurrentUser>     _currentUserMock;
        private readonly Mock<IUserRepository>  _userRepositoryMock;
        private readonly Mock<IPublisher>       _publisherMock;
        private readonly Mock<ILogger<LikeBlogCommandHandler>> _loggerMock;
        private readonly LikeBlogCommandHandler _handler;

        public LikeBlogCommandHandlerTests()
        {
            _blogRepositoryMock = new Mock<IBlogRepository>();
            _blogLikeRepositoryMock = new Mock<IBlogLikeRepository>();
            _blogLikeRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<BlogLike>()))
                .ReturnsAsync((BlogLike like) => like);
            _unitOfWorkMock     = new Mock<IUnitOfWork>();
            _currentUserMock    = new Mock<ICurrentUser>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _publisherMock      = new Mock<IPublisher>();
            _loggerMock         = new Mock<ILogger<LikeBlogCommandHandler>>();

            _handler = new LikeBlogCommandHandler(
                _blogRepositoryMock.Object,
                _blogLikeRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _userRepositoryMock.Object,
                _publisherMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog(string status = "published", int likeCount = 0)
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(),
                tripId: null,
                title: "Test Blog",
                slug: "test-blog",
                coverImageUrl: null,
                shortDescription: "Short description",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);

            blog.Status    = status;
            blog.LikeCount = likeCount;

            if (status == "published")
                blog.PublishedAt = DateTime.UtcNow;

            return blog;
        }

        private void SetupCurrentUser(string? userId = null)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId ?? Guid.NewGuid().ToString());
        }

        private void SetupBlogFound(Blog blog)
        {
            _blogRepositoryMock
                .Setup(r => r.GetByIdAsync(blog.Id))
                .ReturnsAsync(blog);
        }

        private void SetupBlogNotFound(Guid blogId)
        {
            _blogRepositoryMock
                .Setup(r => r.GetByIdAsync(blogId))
                .ReturnsAsync((Blog?)null);
        }

        private void SetupNoExistingLike(Guid blogId, Guid userId)
        {
            _blogLikeRepositoryMock
                .Setup(r => r.GetBlogLikeAsync(blogId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BlogLike?)null);
        }

        private void SetupExistingLike(Guid blogId, Guid userId)
        {
            var like = BlogLike.Create(blogId, userId);
            _blogLikeRepositoryMock
                .Setup(r => r.GetBlogLikeAsync(blogId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(like);
        }

        #endregion

        #region Auth Guard

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNull_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(u => u.Id).Returns((string?)null);
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsEmpty_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(u => u.Id).Returns(string.Empty);
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIdIsNotAGuid_ReturnsInvalidTokenError()
        {
            // Arrange
            _currentUserMock.Setup(u => u.Id).Returns("not-a-guid");
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserIsInvalid_DoesNotCallBlogRepository()
        {
            // Arrange
            _currentUserMock.Setup(u => u.Id).Returns((string?)null);
            var command = new LikeBlogCommand { BlogId = Guid.NewGuid() };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _blogRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Blog Not Found

        [Fact]
        public async Task Handle_WhenBlogNotFound_ReturnsNotFoundError()
        {
            // Arrange
            var blogId = Guid.NewGuid();
            SetupCurrentUser();
            SetupBlogNotFound(blogId);
            var command = new LikeBlogCommand { BlogId = blogId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotFound.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogNotFound_DoesNotSaveChanges()
        {
            // Arrange
            var blogId = Guid.NewGuid();
            SetupCurrentUser();
            SetupBlogNotFound(blogId);
            var command = new LikeBlogCommand { BlogId = blogId };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Deleted Blog

        [Fact]
        public async Task Handle_WhenBlogIsDeleted_ReturnsHasDeletedError()
        {
            // Arrange
            var blog = CreateBlog(status: "deleted");
            SetupCurrentUser();
            SetupBlogFound(blog);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.HasDeleted.Code);
        }

        #endregion

        #region Not Published Blog

        [Theory]
        [InlineData("draft")]
        [InlineData("archived")]
        [InlineData("pending")]
        public async Task Handle_WhenBlogIsNotPublished_ReturnsNotPublishedError(string status)
        {
            // Arrange
            var blog = CreateBlog(status: status);
            SetupCurrentUser();
            SetupBlogFound(blog);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotPublished.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogIsNotPublished_DoesNotSaveChanges()
        {
            // Arrange
            var blog = CreateBlog(status: "draft");
            SetupCurrentUser();
            SetupBlogFound(blog);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Toggle Like ON (first like)

        [Fact]
        public async Task Handle_WhenNoExistingLike_AddsLikeAndReturnsIsLikedTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 5);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupNoExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.IsLiked.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenNoExistingLike_IncreasesLikeCountByOne()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 5);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupNoExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value!.LikeCount.Should().Be(6);
            blog.LikeCount.Should().Be(6);
        }

        [Fact]
        public async Task Handle_WhenNoExistingLike_CallsAddAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published");
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupNoExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _blogLikeRepositoryMock.Verify(
                r => r.AddAsync(It.Is<BlogLike>(l => l.BlogId == blog.Id && l.UserId == userId)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WhenNoExistingLike_SavesChanges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published");
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupNoExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Toggle Like OFF (remove like)

        [Fact]
        public async Task Handle_WhenExistingLike_RemovesLikeAndReturnsIsLikedFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 3);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.IsLiked.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_WhenExistingLike_DecreasesLikeCountByOne()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 3);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value!.LikeCount.Should().Be(2);
            blog.LikeCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenExistingLikeAndLikeCountIsZero_DoesNotGoNegative()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 0);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Value!.LikeCount.Should().Be(0);
            blog.LikeCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenExistingLike_CallsRemove()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 1);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _blogLikeRepositoryMock.Verify(r => r.Remove(It.IsAny<BlogLike>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenExistingLike_DoesNotCallAddAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 1);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _blogLikeRepositoryMock.Verify(r => r.AddAsync(It.IsAny<BlogLike>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenExistingLike_SavesChanges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreateBlog(status: "published", likeCount: 1);
            SetupCurrentUser(userId.ToString());
            SetupBlogFound(blog);
            SetupExistingLike(blog.Id, userId);
            var command = new LikeBlogCommand { BlogId = blog.Id };

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
