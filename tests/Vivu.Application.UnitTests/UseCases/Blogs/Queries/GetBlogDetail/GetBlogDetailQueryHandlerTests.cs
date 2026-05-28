using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetBlogDetail
{
    public class GetBlogDetailQueryHandlerTests
    {
        private readonly Mock<IBlogRepository> _blogRepositoryMock;
        private readonly Mock<IBlogLikeRepository> _blogLikeRepositoryMock;
        private readonly Mock<IBlogSaveRepository> _blogSaveRepositoryMock;
        private readonly Mock<IUnitOfWork>     _unitOfWorkMock;
        private readonly Mock<ICurrentUser>    _currentUser;
        private readonly Mock<IMapper>         _mapperMock;
        private readonly Mock<ILogger<GetBlogDetailQueryHandler>> _loggerMock;
        private readonly GetBlogDetailQueryHandler _handler;

        public GetBlogDetailQueryHandlerTests()
        {
            _blogRepositoryMock     = new Mock<IBlogRepository>();
            _blogLikeRepositoryMock = new Mock<IBlogLikeRepository>();
            _blogSaveRepositoryMock = new Mock<IBlogSaveRepository>();
            _unitOfWorkMock         = new Mock<IUnitOfWork>();
            _currentUser            = new Mock<ICurrentUser>();
            _mapperMock             = new Mock<IMapper>();
            _loggerMock             = new Mock<ILogger<GetBlogDetailQueryHandler>>();

            _handler = new GetBlogDetailQueryHandler(
                _blogRepositoryMock.Object,
                _blogLikeRepositoryMock.Object,
                _blogSaveRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _currentUser.Object,
                _mapperMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog(string status = "published", int viewCount = 0)
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(),
                tripId: null,
                title: "Test Blog Title",
                slug: "test-blog-title",
                coverImageUrl: null,
                shortDescription: "Short description",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);

            blog.Status    = status;
            blog.ViewCount = viewCount;

            if (status == "published")
                blog.PublishedAt = DateTime.UtcNow;

            return blog;
        }

        private static BlogDetailDto MapToDto(Blog blog)
            => new BlogDetailDto
            {
                Id        = blog.Id,
                UserId    = blog.UserId,
                Title     = blog.Title,
                Slug      = blog.Slug,
                ViewCount = blog.ViewCount,
                Status    = blog.Status
            };

        private void SetupRepositoryReturns(Blog? blog, string idOrSlug = "test-blog-title")
        {
            _blogRepositoryMock
                .Setup(r => r.GetBlogDetailByIdOrSlugAsync(idOrSlug, It.IsAny<CancellationToken>()))
                .ReturnsAsync(blog);
        }

        private void SetupMapperReturns(Blog blog, BlogDetailDto dto)
        {
            _mapperMock
                .Setup(m => m.Map<BlogDetailDto>(blog))
                .Returns(dto);
        }

        #endregion

        #region NotFound Tests

        [Fact]
        public async Task Handle_WhenBlogNotFound_ReturnsFailureWithNotFoundError()
        {
            // Arrange
            SetupRepositoryReturns(null, "non-existent-slug");
            var query = new GetBlogDetailQuery { IdOrSlug = "non-existent-slug" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotFound.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogNotFound_DoesNotSaveChanges()
        {
            // Arrange
            SetupRepositoryReturns(null, "ghost");
            var query = new GetBlogDetailQuery { IdOrSlug = "ghost" };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region NotPublished Tests

        [Theory]
        [InlineData("draft")]
        [InlineData("archived")]
        [InlineData("pending")]
        public async Task Handle_WhenBlogIsNotPublished_ReturnsFailureWithNotPublishedError(string status)
        {
            // Arrange
            var blog = CreateBlog(status: status);
            SetupRepositoryReturns(blog, blog.Slug);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotPublished.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogIsNotPublished_DoesNotIncrementViewCount()
        {
            // Arrange
            var blog = CreateBlog(status: "draft", viewCount: 5);
            SetupRepositoryReturns(blog, blog.Slug);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            blog.ViewCount.Should().Be(5);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task Handle_WithPublishedBlog_ReturnsSuccess()
        {
            // Arrange
            var blog = CreateBlog(status: "published");
            var dto  = MapToDto(blog);
            SetupRepositoryReturns(blog, blog.Slug);
            SetupMapperReturns(blog, dto);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithPublishedBlog_ReturnsMappedDto()
        {
            // Arrange
            var blog = CreateBlog(status: "published");
            var dto  = MapToDto(blog);
            SetupRepositoryReturns(blog, blog.Slug);
            SetupMapperReturns(blog, dto);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Value!.Id.Should().Be(blog.Id);
            result.Value.Title.Should().Be(blog.Title);
            result.Value.Slug.Should().Be(blog.Slug);
        }

        [Fact]
        public async Task Handle_WithPublishedBlog_IncrementsViewCount()
        {
            // Arrange
            var blog         = CreateBlog(status: "published", viewCount: 10);
            var dto          = MapToDto(blog);
            SetupRepositoryReturns(blog, blog.Slug);
            SetupMapperReturns(blog, dto);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            blog.ViewCount.Should().Be(11);
        }

        [Fact]
        public async Task Handle_WithPublishedBlog_CallsRepositoryUpdate()
        {
            // Arrange
            var blog = CreateBlog(status: "published");
            var dto  = MapToDto(blog);
            SetupRepositoryReturns(blog, blog.Slug);
            SetupMapperReturns(blog, dto);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _blogRepositoryMock.Verify(r => r.Update(blog), Times.Once);
        }

        [Fact]
        public async Task Handle_WithPublishedBlog_CallsSaveChanges()
        {
            // Arrange
            var blog = CreateBlog(status: "published");
            var dto  = MapToDto(blog);
            SetupRepositoryReturns(blog, blog.Slug);
            SetupMapperReturns(blog, dto);
            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithPublishedBlogByGuidId_ReturnsSuccess()
        {
            // Arrange
            var blog  = CreateBlog(status: "published");
            var idStr = blog.Id.ToString();
            var dto   = MapToDto(blog);

            _blogRepositoryMock
                .Setup(r => r.GetBlogDetailByIdOrSlugAsync(idStr, It.IsAny<CancellationToken>()))
                .ReturnsAsync(blog);
            SetupMapperReturns(blog, dto);

            var query = new GetBlogDetailQuery { IdOrSlug = idStr };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value!.Id.Should().Be(blog.Id);
        }

        #endregion

        #region CancellationToken Tests

        [Fact]
        public async Task Handle_PassesCancellationTokenToRepository()
        {
            // Arrange
            var blog = CreateBlog(status: "published");
            var dto  = MapToDto(blog);
            var cts  = new CancellationTokenSource();
            var ct   = cts.Token;

            _blogRepositoryMock
                .Setup(r => r.GetBlogDetailByIdOrSlugAsync(blog.Slug, ct))
                .ReturnsAsync(blog);
            SetupMapperReturns(blog, dto);

            var query = new GetBlogDetailQuery { IdOrSlug = blog.Slug };

            // Act
            await _handler.Handle(query, ct);

            // Assert
            _blogRepositoryMock.Verify(
                r => r.GetBlogDetailByIdOrSlugAsync(blog.Slug, ct), Times.Once);
        }

        #endregion
    }
}
