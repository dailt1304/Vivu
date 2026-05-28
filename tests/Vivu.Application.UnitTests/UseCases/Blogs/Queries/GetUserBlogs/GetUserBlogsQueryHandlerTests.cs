using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetUserBlogs
{
    public class GetUserBlogsQueryHandlerTests
    {
        private readonly Mock<IBlogRepository>  _blogRepositoryMock;
        private readonly Mock<IUserRepository>  _userRepositoryMock;
        private readonly Mock<IMapper>          _mapperMock;
        private readonly Mock<ILogger<GetUserBlogsQueryHandler>> _loggerMock;
        private readonly GetUserBlogsQueryHandler _handler;

        public GetUserBlogsQueryHandlerTests()
        {
            _blogRepositoryMock = new Mock<IBlogRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _mapperMock         = new Mock<IMapper>();
            _loggerMock         = new Mock<ILogger<GetUserBlogsQueryHandler>>();

            _handler = new GetUserBlogsQueryHandler(
                _blogRepositoryMock.Object,
                _userRepositoryMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static User CreateUser(Guid? id = null)
        {
            var user = User.Create(
                email: "test@example.com",
                passwordHash: "hashedpassword",
                fullName: "Test User");
            if (id.HasValue)
                typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id.Value);
            return user;
        }

        private static Blog CreatePublishedBlog(Guid userId, string title = "Test Blog")
        {
            var blog = Blog.Create(
                userId: userId,
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
            blog.PublishedAt = DateTime.UtcNow;
            return blog;
        }

        private static PublicBlogDto CreateBlogDto(Blog blog)
            => new PublicBlogDto
            {
                Id          = blog.Id,
                UserId      = blog.UserId,
                Title       = blog.Title,
                Slug        = blog.Slug,
                PublishedAt = blog.PublishedAt
            };

        private void SetupUserFound(Guid userId)
        {
            var user = CreateUser(userId);
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
        }

        private void SetupUserNotFound(Guid userId)
        {
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);
        }

        private void SetupBlogRepository(Guid userId, List<Blog> blogs)
        {
            var asyncQueryable = blogs.AsQueryable().ToAsyncQueryable();
            _blogRepositoryMock
                .Setup(r => r.GetBlogsByUserIdQuery(userId))
                .Returns(asyncQueryable);
        }

        private void SetupMapper(List<Blog> blogs)
        {
            foreach (var blog in blogs)
            {
                var dto = CreateBlogDto(blog);
                _mapperMock.Setup(m => m.Map<PublicBlogDto>(blog)).Returns(dto);
            }
        }

        private static GetUserBlogsQuery CreateQuery(Guid userId, int pageNumber = 1, int pageSize = 10)
            => new GetUserBlogsQuery { UserId = userId, PageNumber = pageNumber, PageSize = pageSize };

        #endregion

        #region User Not Found

        [Fact]
        public async Task Handle_WhenUserNotFound_ReturnsFailureWithUserNotFoundError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserNotFound(userId);
            var query = CreateQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.User.NotFoundById(userId).Code);
        }

        [Fact]
        public async Task Handle_WhenUserNotFound_DoesNotCallBlogRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserNotFound(userId);
            var query = CreateQuery(userId);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _blogRepositoryMock.Verify(r => r.GetBlogsByUserIdQuery(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Empty Blog List

        [Fact]
        public async Task Handle_WhenUserHasNoBlogs_ReturnsSuccessWithEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserFound(userId);
            SetupBlogRepository(userId, new List<Blog>());
            var query = CreateQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenUserHasNoBlogs_ReturnsCorrectPaginationMetadata()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserFound(userId);
            SetupBlogRepository(userId, new List<Blog>());
            var query = CreateQuery(userId, pageNumber: 2, pageSize: 5);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task Handle_WhenUserHasBlogs_ReturnsSuccessWithBlogs()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blogs  = new List<Blog>
            {
                CreatePublishedBlog(userId, "Blog One"),
                CreatePublishedBlog(userId, "Blog Two"),
                CreatePublishedBlog(userId, "Blog Three")
            };

            SetupUserFound(userId);
            SetupBlogRepository(userId, blogs);
            SetupMapper(blogs);
            var query = CreateQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
            result.Value.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task Handle_WhenUserHasBlogs_ReturnsMappedDtos()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreatePublishedBlog(userId, "Mapped Blog");

            SetupUserFound(userId);
            SetupBlogRepository(userId, new List<Blog> { blog });
            SetupMapper(new List<Blog> { blog });
            var query = CreateQuery(userId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.Items.Should().ContainSingle();
            result.Value.Items[0].Id.Should().Be(blog.Id);
            result.Value.Items[0].Title.Should().Be(blog.Title);
        }

        [Fact]
        public async Task Handle_WhenUserHasBlogs_CallsMapperForEachBlog()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blogs  = Enumerable.Range(1, 3)
                .Select(i => CreatePublishedBlog(userId, $"Blog {i}"))
                .ToList();

            SetupUserFound(userId);
            SetupBlogRepository(userId, blogs);
            SetupMapper(blogs);
            var query = CreateQuery(userId);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            foreach (var blog in blogs)
                _mapperMock.Verify(m => m.Map<PublicBlogDto>(blog), Times.Once);
        }

        #endregion

        #region Pagination

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blogs  = Enumerable.Range(1, 15)
                .Select(i => CreatePublishedBlog(userId, $"Blog {i}"))
                .ToList();

            SetupUserFound(userId);
            SetupBlogRepository(userId, blogs);
            SetupMapper(blogs);
            var query = CreateQuery(userId, pageNumber: 2, pageSize: 5);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(15);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_FirstPage_HasNoHasPreviousPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blogs  = Enumerable.Range(1, 5)
                .Select(i => CreatePublishedBlog(userId, $"Blog {i}"))
                .ToList();

            SetupUserFound(userId);
            SetupBlogRepository(userId, blogs);
            SetupMapper(blogs);
            var query = CreateQuery(userId, pageNumber: 1, pageSize: 3);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.HasPreviousPage.Should().BeFalse();
            result.Value.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LastPage_HasNoNextPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blogs  = Enumerable.Range(1, 5)
                .Select(i => CreatePublishedBlog(userId, $"Blog {i}"))
                .ToList();

            SetupUserFound(userId);
            SetupBlogRepository(userId, blogs);
            SetupMapper(blogs);
            var query = CreateQuery(userId, pageNumber: 3, pageSize: 2);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.HasNextPage.Should().BeFalse();
            result.Value.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithSingleBlogAndPageSize10_HasNeitherPreviousNorNextPage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var blog   = CreatePublishedBlog(userId);

            SetupUserFound(userId);
            SetupBlogRepository(userId, new List<Blog> { blog });
            SetupMapper(new List<Blog> { blog });
            var query = CreateQuery(userId, pageNumber: 1, pageSize: 10);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.HasPreviousPage.Should().BeFalse();
            result.Value.HasNextPage.Should().BeFalse();
        }

        #endregion

        #region UserId Passed Correctly

        [Fact]
        public async Task Handle_PassesCorrectUserIdToRepositories()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUserFound(userId);
            SetupBlogRepository(userId, new List<Blog>());
            var query = CreateQuery(userId);

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _blogRepositoryMock.Verify(r => r.GetBlogsByUserIdQuery(userId), Times.Once);
        }

        #endregion
    }
}
