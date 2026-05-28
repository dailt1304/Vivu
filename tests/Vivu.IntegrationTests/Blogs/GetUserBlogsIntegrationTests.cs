using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Blogs
{
    [Collection("Integration Tests")]
    public class GetUserBlogsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _testUser = null!;
        private string _accessToken = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public GetUserBlogsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser    = await SeedUserAsync("getuserblogs@test.com");
            _accessToken = await GetAccessTokenAsync("getuserblogs@test.com", "Password123!");
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

        #region Seed & Login Helpers

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

        private async Task<string> GetAccessTokenAsync(string email, string password)
        {
            var loginRequest = new LoginUserCommand
            {
                Email      = email,
                Password   = password,
                IpAddress  = "127.0.0.1",
                DeviceType = "Test",
                DeviceName = "Test Device"
            };

            var response    = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            var apiResponse = await response.Content.ReadFromJsonAsync<UserBlogsLoginApiResponse>(JsonOpts);
            return apiResponse!.Data.AccessToken;
        }

        private async Task<Blog> SeedBlogAsync(
            Guid userId,
            string title,
            string status = "published",
            int viewCount = 0,
            int likeCount = 0,
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

            _dbContext.Set<Blog>().Add(blog);
            await _dbContext.SaveChangesAsync();
            return blog;
        }

        private void SetAuthHeader(string? token = null)
            => _client.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token ?? _accessToken);

        private void ClearAuthHeader()
            => _client.DefaultRequestHeaders.Authorization = null;

        private async Task<UserBlogsApiResponse> GetMyBlogsAsync(string queryString = "")
        {
            SetAuthHeader();
            var response = await _client.GetAsync($"/api/Blogs/me{queryString}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<UserBlogsApiResponse>(JsonOpts))!;
        }

        #endregion

        #region Auth Tests

        [Fact]
        public async Task GetMyBlogs_WithoutToken_ReturnsUnauthorized()
        {
            ClearAuthHeader();
            var response = await _client.GetAsync("/api/Blogs/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyBlogs_WithInvalidToken_ReturnsUnauthorized()
        {
            SetAuthHeader("this-is-not-a-valid-token");
            var response = await _client.GetAsync("/api/Blogs/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyBlogs_WithValidToken_ReturnsOk()
        {
            SetAuthHeader();
            var response = await _client.GetAsync("/api/Blogs/me");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Happy Path – No Blogs

        [Fact]
        public async Task GetMyBlogs_WhenUserHasNoBlogs_ReturnsEmptyList()
        {
            var apiResponse = await GetMyBlogsAsync();

            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().BeEmpty();
            apiResponse.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetMyBlogs_WhenUserHasNoBlogs_ReturnsDefaultPagination()
        {
            var apiResponse = await GetMyBlogsAsync();

            apiResponse.Data!.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }

        #endregion

        #region Happy Path – With Blogs

        [Fact]
        public async Task GetMyBlogs_WhenUserHasPublishedBlogs_ReturnsAll()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "My Blog One");
            await SeedBlogAsync(_testUser.Id, "My Blog Two");
            await SeedBlogAsync(_testUser.Id, "My Blog Three");

            // Act
            var apiResponse = await GetMyBlogsAsync();

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.TotalCount.Should().Be(3);
        }

        [Fact]
        public async Task GetMyBlogs_WhenUserHasDraftBlogs_ReturnsThem()
        {
            // Handler returns all blogs owned by the user regardless of status
            await SeedBlogAsync(_testUser.Id, "Published Blog");
            await SeedBlogAsync(_testUser.Id, "Draft Blog",   status: "draft");

            var apiResponse = await GetMyBlogsAsync();

            apiResponse.Data!.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyBlogs_BlogItems_HaveExpectedFields()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Fields Blog", viewCount: 7, likeCount: 3);

            // Act
            var apiResponse = await GetMyBlogsAsync();

            // Assert
            var item = apiResponse.Data!.Items.Should().ContainSingle().Subject;
            item.Id.Should().NotBeEmpty();
            item.Title.Should().Be("Fields Blog");
        }

        [Fact]
        public async Task GetMyBlogs_DoesNotReturnOtherUsersBlogs()
        {
            // Arrange – seed a second user with blogs
            var otherUser = await SeedUserAsync("other@test.com");
            await SeedBlogAsync(_testUser.Id, "My Blog");
            await SeedBlogAsync(otherUser.Id, "Other User Blog");

            // Act
            var apiResponse = await GetMyBlogsAsync();

            // Assert – only testUser's blog is returned
            apiResponse.Data!.Items.Should().ContainSingle();
            apiResponse.Data.Items[0].Title.Should().Be("My Blog");
        }

        #endregion

        #region Pagination Tests

        [Fact]
        public async Task GetMyBlogs_WithPageSize2_ReturnsCorrectChunk()
        {
            // Arrange
            for (var i = 1; i <= 5; i++)
                await SeedBlogAsync(_testUser.Id, $"Paginated Blog {i}");

            // Act
            var apiResponse = await GetMyBlogsAsync("?pageNumber=1&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(2);
            apiResponse.Data.TotalCount.Should().Be(5);
            apiResponse.Data.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetMyBlogs_SecondPage_ReturnsCorrectItems()
        {
            // Arrange
            for (var i = 1; i <= 5; i++)
                await SeedBlogAsync(_testUser.Id, $"Page Blog {i}");

            // Act
            var apiResponse = await GetMyBlogsAsync("?pageNumber=2&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(2);
            apiResponse.Data.PageNumber.Should().Be(2);
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
            apiResponse.Data.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyBlogs_LastPage_HasNoNextPage()
        {
            // Arrange
            for (var i = 1; i <= 5; i++)
                await SeedBlogAsync(_testUser.Id, $"Last Page Blog {i}");

            // Act
            var apiResponse = await GetMyBlogsAsync("?pageNumber=3&pageSize=2");

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(1);
            apiResponse.Data.HasNextPage.Should().BeFalse();
            apiResponse.Data.HasPreviousPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyBlogs_OutOfRangePage_ReturnsEmptyItems()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Only Blog");

            // Act
            var apiResponse = await GetMyBlogsAsync("?pageNumber=99&pageSize=10");

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().BeEmpty();
        }

        #endregion

        #region Ordering

        [Fact]
        public async Task GetMyBlogs_ReturnsBlogs_OrderedByPublishedAtDescending()
        {
            // Arrange
            await SeedBlogAsync(_testUser.Id, "Oldest Blog",  publishedAt: DateTime.UtcNow.AddDays(-3));
            await SeedBlogAsync(_testUser.Id, "Middle Blog",  publishedAt: DateTime.UtcNow.AddDays(-2));
            await SeedBlogAsync(_testUser.Id, "Newest Blog",  publishedAt: DateTime.UtcNow.AddDays(-1));

            // Act
            var apiResponse = await GetMyBlogsAsync();

            // Assert
            apiResponse.Data!.Items.Should().HaveCount(3);
            apiResponse.Data.Items.First().Title.Should().Be("Newest Blog");
            apiResponse.Data.Items.Last().Title.Should().Be("Oldest Blog");
        }

        #endregion
    }

    #region Response Models

    public class UserBlogsLoginData
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserBlogsLoginApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public UserBlogsLoginData Data { get; set; } = new();
    }

    public class UserBlogsApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public UserBlogsPaginatedResponse? Data { get; set; }
    }

    public class UserBlogsPaginatedResponse
    {
        [JsonPropertyName("items")]
        public List<UserBlogItem> Items { get; set; } = new();

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

    public class UserBlogItem
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

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }

    #endregion
}
