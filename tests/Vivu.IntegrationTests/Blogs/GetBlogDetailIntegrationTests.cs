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
    public class GetBlogDetailIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;

        public GetBlogDetailIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser = await SeedUserAsync("getblogdetail@test.com");
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
            DateTime? publishedAt = null,
            string? slug = null)
        {
            var resolvedSlug = slug ?? title.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..6];
            var blog = Blog.Create(
                userId: userId,
                tripId: null,
                title: title,
                slug: resolvedSlug,
                coverImageUrl: null,
                shortDescription: $"Description for {title}",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);

            blog.Status      = status;
            blog.PublishedAt = publishedAt ?? (status == "published" ? DateTime.UtcNow : null);
            blog.ViewCount   = viewCount;

            _dbContext.Set<Blog>().Add(blog);
            await _dbContext.SaveChangesAsync();
            return blog;
        }

        private static readonly JsonSerializerOptions JsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private async Task<GetBlogDetailApiResponse> GetBlogDetailAsync(string idOrSlug)
        {
            var response = await _client.GetAsync($"/api/Blogs/{idOrSlug}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<GetBlogDetailApiResponse>(JsonOpts))!;
        }

        #endregion

        #region Anonymous Access

        [Fact]
        public async Task GetBlogDetail_WithoutToken_ReturnsOk()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var blog = await SeedBlogAsync(_testUser.Id, "Anon Test Blog");

            // Act
            var response = await _client.GetAsync($"/api/Blogs/{blog.Slug}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path – By Slug

        [Fact]
        public async Task GetBlogDetail_BySlug_ReturnsSuccess()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "My Travel Blog");

            // Act
            var apiResponse = await GetBlogDetailAsync(blog.Slug);

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetBlogDetail_BySlug_ReturnsCorrectBlogFields()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Fields Check Blog", viewCount: 3);

            // Act
            var apiResponse = await GetBlogDetailAsync(blog.Slug);

            // Assert
            apiResponse.Data!.Id.Should().Be(blog.Id);
            apiResponse.Data.Title.Should().Be("Fields Check Blog");
            apiResponse.Data.Slug.Should().Be(blog.Slug);
            apiResponse.Data.ShortDescription.Should().NotBeNullOrEmpty();
            apiResponse.Data.Status.Should().Be("published");
        }

        #endregion

        #region Happy Path – By ID

        [Fact]
        public async Task GetBlogDetail_ByGuidId_ReturnsSuccess()
        {
            // Arrange
            var blog  = await SeedBlogAsync(_testUser.Id, "Blog By Id");
            var idStr = blog.Id.ToString();

            // Act
            var apiResponse = await GetBlogDetailAsync(idStr);

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Id.Should().Be(blog.Id);
        }

        [Fact]
        public async Task GetBlogDetail_ByGuidId_ReturnsSameBlogAsSlug()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Blog Same Result");

            var bySlugResponse = await GetBlogDetailAsync(blog.Slug);
            var byIdResponse   = await GetBlogDetailAsync(blog.Id.ToString());

            // Assert
            bySlugResponse.Data!.Id.Should().Be(byIdResponse.Data!.Id);
            bySlugResponse.Data.Title.Should().Be(byIdResponse.Data.Title);
        }

        #endregion

        #region View Count Increment

        [Fact]
        public async Task GetBlogDetail_WhenFetched_IncrementsViewCount()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "View Count Blog", viewCount: 5);

            // Act
            var apiResponse = await GetBlogDetailAsync(blog.Slug);

            // Assert
            apiResponse.Data!.ViewCount.Should().Be(6);
        }

        [Fact]
        public async Task GetBlogDetail_WhenFetchedTwice_IncrementsViewCountTwice()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Double View Blog", viewCount: 0);

            // Act – fetch twice
            await GetBlogDetailAsync(blog.Slug);
            var secondResponse = await GetBlogDetailAsync(blog.Slug);

            // Assert
            secondResponse.Data!.ViewCount.Should().Be(2);
        }

        #endregion

        #region Not Found

        [Fact]
        public async Task GetBlogDetail_WithNonExistentSlug_Returns404()
        {
            // Act
            var response = await _client.GetAsync("/api/Blogs/this-slug-does-not-exist");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetBlogDetail_WithNonExistentGuid_Returns404()
        {
            // Act
            var response = await _client.GetAsync($"/api/Blogs/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetBlogDetail_WithNonExistentId_ReturnsNotFoundErrorCode()
        {
            // Act
            var response = await _client.GetAsync($"/api/Blogs/{Guid.NewGuid()}");
            var body     = await response.Content.ReadFromJsonAsync<BlogDetailErrorResponse>(JsonOpts);

            // Assert
            body!.Success.Should().BeFalse();
            body.Code.Should().Contain("NotFound");
        }

        #endregion

        #region Not Published

        [Fact]
        public async Task GetBlogDetail_WithDraftBlog_Returns400()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Draft Blog", status: "draft");

            // Act
            var response = await _client.GetAsync($"/api/Blogs/{blog.Slug}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetBlogDetail_WithArchivedBlog_Returns400()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Archived Blog", status: "archived");

            // Act
            var response = await _client.GetAsync($"/api/Blogs/{blog.Slug}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetBlogDetail_WithDraftBlog_ReturnsNotPublishedErrorCode()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Not Published Blog", status: "draft");

            // Act
            var response = await _client.GetAsync($"/api/Blogs/{blog.Slug}");
            var body     = await response.Content.ReadFromJsonAsync<BlogDetailErrorResponse>(JsonOpts);

            // Assert
            body!.Success.Should().BeFalse();
            body.Code.Should().Be("Blog.NotPublished");
        }

        #endregion

        #region Validation

        [Fact]
        public async Task GetBlogDetail_WithValidSlugFormat_ReturnsOkOrNotFound()
        {
            // GET /api/Blogs/some-valid-slug always hits the handler;
            // if blog doesn't exist it returns 404, not a validation error.
            var response = await _client.GetAsync("/api/Blogs/some-valid-slug");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        #endregion
    }

    #region Response Models

    public class GetBlogDetailApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public BlogDetailResponseData? Data { get; set; }
    }

    public class BlogDetailResponseData
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("shortDescription")]
        public string? ShortDescription { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("viewCount")]
        public int ViewCount { get; set; }

        [JsonPropertyName("likeCount")]
        public int LikeCount { get; set; }

        [JsonPropertyName("commentCount")]
        public int CommentCount { get; set; }

        [JsonPropertyName("saveCount")]
        public int SaveCount { get; set; }

        [JsonPropertyName("authorName")]
        public string? AuthorName { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("blogStoryDays")]
        public List<object> BlogStoryDays { get; set; } = new();

        [JsonPropertyName("tags")]
        public List<object> Tags { get; set; } = new();
    }

    public class BlogDetailErrorResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    #endregion
}
