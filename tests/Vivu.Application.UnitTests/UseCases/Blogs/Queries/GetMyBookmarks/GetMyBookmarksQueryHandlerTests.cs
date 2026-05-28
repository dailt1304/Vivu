using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Blogs.Queries.GetMyBookmarks;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetMyBookmarks
{
    public class GetMyBookmarksQueryHandlerTests
    {
        private readonly Mock<IBlogSaveRepository> _blogSaveRepositoryMock;
        private readonly Mock<ICurrentUser>    _currentUserMock;
        private readonly Mock<IMapper>         _mapperMock;
        private readonly Mock<ILogger<GetMyBookmarksQueryHandler>> _loggerMock;
        private readonly GetMyBookmarksQueryHandler _handler;

        public GetMyBookmarksQueryHandlerTests()
        {
            _blogSaveRepositoryMock = new Mock<IBlogSaveRepository>();
            _currentUserMock    = new Mock<ICurrentUser>();
            _mapperMock         = new Mock<IMapper>();
            _loggerMock         = new Mock<ILogger<GetMyBookmarksQueryHandler>>();

            _handler = new GetMyBookmarksQueryHandler(
                _blogSaveRepositoryMock.Object,
                _currentUserMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private void SetupUser(string? id = null)
            => _currentUserMock.Setup(u => u.Id).Returns(id ?? Guid.NewGuid().ToString());

        private static Blog CreateBlog(DateTime? publishedAt = null)
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(), tripId: null,
                title: "Bookmarked Blog", slug: "b-" + Guid.NewGuid().ToString("N")[..6],
                coverImageUrl: null, shortDescription: "desc",
                travelDateStart: null, travelDateEnd: null,
                totalCost: null, groupSize: null);
            blog.Status      = "published";
            blog.PublishedAt = publishedAt ?? DateTime.UtcNow;
            return blog;
        }

        private void SetupBlogs(Guid userId, List<Blog> blogs)
            => _blogSaveRepositoryMock
                .Setup(r => r.GetBookmarkedBlogsByUserIdQuery(userId))
                .Returns(blogs.AsQueryable().ToAsyncQueryable());

        private void SetupMapper()
            => _mapperMock
                .Setup(m => m.Map<PublicBlogDto>(It.IsAny<Blog>()))
                .Returns((Blog b) => new PublicBlogDto { Id = b.Id, Title = b.Title });

        private static GetMyBookmarksQuery MakeQuery(int pageNumber = 1, int pageSize = 10)
            => new GetMyBookmarksQuery { PageNumber = pageNumber, PageSize = pageSize };

        #endregion

        #region Auth Guard

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-guid")]
        public async Task Handle_InvalidUserId_ReturnsAuthError(string? userId)
        {
            _currentUserMock.Setup(u => u.Id).Returns(userId!);

            var result = await _handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Auth.InvalidToken.Code);
            _blogSaveRepositoryMock.Verify(r => r.GetBookmarkedBlogsByUserIdQuery(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region No Bookmarks

        [Fact]
        public async Task Handle_NoBookmarks_ReturnsSuccessWithEmptyList()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupBlogs(userId, new List<Blog>());

            var result = await _handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_NoBookmarks_DoesNotCallMapper()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupBlogs(userId, new List<Blog>());

            await _handler.Handle(MakeQuery(), CancellationToken.None);

            _mapperMock.Verify(m => m.Map<PublicBlogDto>(It.IsAny<Blog>()), Times.Never);
        }

        #endregion

        #region With Bookmarks

        [Fact]
        public async Task Handle_WithBookmarks_ReturnsCorrectCount()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupBlogs(userId, new List<Blog>
            {
                CreateBlog(), CreateBlog(), CreateBlog()
            });
            SetupMapper();

            var result = await _handler.Handle(MakeQuery(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(3);
            result.Value.Items.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_WithBookmarks_CallsMapperForEachBlog()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blogs = Enumerable.Range(1, 4).Select(_ => CreateBlog()).ToList();
            SetupBlogs(userId, blogs);
            SetupMapper();

            await _handler.Handle(MakeQuery(), CancellationToken.None);

            _mapperMock.Verify(m => m.Map<PublicBlogDto>(It.IsAny<Blog>()), Times.Exactly(4));
        }

        [Fact]
        public async Task Handle_WithBookmarks_ReturnsCorrectDtoFields()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blog = CreateBlog();
            SetupBlogs(userId, new List<Blog> { blog });
            SetupMapper();

            var result = await _handler.Handle(MakeQuery(), CancellationToken.None);

            var dto = result.Value!.Items.Should().ContainSingle().Subject;
            dto.Id.Should().Be(blog.Id);
            dto.Title.Should().Be(blog.Title);
        }

        [Fact]
        public async Task Handle_QueriesWithCorrectUserId()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupBlogs(userId, new List<Blog>());

            await _handler.Handle(MakeQuery(), CancellationToken.None);

            _blogSaveRepositoryMock.Verify(r => r.GetBookmarkedBlogsByUserIdQuery(userId), Times.Once);
        }

        #endregion

        #region Pagination

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var blogs = Enumerable.Range(1, 12).Select(_ => CreateBlog()).ToList();
            SetupBlogs(userId, blogs);
            SetupMapper();

            var result = await _handler.Handle(MakeQuery(pageNumber: 2, pageSize: 5), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(12);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_LastPage_HasNoNextPage()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            SetupBlogs(userId, Enumerable.Range(1, 3).Select(_ => CreateBlog()).ToList());
            SetupMapper();

            var result = await _handler.Handle(MakeQuery(pageNumber: 2, pageSize: 2), CancellationToken.None);

            result.Value!.HasNextPage.Should().BeFalse();
            result.Value.HasPreviousPage.Should().BeTrue();
        }

        #endregion
    }
}
