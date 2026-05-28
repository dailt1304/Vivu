using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Commands.BookmarkBlog
{
    public class BookmarkBlogCommandHandlerTests
    {
        private readonly Mock<IBlogRepository>  _blogRepositoryMock;
        private readonly Mock<IBlogSaveRepository> _blogSaveRepositoryMock;
        private readonly Mock<IUnitOfWork>      _unitOfWorkMock;
        private readonly Mock<ICurrentUser>     _currentUserMock;
        private readonly Mock<ILogger<BookmarkBlogCommandHandler>> _loggerMock;
        private readonly BookmarkBlogCommandHandler _handler;

        public BookmarkBlogCommandHandlerTests()
        {
            _blogRepositoryMock = new Mock<IBlogRepository>();
            _blogSaveRepositoryMock = new Mock<IBlogSaveRepository>();
            _blogSaveRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<BlogSave>()))
                .ReturnsAsync((BlogSave save) => save);
            _unitOfWorkMock     = new Mock<IUnitOfWork>();
            _currentUserMock    = new Mock<ICurrentUser>();
            _loggerMock         = new Mock<ILogger<BookmarkBlogCommandHandler>>();

            _handler = new BookmarkBlogCommandHandler(
                _blogRepositoryMock.Object,
                _blogSaveRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUserMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog(string status = "published", int saveCount = 0)
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(), tripId: null,
                title: "Test Blog", slug: "test-blog",
                coverImageUrl: null, shortDescription: "desc",
                travelDateStart: null, travelDateEnd: null,
                totalCost: null, groupSize: null);
            blog.Status    = status;
            blog.SaveCount = saveCount;
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

        private void SetupExistingSave(Guid blogId, Guid userId, BlogSave? save)
            => _blogSaveRepositoryMock
                .Setup(r => r.GetBlogSaveAsync(blogId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(save);

        private static BookmarkBlogCommand MakeCommand(Guid? blogId = null)
            => new BookmarkBlogCommand { BlogId = blogId ?? Guid.NewGuid() };

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_InvalidUserId_ReturnsAuthError(string? userId)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId!);

            var result = await _handler.Handle(MakeCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
            _blogRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Blog Validation

        [Fact]
        public async Task Handle_BlogNotFound_ReturnsNotFoundError()
        {
            SetupUser();
            var blogId = Guid.NewGuid();
            SetupBlog(null, blogId);

            var result = await _handler.Handle(MakeCommand(blogId), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotFound.Code);
        }

        [Fact]
        public async Task Handle_BlogDeleted_ReturnsHasDeletedError()
        {
            SetupUser();
            var blog = CreateBlog(status: "deleted");
            SetupBlog(blog);

            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.HasDeleted.Code);
        }

        [Fact]
        public async Task Handle_BlogNotPublished_ReturnsNotPublishedError()
        {
            SetupUser();
            var blog = CreateBlog(status: "draft");
            SetupBlog(blog);

            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotPublished.Code);
        }

        #endregion

        #region Toggle ON (Bookmark)

        [Fact]
        public async Task Handle_NoExistingSave_BookmarksAndReturnsTrue()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog(saveCount: 0);
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, null);

            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.IsBookmarked.Should().BeTrue();
            result.Value.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_NoExistingSave_CallsAddAsync()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog();
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, null);

            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            _blogSaveRepositoryMock.Verify(r => r.AddAsync(It.Is<BlogSave>(s =>
                s.BlogId == blog.Id && s.UserId == userId)), Times.Once);
        }

        [Fact]
        public async Task Handle_NoExistingSave_IncrementsSaveCount()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog(saveCount: 5);
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, null);

            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            blog.SaveCount.Should().Be(6);
        }

        #endregion

        #region Toggle OFF (Un-bookmark)

        [Fact]
        public async Task Handle_ExistingSave_UnbookmarksAndReturnsFalse()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog(saveCount: 3);
            var save = BlogSave.Create(blog.Id, userId);
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, save);

            var result = await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.IsBookmarked.Should().BeFalse();
            result.Value.SaveCount.Should().Be(2);
        }

        [Fact]
        public async Task Handle_ExistingSave_CallsRemove()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog();
            var save = BlogSave.Create(blog.Id, userId);
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, save);

            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            _blogSaveRepositoryMock.Verify(r => r.Remove(save), Times.Once);
        }

        [Fact]
        public async Task Handle_ExistingSave_SaveCountNeverGoesNegative()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog(saveCount: 0);  // already 0
            var save = BlogSave.Create(blog.Id, userId);
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, save);

            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            blog.SaveCount.Should().Be(0);
        }

        #endregion

        #region Save Changes

        [Fact]
        public async Task Handle_OnSuccess_CallsSaveChanges()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog();
            SetupBlog(blog);
            SetupExistingSave(blog.Id, userId, null);

            await _handler.Handle(MakeCommand(blog.Id), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_OnFailure_DoesNotCallSaveChanges()
        {
            SetupUser();
            var blogId = Guid.NewGuid();
            SetupBlog(null, blogId);

            await _handler.Handle(MakeCommand(blogId), CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}
