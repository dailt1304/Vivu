using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Vivu.Application.UnitTests.Helpers;
using Vivu.Application.UseCases.Blogs.Queries.GetComments;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Blogs.Queries.GetComments
{
    public class GetCommentsQueryHandlerTests
    {
        private readonly Mock<IBlogRepository>        _blogRepositoryMock;
        private readonly Mock<IBlogCommentRepository> _commentRepositoryMock;
        private readonly Mock<ILogger<GetCommentsQueryHandler>> _loggerMock;
        private readonly GetCommentsQueryHandler      _handler;

        public GetCommentsQueryHandlerTests()
        {
            _blogRepositoryMock    = new Mock<IBlogRepository>();
            _commentRepositoryMock = new Mock<IBlogCommentRepository>();
            _loggerMock            = new Mock<ILogger<GetCommentsQueryHandler>>();

            _handler = new GetCommentsQueryHandler(
                _blogRepositoryMock.Object,
                _commentRepositoryMock.Object,
                _loggerMock.Object);
        }

        #region Helpers

        private static Blog CreateBlog()
        {
            var blog = Blog.Create(
                userId: Guid.NewGuid(), tripId: null,
                title: "Test Blog", slug: "test-blog",
                coverImageUrl: null, shortDescription: "desc",
                travelDateStart: null, travelDateEnd: null,
                totalCost: null, groupSize: null);
            blog.Status = "published";
            return blog;
        }

        private static BlogComment CreateComment(Guid blogId, string content = "Comment", bool isHidden = false,
            DateTime? createdAt = null)
        {
            var comment = BlogComment.Create(blogId, Guid.NewGuid(), content);
            comment.IsHidden = isHidden;
            if (createdAt.HasValue)
                typeof(BlogComment).GetProperty(nameof(BlogComment.CreatedDate))!
                    .SetValue(comment, createdAt.Value);
            return comment;
        }

        private void SetupBlog(Blog? blog, Guid? id = null)
        {
            var resolvedId = id ?? blog?.Id ?? Guid.NewGuid();
            _blogRepositoryMock
                .Setup(r => r.GetByIdAsync(resolvedId))
                .ReturnsAsync(blog);
        }

        private void SetupComments(Guid blogId, List<BlogComment> comments)
            => _commentRepositoryMock
                .Setup(r => r.GetCommentsByBlogIdQuery(blogId))
                .Returns(comments.AsQueryable().ToAsyncQueryable());

        private static GetCommentsQuery MakeQuery(Guid blogId, int pageNumber = 1, int pageSize = 10)
            => new GetCommentsQuery { BlogId = blogId, PageNumber = pageNumber, PageSize = pageSize };

        #endregion

        #region Blog Not Found

        [Fact]
        public async Task Handle_WhenBlogNotFound_ReturnsNotFoundError()
        {
            var blogId = Guid.NewGuid();
            SetupBlog(null, blogId);
            var result = await _handler.Handle(MakeQuery(blogId), CancellationToken.None);
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(DomainErrors.Blog.NotFound.Code);
        }

        [Fact]
        public async Task Handle_WhenBlogNotFound_DoesNotCallCommentRepository()
        {
            var blogId = Guid.NewGuid();
            SetupBlog(null, blogId);
            await _handler.Handle(MakeQuery(blogId), CancellationToken.None);
            _commentRepositoryMock.Verify(r => r.GetCommentsByBlogIdQuery(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region Happy Path – No Comments

        [Fact]
        public async Task Handle_WhenBlogHasNoComments_ReturnsSuccessWithEmptyList()
        {
            var blog = CreateBlog();
            SetupBlog(blog);
            SetupComments(blog.Id, new List<BlogComment>());

            var result = await _handler.Handle(MakeQuery(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().BeEmpty();
            result.Value.TotalCount.Should().Be(0);
        }

        #endregion

        #region Happy Path – With Comments

        [Fact]
        public async Task Handle_WithVisibleComments_ReturnsAllVisible()
        {
            var blog = CreateBlog();
            var comments = new List<BlogComment>
            {
                CreateComment(blog.Id, "First"),
                CreateComment(blog.Id, "Second"),
                CreateComment(blog.Id, "Third")
            };

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(3);
        }

        [Fact]
        public async Task Handle_WithHiddenComments_ExcludesHiddenOnes()
        {
            var blog = CreateBlog();
            var comments = new List<BlogComment>
            {
                CreateComment(blog.Id, "Visible 1"),
                CreateComment(blog.Id, "Hidden",   isHidden: true),
                CreateComment(blog.Id, "Visible 2")
            };

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().HaveCount(2);
            result.Value.Items.All(c => c.Content != "Hidden").Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WhenAllCommentsHidden_ReturnsEmpty()
        {
            var blog = CreateBlog();
            var comments = new List<BlogComment>
            {
                CreateComment(blog.Id, "H1", isHidden: true),
                CreateComment(blog.Id, "H2", isHidden: true)
            };

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ReturnsCorrectDtoFields()
        {
            var blog    = CreateBlog();
            var userId  = Guid.NewGuid();
            var comment = BlogComment.Create(blog.Id, userId, "Test content");

            SetupBlog(blog);
            SetupComments(blog.Id, new List<BlogComment> { comment });

            var result = await _handler.Handle(MakeQuery(blog.Id), CancellationToken.None);

            var dto = result.Value!.Items.Should().ContainSingle().Subject;
            dto.Id.Should().Be(comment.Id);
            dto.BlogId.Should().Be(comment.BlogId);
            dto.UserId.Should().Be(userId);
            dto.Content.Should().Be("Test content");
            dto.LikeCount.Should().Be(0);
        }

        #endregion

        #region Pagination

        [Fact]
        public async Task Handle_WithPagination_ReturnsCorrectPage()
        {
            var blog     = CreateBlog();
            var comments = Enumerable.Range(1, 15)
                .Select(i => CreateComment(blog.Id, $"Comment {i}"))
                .ToList();

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id, pageNumber: 2, pageSize: 5), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TotalCount.Should().Be(15);
            result.Value.PageNumber.Should().Be(2);
            result.Value.PageSize.Should().Be(5);
            result.Value.Items.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_FirstPage_HasNoPreviousPage()
        {
            var blog     = CreateBlog();
            var comments = Enumerable.Range(1, 5).Select(i => CreateComment(blog.Id, $"C{i}")).ToList();

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id, pageNumber: 1, pageSize: 3), CancellationToken.None);

            result.Value!.HasPreviousPage.Should().BeFalse();
            result.Value.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LastPage_HasNoNextPage()
        {
            var blog     = CreateBlog();
            var comments = Enumerable.Range(1, 5).Select(i => CreateComment(blog.Id, $"C{i}")).ToList();

            SetupBlog(blog);
            SetupComments(blog.Id, comments);

            var result = await _handler.Handle(MakeQuery(blog.Id, pageNumber: 3, pageSize: 2), CancellationToken.None);

            result.Value!.HasNextPage.Should().BeFalse();
            result.Value.HasPreviousPage.Should().BeTrue();
        }

        #endregion
    }
}
