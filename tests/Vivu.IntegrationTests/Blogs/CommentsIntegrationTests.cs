using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Domain.Entities;
using Vivu.Infrastructure.Data;
using Xunit;

namespace Vivu.IntegrationTests.Blogs
{
    [Collection("Integration Tests")]
    public class CommentsIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _blogOwner = null!;
        private User _otherUser = null!;
        private string _blogOwnerToken  = string.Empty;
        private string _otherUserToken  = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        public CommentsIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _blogOwner       = await SeedUserAsync("blogowner@test.com", "Password123!");
            _otherUser       = await SeedUserAsync("otheruser@test.com", "Password123!");
            _blogOwnerToken  = await GetAccessTokenAsync("blogowner@test.com", "Password123!");
            _otherUserToken  = await GetAccessTokenAsync("otheruser@test.com", "Password123!");
        }

        public async Task DisposeAsync()
        {
            await CleanupDatabaseAsync();
            _scope.Dispose();
        }

        // ── Cleanup ───────────────────────────────────────────────────────

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

        // ── Seed Helpers ──────────────────────────────────────────────────

        private async Task<User> SeedUserAsync(string email, string password)
        {
            var user = User.Create(
                email: email,
                passwordHash: _passwordHasher.HashPassword(password),
                fullName: "Test User");
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

        private async Task<Blog> SeedBlogAsync(Guid userId, string status = "published")
        {
            var blog = Blog.Create(
                userId: userId,
                tripId: null,
                title: "Test Blog",
                slug: "test-blog-" + Guid.NewGuid().ToString("N")[..8],
                coverImageUrl: null,
                shortDescription: "Short desc",
                travelDateStart: null,
                travelDateEnd: null,
                totalCost: null,
                groupSize: null);
            blog.Status      = status;
            blog.PublishedAt = status == "published" ? DateTime.UtcNow : null;

            _dbContext.Set<Blog>().Add(blog);
            await _dbContext.SaveChangesAsync();
            return blog;
        }

        private async Task<BlogComment> SeedCommentAsync(Guid blogId, Guid userId, string content = "Seeded comment",
            bool isHidden = false)
        {
            var comment = BlogComment.Create(blogId, userId, content);
            comment.IsHidden = isHidden;
            _dbContext.Set<BlogComment>().Add(comment);
            await _dbContext.SaveChangesAsync();
            return comment;
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
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOpts);
            return apiResponse!.Data.AccessToken;
        }

        private void SetAuth(string token)
            => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        private void ClearAuth()
            => _client.DefaultRequestHeaders.Authorization = null;

        // ── Response models ───────────────────────────────────────────────

        private record ApiResponse<T>(bool Success, T? Data, int StatusCode = 200,
            string? Message = null, string? Code = null);

        private record CommentData(Guid Id, Guid BlogId, Guid UserId, string Content,
            int LikeCount, string? AuthorName, string? AuthorAvatarUrl);

        private record PaginatedComments(List<CommentData> Items, int TotalCount,
            int PageNumber, int PageSize, bool HasNextPage, bool HasPreviousPage);

        // ═════════════════════════════════════════════════════════════════
        // CreateComment Tests
        // ═════════════════════════════════════════════════════════════════

        #region CreateComment – Auth Guard

        [Fact]
        public async Task CreateComment_WithoutToken_Returns401()
        {
            ClearAuth();
            var blog     = await SeedBlogAsync(_blogOwner.Id);
            var response = await _client.PostAsJsonAsync(
                $"/api/Blogs/{blog.Id}/comments",
                new { Content = "Hello" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region CreateComment – Failures

        [Fact]
        public async Task CreateComment_BlogNotFound_Returns404()
        {
            SetAuth(_otherUserToken);
            var response = await _client.PostAsJsonAsync(
                $"/api/Blogs/{Guid.NewGuid()}/comments",
                new { Content = "Hello" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateComment_BlogNotPublished_Returns400()
        {
            SetAuth(_otherUserToken);
            var blog     = await SeedBlogAsync(_blogOwner.Id, status: "draft");
            var response = await _client.PostAsJsonAsync(
                $"/api/Blogs/{blog.Id}/comments",
                new { Content = "Hello" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region CreateComment – Happy Path

        [Fact]
        public async Task CreateComment_ValidRequest_Returns200WithDto()
        {
            SetAuth(_otherUserToken);
            var blog = await SeedBlogAsync(_blogOwner.Id);

            var response = await _client.PostAsJsonAsync(
                $"/api/Blogs/{blog.Id}/comments",
                new { Content = "Great post!" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<CommentData>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Content.Should().Be("Great post!");
            body.Data.BlogId.Should().Be(blog.Id);
            body.Data.UserId.Should().Be(_otherUser.Id);
        }

        [Fact]
        public async Task CreateComment_ValidRequest_IncrementsCommentCount()
        {
            SetAuth(_otherUserToken);
            var blog = await SeedBlogAsync(_blogOwner.Id);

            await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/comments", new { Content = "C1" });
            await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/comments", new { Content = "C2" });

            var updated = await _dbContext.Set<Blog>().FindAsync(blog.Id);
            await _dbContext.Entry(updated!).ReloadAsync();
            updated!.CommentCount.Should().Be(2);
        }

        [Fact]
        public async Task CreateComment_ValidRequest_PersistsInDatabase()
        {
            SetAuth(_otherUserToken);
            var blog = await SeedBlogAsync(_blogOwner.Id);

            await _client.PostAsJsonAsync($"/api/Blogs/{blog.Id}/comments", new { Content = "Persisted" });

            var dbComment = await _dbContext.Set<BlogComment>()
                .Where(c => c.BlogId == blog.Id)
                .FirstOrDefaultAsync();

            dbComment.Should().NotBeNull();
            dbComment!.Content.Should().Be("Persisted");
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════
        // GetComments Tests
        // ═════════════════════════════════════════════════════════════════

        #region GetComments – Auth / Anonymous

        [Fact]
        public async Task GetComments_WithoutToken_Returns200()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_blogOwner.Id);
            var response = await _client.GetAsync($"/api/Blogs/{blog.Id}/comments");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region GetComments – Failures

        [Fact]
        public async Task GetComments_BlogNotFound_Returns404()
        {
            ClearAuth();
            var response = await _client.GetAsync($"/api/Blogs/{Guid.NewGuid()}/comments");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        #endregion

        #region GetComments – Happy Path

        [Fact]
        public async Task GetComments_EmptyBlog_ReturnsEmptyList()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_blogOwner.Id);

            var response = await _client.GetAsync($"/api/Blogs/{blog.Id}/comments");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedComments>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data!.Items.Should().BeEmpty();
            body.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetComments_WithComments_ReturnsCorrectCount()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_blogOwner.Id);
            await SeedCommentAsync(blog.Id, _otherUser.Id, "C1");
            await SeedCommentAsync(blog.Id, _otherUser.Id, "C2");

            var response = await _client.GetAsync($"/api/Blogs/{blog.Id}/comments");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedComments>>(JsonOpts);

            body!.Data!.TotalCount.Should().Be(2);
            body.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetComments_HiddenCommentsExcluded()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_blogOwner.Id);
            await SeedCommentAsync(blog.Id, _otherUser.Id, "Visible",   isHidden: false);
            await SeedCommentAsync(blog.Id, _otherUser.Id, "Hidden One", isHidden: true);

            var response = await _client.GetAsync($"/api/Blogs/{blog.Id}/comments");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedComments>>(JsonOpts);

            body!.Data!.Items.Should().HaveCount(1);
            body.Data.Items[0].Content.Should().Be("Visible");
        }

        [Fact]
        public async Task GetComments_Pagination_ReturnsCorrectPage()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_blogOwner.Id);
            for (var i = 1; i <= 8; i++)
                await SeedCommentAsync(blog.Id, _otherUser.Id, $"Comment {i}");

            var response = await _client.GetAsync($"/api/Blogs/{blog.Id}/comments?pageNumber=2&pageSize=3");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedComments>>(JsonOpts);

            body!.Data!.TotalCount.Should().Be(8);
            body.Data.PageNumber.Should().Be(2);
            body.Data.Items.Should().HaveCount(3);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════
        // DeleteComment Tests
        // ═════════════════════════════════════════════════════════════════

        #region DeleteComment – Auth Guard

        [Fact]
        public async Task DeleteComment_WithoutToken_Returns401()
        {
            ClearAuth();
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id);

            var response = await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region DeleteComment – Failures

        [Fact]
        public async Task DeleteComment_CommentNotFound_Returns404()
        {
            SetAuth(_otherUserToken);
            var blog     = await SeedBlogAsync(_blogOwner.Id);
            var response = await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{Guid.NewGuid()}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteComment_RandomUserDeletesOthersComment_Returns403()
        {
            // Seed a 3rd user who is neither comment owner nor blog owner
            var thirdUser      = await SeedUserAsync("thirduser@test.com", "Password123!");
            var thirdUserToken = await GetAccessTokenAsync("thirduser@test.com", "Password123!");

            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id, "Other's comment");

            SetAuth(thirdUserToken);
            var response = await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        #endregion

        #region DeleteComment – Happy Path

        [Fact]
        public async Task DeleteComment_CommentOwnerDeletes_Returns200()
        {
            SetAuth(_otherUserToken);
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id, "My comment");

            var response = await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteComment_CommentOwnerDeletes_RemovesFromDatabase()
        {
            SetAuth(_otherUserToken);
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id);

            await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");

            var exists = await _dbContext.Set<BlogComment>().AnyAsync(c => c.Id == comment.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteComment_BlogOwnerDeletesOthersComment_Returns200()
        {
            SetAuth(_blogOwnerToken);
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id, "Other comment");

            var response = await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(JsonOpts);
            body!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteComment_DecrementsCommentCount()
        {
            SetAuth(_otherUserToken);
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id);

            // manually set CommentCount = 1
            var dbBlog = await _dbContext.Set<Blog>().FindAsync(blog.Id);
            dbBlog!.CommentCount = 1;
            await _dbContext.SaveChangesAsync();

            await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");

            await _dbContext.Entry(dbBlog).ReloadAsync();
            dbBlog.CommentCount.Should().Be(0);
        }

        [Fact]
        public async Task DeleteComment_CommentCountNeverGoesNegative()
        {
            SetAuth(_otherUserToken);
            var blog    = await SeedBlogAsync(_blogOwner.Id);
            var comment = await SeedCommentAsync(blog.Id, _otherUser.Id);

            // CommentCount already 0
            await _client.DeleteAsync($"/api/Blogs/{blog.Id}/comments/{comment.Id}");

            var dbBlog = await _dbContext.Set<Blog>().FindAsync(blog.Id);
            await _dbContext.Entry(dbBlog!).ReloadAsync();
            dbBlog!.CommentCount.Should().Be(0);
        }

        #endregion
    }
}
