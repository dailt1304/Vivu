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
    public class LikeBlogIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
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

        public LikeBlogIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _testUser    = await SeedUserAsync("likeblog@test.com");
            _accessToken = await GetAccessTokenAsync("likeblog@test.com", "Password123!");
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
            await _dbContext.Set<BlogLike>().ExecuteDeleteAsync();
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
            var apiResponse = await response.Content.ReadFromJsonAsync<LikeBlogLoginApiResponse>(JsonOpts);
            return apiResponse!.Data.AccessToken;
        }

        private async Task<Blog> SeedBlogAsync(
            Guid userId,
            string title,
            string status = "published",
            int likeCount = 0)
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

            blog.Status    = status;
            blog.LikeCount = likeCount;

            if (status == "published")
                blog.PublishedAt = DateTime.UtcNow;

            _dbContext.Set<Blog>().Add(blog);
            await _dbContext.SaveChangesAsync();
            return blog;
        }

        private void SetAuthHeader(string? token = null)
            => _client.DefaultRequestHeaders.Authorization =
               new AuthenticationHeaderValue("Bearer", token ?? _accessToken);

        private void ClearAuthHeader()
            => _client.DefaultRequestHeaders.Authorization = null;

        private async Task<LikeBlogApiResponse> LikeBlogAsync(Guid blogId, string? token = null)
        {
            SetAuthHeader(token);
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{blogId}/like", new { });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<LikeBlogApiResponse>(JsonOpts))!;
        }

        #endregion

        #region Auth Tests

        [Fact]
        public async Task LikeBlog_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            ClearAuthHeader();
            var blog = await SeedBlogAsync(_testUser.Id, "Auth Test Blog");

            // Act
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/like", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task LikeBlog_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Invalid Token Blog");
            SetAuthHeader("not-a-valid-jwt");

            // Act
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/like", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Blog Not Found

        [Fact]
        public async Task LikeBlog_WithNonExistentBlogId_Returns404()
        {
            // Arrange
            SetAuthHeader();

            // Act
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{Guid.NewGuid()}/like", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region Blog Not Published / Deleted

        [Fact]
        public async Task LikeBlog_WhenBlogIsDraft_ReturnsBadRequest()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Draft Blog", status: "draft");
            SetAuthHeader();

            // Act
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/like", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task LikeBlog_WhenBlogIsDeleted_ReturnsBadRequest()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Deleted Blog", status: "deleted");
            SetAuthHeader();

            // Act
            var response = await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/like", new { });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Toggle Like ON

        [Fact]
        public async Task LikeBlog_WhenNotPreviouslyLiked_ReturnsIsLikedTrue()
        {
            // Arrange
            var blog     = await SeedBlogAsync(_testUser.Id, "Like ON Blog", likeCount: 0);

            // Act
            var apiResponse = await LikeBlogAsync(blog.Id);

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.IsLiked.Should().BeTrue();
        }

        [Fact]
        public async Task LikeBlog_WhenNotPreviouslyLiked_IncreasesLikeCountByOne()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "LikeCount Blog", likeCount: 5);

            // Act
            var apiResponse = await LikeBlogAsync(blog.Id);

            // Assert
            apiResponse.Data!.LikeCount.Should().Be(6);
        }

        [Fact]
        public async Task LikeBlog_WhenNotPreviouslyLiked_PersistsLikeInDatabase()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Persist Like Blog");

            // Act
            await LikeBlogAsync(blog.Id);

            // Assert
            var likeExists = await _dbContext.Set<BlogLike>()
                .AnyAsync(l => l.BlogId == blog.Id && l.UserId == _testUser.Id);
            likeExists.Should().BeTrue();
        }

        #endregion

        #region Toggle Like OFF

        [Fact]
        public async Task LikeBlog_WhenAlreadyLiked_ReturnsIsLikedFalse()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Toggle OFF Blog", likeCount: 3);

            // First like
            await LikeBlogAsync(blog.Id);

            // Act – second like (toggles off)
            var apiResponse = await LikeBlogAsync(blog.Id);

            // Assert
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.IsLiked.Should().BeFalse();
        }

        [Fact]
        public async Task LikeBlog_WhenAlreadyLiked_DecreasesLikeCount()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Decrease Count Blog", likeCount: 5);

            await LikeBlogAsync(blog.Id); // like → count becomes 6

            // Act – unlike → count should decrease
            var apiResponse = await LikeBlogAsync(blog.Id);

            // Assert
            apiResponse.Data!.LikeCount.Should().Be(5);
        }

        [Fact]
        public async Task LikeBlog_WhenAlreadyLiked_RemovesLikeFromDatabase()
        {
            // Arrange
            var blog = await SeedBlogAsync(_testUser.Id, "Remove Like Blog");

            await LikeBlogAsync(blog.Id); // add like

            // Act
            await LikeBlogAsync(blog.Id); // remove like

            // Assert
            var likeExists = await _dbContext.Set<BlogLike>()
                .AnyAsync(l => l.BlogId == blog.Id && l.UserId == _testUser.Id);
            likeExists.Should().BeFalse();
        }

        #endregion

        #region Multiple Users

        [Fact]
        public async Task LikeBlog_TwoDifferentUsers_BothCanLikeSameBlog()
        {
            // Arrange
            var blog       = await SeedBlogAsync(_testUser.Id, "Multi User Blog", likeCount: 0);
            var otherUser  = await SeedUserAsync("likeblog2@test.com");
            var otherToken = await GetAccessTokenAsync("likeblog2@test.com", "Password123!");

            // Act
            var r1 = await LikeBlogAsync(blog.Id, _accessToken);
            var r2 = await LikeBlogAsync(blog.Id, otherToken);

            // Assert
            r1.Data!.IsLiked.Should().BeTrue();
            r2.Data!.IsLiked.Should().BeTrue();
            r2.Data.LikeCount.Should().Be(2);
        }

        #endregion
    }

    #region Response Models

    public class LikeBlogLoginData
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LikeBlogLoginApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public LikeBlogLoginData Data { get; set; } = new();
    }

    public class LikeBlogApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public LikeBlogResponseData? Data { get; set; }
    }

    public class LikeBlogResponseData
    {
        [JsonPropertyName("isLiked")]
        public bool IsLiked { get; set; }

        [JsonPropertyName("likeCount")]
        public int LikeCount { get; set; }
    }

    #endregion
}
