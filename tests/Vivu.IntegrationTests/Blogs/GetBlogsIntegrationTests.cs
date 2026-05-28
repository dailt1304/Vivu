using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Blogs
{
    [Collection("Integration Tests")]
    public class GetBlogsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;

        public GetBlogsIntegrationTests(IntegrationTestWebAppFactory factory)
        {
            _factory = factory;
            _client  = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            _scope         = _factory.Services.CreateScope();
            _dbContext      = _scope.ServiceProvider.GetRequiredService<VivuDbContext>();
            _passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        }

        public async Task InitializeAsync()
        {
            await CleanupDatabaseAsync();
            _testUser = await SeedUserAsync("getblogs@test.com");
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        private async Task CleanupDatabaseAsync()
        {
            await _dbContext.Set<BlogCommentLike>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogComment>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogReport>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogSave>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogView>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogPostTag>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogImage>().ExecuteDeleteAsync();
            await _dbContext.Set<BlogStoryDay>().ExecuteDeleteAsync();
            await _dbContext.Set<Blog>().ExecuteDeleteAsync();
            await _dbContext.Set<RefreshToken>().ExecuteDeleteAsync();
            await _dbContext.Set<UserRole>().ExecuteDeleteAsync();
            await _dbContext.Set<UserProfile>().ExecuteDeleteAsync();
            await _dbContext.Set<User>().ExecuteDeleteAsync();
            await _dbContext.Set<Role>().ExecuteDeleteAsync();
        }

        #region Seed Helpers

        private async Task<User> SeedUserAsync(string email)
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword("Password123!"),
                fullName: "Test Author");
            user.IsEmailVerified = true;

            var role = new Role
            {
                Id              = Guid.NewGuid(),
                RoleName        = "USER",
                RoleDescription = "Standard User"
            };
            user.UserRoles = new List<UserRole>
            {
                new() { UserId = user.Id, RoleId = role.Id, Role = role }
            };

            _dbContext.Set<Role>().Add(role);
            _dbContext.Set<User>().Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        private async Task<Blog> SeedBlogAsync(
            Guid userId,
            string title,
            string status = "published",
            int viewCount = 0,
            int likeCount = 0,
            int saveCount = 0,
            DateTime? publishedAt = null)
        {
            var blog = Blog.Create(
                userId: userId,
                tripId: null,
                title: title,
                slug: title.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..6],
                coverImageUrl: null,
                shortDescription: $"Description for {title}",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);

            blog.Status      = status;
            blog.PublishedAt = publishedAt ?? (status == "published" ? DateTime.UtcNow : null);
            blog.ViewCount   = viewCount;
            blog.LikeCount   = likeCount;
            blog.SaveCount   = saveCount;

            _dbContext.Set<Blog>().Add(blog);
            await _dbContext.SaveChangesAsync();
            return blog;
        }

        private async Task<GetBlogsApiResponse> GetBlogsAsync(string queryString = "")
        {
            var response = await _client.GetAsync($"/api/Blogs{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<GetBlogsApiResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))!;
        }

        #endregion

        #region Anonymous Access Tests

        [Fact]
        public async Task GetBlogs_WithoutToken_ReturnsOk()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/Blogs");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path Tests

        [Fact]
        public async Task GetBlogs_WithNoBlogs_ReturnsEmptyList()
        {
            // Act
            var apiResponse = await GetBlogsAsync();

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetBlogs_WithPublishedBlogs_ReturnsThemAll()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Blog One");
            await SeedBlogAsync(_testUser.Id, "Blog Two");
            await SeedBlogAsync(_testUser.Id, "Blog Three");

            // Act
            var apiResponse = await GetBlogsAsync();

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetBlogs_WithDraftBlogs_ExcludesThem()
        {
            // Arrange – 2 published + 1 draft
            await SeedBlogAsync(_testUser.Id, "Published Blog 1");
            await SeedBlogAsync(_testUser.Id, "Published Blog 2");
            await SeedBlogAsync(_testUser.Id, "Draft Blog", status: "draft");

            // Act
            var apiResponse = await GetBlogsAsync();

            // Assert
            apiResponse.Data!.TotalCount.Should().Be(2);
            apiResponse.Data.Items.Should().AllSatisfy(b =>
                b.Title.Should().NotBe("Draft Blog"));
        }

        [Fact]
        public async Task GetBlogs_BlogItems_HaveExpectedFields()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Field Check Blog", viewCount: 5, likeCount: 3);

            // Act
            var apiResponse = await GetBlogsAsync();

            // Assert
            var item = apiResponse.Data!.Items.Should().ContainSingle().Subject;
            item.Id.Should().NotBeEmpty();
            item.Title.Should().Be("Field Check Blog");
            item.ViewCount.Should().Be(5);
            item.LikeCount.Should().Be(3);
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetBlogs_WithPaginationPageSize2_ReturnsCorrectChunk()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Blog A");
            await SeedBlogAsync(_testUser.Id, "Blog B");
            await SeedBlogAsync(_testUser.Id, "Blog C");
            await SeedBlogAsync(_testUser.Id, "Blog D");
            await SeedBlogAsync(_testUser.Id, "Blog E");

            // Act
            var apiResponse = await GetBlogsAsync("?pageNumber=1&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(2);
            apiResponse.Data.TotalCount.Should().Be(5);
            apiResponse.Data.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetBlogs_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            for (var i = 1; i <= 5; i++)
                await SeedBlogAsync(_testUser.Id, $"Page Blog {i}");

            // Act
            var apiResponse = await GetBlogsAsync("?pageNumber=2&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(2);
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
            apiResponse.Data.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetBlogs_LastPage_HasNoNextPage()
        {
            // Arrange
            for (var i = 1; i <= 5; i++)
                await SeedBlogAsync(_testUser.Id, $"Last Page Blog {i}");

            // Act
            var apiResponse = await GetBlogsAsync("?pageNumber=3&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.HasNextPage.Should().BeFalse();
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetBlogs_DefaultPagination_UsesPageSize10()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Some Blog");

            // Act
            var apiResponse = await GetBlogsAsync();

            // Assert
            apiResponse.Data!.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        #endregion

        #region SortBy Tests

        [Theory]
        [InlineData("newest")]
        [InlineData("popular")]
        [InlineData("most_viewed")]
        [InlineData("most_liked")]
        public async Task GetBlogs_WithValidSortBy_ReturnsOk(string sortBy)
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, $"Sort Test Blog {sortBy}");

            // Act
            var response = await _client.GetAsync($"/api/Blogs?sortBy={sortBy}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetBlogs_WithNewestSort_ReturnsBlogsOrderedByPublishedAtDesc()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Oldest Blog",  publishedAt: DateTime.UtcNow.AddDays(-3));
            await SeedBlogAsync(_testUser.Id, "Middle Blog",  publishedAt: DateTime.UtcNow.AddDays(-2));
            await SeedBlogAsync(_testUser.Id, "Newest Blog",  publishedAt: DateTime.UtcNow.AddDays(-1));

            // Act
            var apiResponse = await GetBlogsAsync("?sortBy=newest");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.Items.First().Title.Should().Be("Newest Blog");
            apiResponse.Data.Items.Last().Title.Should().Be("Oldest Blog");
        }

        [Fact]
        public async Task GetBlogs_WithMostViewedSort_ReturnsBlogsOrderedByViewCountDesc()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Low Views",    viewCount: 5);
            await SeedBlogAsync(_testUser.Id, "Medium Views", viewCount: 50);
            await SeedBlogAsync(_testUser.Id, "High Views",   viewCount: 200);

            // Act
            var apiResponse = await GetBlogsAsync("?sortBy=most_viewed");

            // Assert
            apiResponse.Data!.Items.First().ViewCount.Should().Be(200);
            apiResponse.Data.Items.Last().ViewCount.Should().Be(5);
        }

        [Fact]
        public async Task GetBlogs_WithMostLikedSort_ReturnsBlogsOrderedByLikeCountDesc()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Low Likes",  likeCount: 1);
            await SeedBlogAsync(_testUser.Id, "High Likes", likeCount: 99);

            // Act
            var apiResponse = await GetBlogsAsync("?sortBy=most_liked");

            // Assert
            apiResponse.Data!.Items.First().LikeCount.Should().Be(99);
            apiResponse.Data.Items.Last().LikeCount.Should().Be(1);
        }

        [Fact]
        public async Task GetBlogs_WithPopularSort_ReturnsBlogsOrderedByPopularityDesc()
        {
            // Arrange – popular = likeCount + viewCount + saveCount
            await SeedBlogAsync(_testUser.Id, "Less Popular",
                viewCount: 1, likeCount: 1,  saveCount: 1);  // score = 3
            await SeedBlogAsync(_testUser.Id, "Most Popular",
                viewCount: 10, likeCount: 10, saveCount: 10); // score = 30

            // Act
            var apiResponse = await GetBlogsAsync("?sortBy=popular");

            // Assert
            apiResponse.Data!.Items.First().Title.Should().Be("Most Popular");
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetBlogs_WithInvalidSortBy_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Blogs?sortBy=invalid_sort");

            // Assert – ValidationBehavior throws → GlobalExceptionHandler returns 422
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task GetBlogs_WithPageSizeOver50_ReturnsUnprocessableEntity()
        {
            // Act
            var response = await _client.GetAsync("/api/Blogs?pageSize=51");

            // Assert – 51 passes the setter (MaxPageSize=100) but fails validator (max=50) → 422
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task GetBlogs_WithPageSizeZero_ReturnsOk()
        {
            // pageSize=0 is silently clamped to DefaultPageSize=10 by PaginationRequest setter,
            // so the validator never sees an invalid value → request succeeds.
            var response = await _client.GetAsync("/api/Blogs?pageSize=0");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion
    }

    #region Response Models

    public class GetBlogsApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public BlogsPaginatedResponse? Data { get; set; }
    }

    public class BlogsPaginatedResponse
    {
        [JsonPropertyName("items")]
        public List<BlogResponseItem> Items { get; set; } = new();

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    public class BlogResponseItem
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("shortDescription")]
        public string? ShortDescription { get; set; }

        [JsonPropertyName("viewCount")]
        public int ViewCount { get; set; }

        [JsonPropertyName("likeCount")]
        public int LikeCount { get; set; }

        [JsonPropertyName("saveCount")]
        public int SaveCount { get; set; }

        [JsonPropertyName("authorName")]
        public string? AuthorName { get; set; }
    }

    #endregion
}
