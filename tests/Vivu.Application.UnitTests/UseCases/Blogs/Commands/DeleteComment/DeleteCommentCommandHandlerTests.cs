using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Blogs.Commands.DeleteComment;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.DeleteComment
{
    public class DeleteCommentCommandHandlerTests
    {
        private readonly Mock<IBlogRepository>        _blogRepositoryMock;
        private readonly Mock<IBlogCommentRepository> _commentRepositoryMock;
        private readonly Mock<IUnitOfWork>            _unitOfWorkMock;
        private readonly Mock<ICurrentUser>           _currentUserMock;
        private readonly Mock<ILogger<DeleteCommentCommandHandler>> _loggerMock;
        private readonly DeleteCommentCommandHandler  _handler;

        public DeleteCommentCommandHandlerTests()
        {
            _blogRepositoryMock    = new Mock<IBlogRepository>();
            _commentRepositoryMock = new Mock<IBlogCommentRepository>();
            _unitOfWorkMock        = new Mock<IUnitOfWork>();
            _currentUserMock       = new Mock<ICurrentUser>();
            _loggerMock            = new Mock<ILogger<DeleteCommentCommandHandler>>();

            _handler = new DeleteCommentCommandHandler(
                _blogRepositoryMock.Object,
                _commentRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog(Guid? userId = null)
        {
            var blog = Blog.Create(
                userId: userId ?? Guid.NewGuid(), tripId: null,
                title: "Test Blog", slug: "test-blog",
                coverImageUrl: null, shortDescription: "desc",
                travelDateStart: null, travelDateEnd: null,
                totalCost: null, groupSize: null);
            blog.Status      = "published";
            blog.CommentCount = 5;
            return blog;
        }

        private static BlogComment CreateComment(Guid blogId, Guid userId, string content = "test comment")
            => BlogComment.Create(blogId, userId, content);

        private void SetupUser(Guid? userId = null)
            => _currentUserMock.Setup(u => u.Id).Returns((userId ?? Guid.NewGuid()).ToString());

        private void SetupCommentFound(BlogComment comment)
            => _commentRepositoryMock
                .Setup(r => r.GetByIdAsync(comment.Id))
                .ReturnsAsync(comment);

        private void SetupCommentNotFound(Guid commentId)
            => _commentRepositoryMock
                .Setup(r => r.GetByIdAsync(commentId))
                .ReturnsAsync((BlogComment?)null);

        private void SetupBlogForUpdate(Blog blog)
            => _blogRepositoryMock
                .Setup(r => r.GetByIdAsync(blog.Id))
                .ReturnsAsync(blog);

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_WhenCurrentUserInvalid_ReturnsInvalidToken(string? userId)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId);
            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = Guid.NewGuid() }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
        }

        [Fact]
        public async Task Handle_WhenCurrentUserInvalid_DoesNotCallCommentRepository()
        {
            _currentUserMock.Setup(u => u.Id).Returns((string?)null);
            await _handler.Handle(
                new DeleteCommentCommand { CommentId = Guid.NewGuid() }, CancellationToken.None);
            _commentRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Comment Not Found

        [Fact]
        public async Task Handle_WhenCommentNotFound_ReturnsNotFoundError()
        {
            var commentId = Guid.NewGuid();
            SetupUser();
            SetupCommentNotFound(commentId);
            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = commentId }, CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.BlogComment.NotFound.Code);
        }

        [Fact]
        public async Task Handle_WhenCommentNotFound_DoesNotSaveChanges()
        {
            var commentId = Guid.NewGuid();
            SetupUser();
            SetupCommentNotFound(commentId);
            await _handler.Handle(
                new DeleteCommentCommand { CommentId = commentId }, CancellationToken.None);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Access Denied

        [Fact]
        public async Task Handle_WhenUserIsNeitherCommentNorBlogOwner_ReturnsAccessDenied()
        {
            var commentOwner = Guid.NewGuid();
            var blogOwner    = Guid.NewGuid();
            var randomUser   = Guid.NewGuid();

            var blog    = CreateBlog(blogOwner);
            var comment = CreateComment(blog.Id, commentOwner);

            SetupUser(randomUser);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.BlogComment.AccessDenied.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogNotFoundDuringAccessCheck_ReturnsAccessDenied()
        {
            var commentOwner = Guid.NewGuid();
            var randomUser   = Guid.NewGuid();
            var blogId       = Guid.NewGuid();

            var comment = CreateComment(blogId, commentOwner);

            SetupUser(randomUser);
            SetupCommentFound(comment);
            _blogRepositoryMock.Setup(r => r.GetByIdAsync(blogId)).ReturnsAsync((Blog?)null);

            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.BlogComment.AccessDenied.Code);
        }

        [Fact]
        public async Task Handle_WhenAccessDenied_DoesNotSaveChanges()
        {
            var commentOwner = Guid.NewGuid();
            var blogOwner    = Guid.NewGuid();
            var randomUser   = Guid.NewGuid();

            var blog    = CreateBlog(blogOwner);
            var comment = CreateComment(blog.Id, commentOwner);

            SetupUser(randomUser);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Comment Owner Deletes

        [Fact]
        public async Task Handle_WhenCommentOwnerDeletes_ReturnsSuccess()
        {
            var userId  = Guid.NewGuid();
            var blog    = CreateBlog();
            var comment = CreateComment(blog.Id, userId);

            SetupUser(userId);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenCommentOwnerDeletes_CallsRemove()
        {
            var userId  = Guid.NewGuid();
            var blog    = CreateBlog();
            var comment = CreateComment(blog.Id, userId);

            SetupUser(userId);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            _commentRepositoryMock.Verify(r => r.Remove(comment), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenCommentOwnerDeletes_DecrementsBlogCommentCount()
        {
            var userId  = Guid.NewGuid();
            var blog    = CreateBlog();
            blog.CommentCount = 3;
            var comment = CreateComment(blog.Id, userId);

            SetupUser(userId);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            blog.CommentCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_WhenCommentOwnerDeletes_CommentCountDoesNotGoBelowZero()
        {
            var userId  = Guid.NewGuid();
            var blog    = CreateBlog();
            blog.CommentCount = 0;
            var comment = CreateComment(blog.Id, userId);

            SetupUser(userId);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            blog.CommentCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenCommentOwnerDeletes_SavesChanges()
        {
            var userId  = Guid.NewGuid();
            var blog    = CreateBlog();
            var comment = CreateComment(blog.Id, userId);

            SetupUser(userId);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Blog Owner Deletes Another User's Comment

        [Fact]
        public async Task Handle_WhenBlogOwnerDeletesOthersComment_ReturnsSuccess()
        {
            var blogOwner    = Guid.NewGuid();
            var commentOwner = Guid.NewGuid();
            var blog    = CreateBlog(blogOwner);
            var comment = CreateComment(blog.Id, commentOwner);

            SetupUser(blogOwner);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            var result = await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenBlogOwnerDeletesOthersComment_CallsRemove()
        {
            var blogOwner    = Guid.NewGuid();
            var commentOwner = Guid.NewGuid();
            var blog    = CreateBlog(blogOwner);
            var comment = CreateComment(blog.Id, commentOwner);

            SetupUser(blogOwner);
            SetupCommentFound(comment);
            SetupBlogForUpdate(blog);

            await _handler.Handle(
                new DeleteCommentCommand { CommentId = comment.Id }, CancellationToken.None);

            _commentRepositoryMock.Verify(r => r.Remove(comment), Times.Once);
        }

        #endregion
    }
}
