using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogs;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetBlogs
{
    public class GetBlogsQueryHandlerTests
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock;
        private readonly Mock<IBlogLikeRepository> _blogLikeRepositoryMock;
        private readonly Mock<IBlogSaveRepository> _blogSaveRepositoryMock;
        private readonly Mock<ICurrentUser> _currentUserMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetBlogsQueryHandler>> _loggerMock;
        private readonly GetBlogsQueryHandler _handler;

        public GetBlogsQueryHandlerTests()
        {
            _blogRepositoryMock     = new Mock<IBlogRepository>();
            _blogLikeRepositoryMock = new Mock<IBlogLikeRepository>();
            _blogSaveRepositoryMock = new Mock<IBlogSaveRepository>();
            _currentUserMock        = new Mock<ICurrentUser>();
            _mapperMock             = new Mock<IMapper>();
            _loggerMock             = new Mock<ILogger<GetBlogsQueryHandler>>();

            _handler = new GetBlogsQueryHandler(
                _blogRepositoryMock.Object,
                _blogLikeRepositoryMock.Object,
                _blogSaveRepositoryMock.Object,
                _currentUserMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static GetBlogsQuery CreateQuery(
            string sortBy = "newest",
            int pageNumber = 1,
            int pageSize = 10)
            => new GetBlogsQuery { SortBy = sortBy, PageNumber = pageNumber, PageSize = pageSize };

        private static Blog CreatePublishedBlog(
            string title = "Test Blog",
            int viewCount = 0,
            int likeCount = 0,
            int saveCount = 0,
            DateTime? publishedAt = null)
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(),
                tripId: null,
                title: title,
                slug: title.ToLower().Replace(" ", "-"),
                coverImageUrl: null,
                shortDescription: "Short description",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);

            blog.Status      = "published";
            blog.PublishedAt = publishedAt ?? DateTime.UtcNow;
            blog.ViewCount   = viewCount;
            blog.LikeCount   = likeCount;
            blog.SaveCount   = saveCount;
            return blog;
        }

        private static PublicBlogDto CreateBlogDto(Blog blog)
            => new PublicBlogDto
            {
                Id          = blog.Id,
                UserId      = blog.UserId,
                Title       = blog.Title,
                Slug        = blog.Slug,
                ViewCount   = blog.ViewCount,
                LikeCount   = blog.LikeCount,
                SaveCount   = blog.SaveCount,
                PublishedAt = blog.PublishedAt
            };

        private void SetupRepository(List<Blog> blogs)
        {
            var asyncQueryable = blogs.AsQueryable().ToAsyncQueryable();
            _blogRepositoryMock
                .Setup(x => x.GetPublicBlogsQuery(false, null))
                .Returns(asyncQueryable);
        }

        private void SetupMapper(List<Blog> blogs)
        {
            foreach (var blog in blogs)
            {
                var dto = CreateBlogDto(blog);
                _mapperMock
                    .Setup(x => x.Map<PublicBlogDto>(blog))
                    .Returns(dto);
            }
        }

        #endregion

        #region Happy Path – Empty / No Blogs

        [Fact]
        public async Task Handle_WithNoBlogs_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            SetupRepository(new List<Blog>());
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WithNoBlogs_DoesNotCallMapper()
        {
            // Arrange
            SetupRepository(new List<Blog>());
            var query = CreateQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(x => x.Map<PublicBlogDto>(It.IsAny<object>()), Times.Never);
        }

        #endregion

        #region Happy Path – With Blogs

        [Fact]
        public async Task Handle_WithPublishedBlogs_ReturnsSuccessWithItems()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Blog A"),
                CreatePublishedBlog("Blog B"),
                CreatePublishedBlog("Blog C")
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task Handle_WithBlogs_MapsEachBlogToDto()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Blog A"),
                CreatePublishedBlog("Blog B")
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            _mapperMock.Verify(x => x.Map<PublicBlogDto>(It.IsAny<Blog>()), Times.Exactly(2));
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var blogs = Enumerable.Range(1, 5)
                .Select(i => CreatePublishedBlog($"Blog {i}"))
                .ToList();
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(2);
            result.Value.TotalCount.Should().Be(5);
            result.Value.HasPreviousPage.Should().BeTrue();
            result.Value.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FirstPage_HasNoPreviousPage()
        {
            // Arrange
            var blogs = new List<Blog> { CreatePublishedBlog() };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(pageNumber: 1, pageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.HasPreviousPage.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_LastPage_HasNoNextPage()
        {
            // Arrange
            var blogs = Enumerable.Range(1, 3)
                .Select(i => CreatePublishedBlog($"Blog {i}"))
                .ToList();
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(pageNumber: 2, pageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.Items.Should().HaveCount(1);
            result.Value.HasNextPage.Should().BeFalse();
        }

        #endregion

        #region SortBy Tests

        [Theory]
        [InlineData("newest")]
        [InlineData("NEWEST")]
        [InlineData(null)]
        [InlineData("")]
        public async Task Handle_WithNewestOrDefaultSort_ReturnsSuccess(string? sortBy)
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Older", publishedAt: DateTime.UtcNow.AddDays(-2)),
                CreatePublishedBlog("Newer", publishedAt: DateTime.UtcNow.AddDays(-1))
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(sortBy: sortBy ?? "newest");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithMostViewedSort_ReturnsSuccess()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Low Views",  viewCount: 10),
                CreatePublishedBlog("High Views", viewCount: 100)
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(sortBy: "most_viewed");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithMostLikedSort_ReturnsSuccess()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Low Likes",  likeCount: 5),
                CreatePublishedBlog("High Likes", likeCount: 50)
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(sortBy: "most_liked");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithPopularSort_ReturnsSuccess()
        {
            // Arrange
            var blogs = new List<Blog>
            {
                CreatePublishedBlog("Less Popular",  viewCount: 1,  likeCount: 1,  saveCount: 1),
                CreatePublishedBlog("More Popular",  viewCount: 50, likeCount: 50, saveCount: 50)
            };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(sortBy: "popular");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_WithUnknownSortBy_FallsBackToNewest()
        {
            // Arrange
            var blogs = new List<Blog> { CreatePublishedBlog() };
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(sortBy: "unknown_sort");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region Result Shape Tests

        [Fact]
        public async Task Handle_Result_HasCorrectPaginationMetadata()
        {
            // Arrange
            var blogs = Enumerable.Range(1, 6)
                .Select(i => CreatePublishedBlog($"Blog {i}"))
                .ToList();
            SetupRepository(blogs);
            SetupMapper(blogs);
            var query = CreateQuery(pageNumber: 1, pageSize: 4);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.TotalCount.Should().Be(6);
            result.Value.TotalPages.Should().Be(2);
            result.Value.PageNumber.Should().Be(1);
            result.Value.PageSize.Should().Be(4);
            result.Value.Items.Should().HaveCount(4);
            result.Value.HasNextPage.Should().BeTrue();
            result.Value.HasPreviousPage.Should().BeFalse();
        }

        #endregion
    }
}
