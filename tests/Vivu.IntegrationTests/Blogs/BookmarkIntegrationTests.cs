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
    public class BookmarkIntegrationTests : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
    {
        private readonly IntegrationTestWebAppFactory _factory;
        private readonly HttpClient _client;
        private readonly IServiceScope _scope;
        private readonly VivuDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;

        private User _userA = null!;
        private User _userB = null!;
        private string _tokenA = string.Empty;
        private string _tokenB = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        public BookmarkIntegrationTests(IntegrationTestWebAppFactory factory)
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
            _userA  = await SeedUserAsync("bookmark_a@test.com", "Password123!");
            _userB  = await SeedUserAsync("bookmark_b@test.com", "Password123!");
            _tokenA = await GetAccessTokenAsync("bookmark_a@test.com", "Password123!");
            _tokenB = await GetAccessTokenAsync("bookmark_b@test.com", "Password123!");
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
                title: "Blog " + Guid.NewGuid().ToString("N")[..6],
                slug: "slug-" + Guid.NewGuid().ToString("N")[..8],
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

        private record BookmarkData(bool IsBookmarked, int SaveCount);
        private record BlogData(Guid Id, string Title, string Slug);
        private record PaginatedBlogs(List<BlogData> Items, int TotalCount,
            int PageNumber, int PageSize, bool HasNextPage, bool HasPreviousPage);

        // ═════════════════════════════════════════════════════════════════
        // BookmarkBlog Tests  POST /api/Blogs/{blogId}/bookmark
        // ═════════════════════════════════════════════════════════════════

        #region Auth Guard

        [Fact]
        public async Task BookmarkBlog_WithoutToken_Returns401()
        {
            ClearAuth();
            var blog = await SeedBlogAsync(_userA.Id);

            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Failures

        [Fact]
        public async Task BookmarkBlog_BlogNotFound_Returns404()
        {
            SetAuth(_tokenA);
            var response = await _client.PostAsync($"/api/Blogs/{Guid.NewGuid()}/bookmark", null);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BookmarkBlog_BlogDeleted_Returns400()
        {
            SetAuth(_tokenA);
            var blog     = await SeedBlogAsync(_userA.Id, status: "deleted");
            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task BookmarkBlog_BlogNotPublished_Returns400()
        {
            SetAuth(_tokenA);
            var blog     = await SeedBlogAsync(_userA.Id, status: "draft");
            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Toggle ON

        [Fact]
        public async Task BookmarkBlog_FirstTime_ReturnsIsBookmarkedTrue()
        {
            SetAuth(_tokenA);
            var blog = await SeedBlogAsync(_userB.Id);

            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookmarkData>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data!.IsBookmarked.Should().BeTrue();
            body.Data.SaveCount.Should().Be(1);
        }

        [Fact]
        public async Task BookmarkBlog_FirstTime_PersistsSaveInDatabase()
        {
            SetAuth(_tokenA);
            var blog = await SeedBlogAsync(_userB.Id);

            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            var exists = await _dbContext.Set<BlogSave>()
                .AnyAsync(s => s.BlogId == blog.Id && s.UserId == _userA.Id);
            exists.Should().BeTrue();
        }

        #endregion

        #region Toggle OFF

        [Fact]
        public async Task BookmarkBlog_SecondTime_ReturnsIsBookmarkedFalse()
        {
            SetAuth(_tokenA);
            var blog = await SeedBlogAsync(_userB.Id);

            // First toggle → ON
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            // Second toggle → OFF
            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookmarkData>>(JsonOpts);
            body!.Data!.IsBookmarked.Should().BeFalse();
            body.Data.SaveCount.Should().Be(0);
        }

        [Fact]
        public async Task BookmarkBlog_SecondTime_RemovesSaveFromDatabase()
        {
            SetAuth(_tokenA);
            var blog = await SeedBlogAsync(_userB.Id);

            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            var exists = await _dbContext.Set<BlogSave>()
                .AnyAsync(s => s.BlogId == blog.Id && s.UserId == _userA.Id);
            exists.Should().BeFalse();
        }

        #endregion

        #region Multiple Users

        [Fact]
        public async Task BookmarkBlog_TwoUsersBookmarkSameBlog_SaveCountIsTwo()
        {
            var blog = await SeedBlogAsync(_userA.Id);

            SetAuth(_tokenA);
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            SetAuth(_tokenB);
            var response = await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<BookmarkData>>(JsonOpts);
            body!.Data!.SaveCount.Should().Be(2);
        }

        #endregion

        // ═════════════════════════════════════════════════════════════════
        // GetMyBookmarks Tests  GET /api/Blogs/me/bookmarks
        // ═════════════════════════════════════════════════════════════════

        #region Auth Guard

        [Fact]
        public async Task GetMyBookmarks_WithoutToken_Returns401()
        {
            ClearAuth();
            var response = await _client.GetAsync("/api/Blogs/me/bookmarks");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Happy Path

        [Fact]
        public async Task GetMyBookmarks_NoBookmarks_ReturnsEmptyList()
        {
            SetAuth(_tokenA);
            var response = await _client.GetAsync("/api/Blogs/me/bookmarks");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedBlogs>>(JsonOpts);
            body!.Success.Should().BeTrue();
            body.Data!.Items.Should().BeEmpty();
            body.Data.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetMyBookmarks_WithBookmarks_ReturnsCorrectCount()
        {
            var blog1 = await SeedBlogAsync(_userB.Id);
            var blog2 = await SeedBlogAsync(_userB.Id);

            SetAuth(_tokenA);
            await _client.PostAsync($"/api/Blogs/{blog1.Id}/bookmark", null);
            await _client.PostAsync($"/api/Blogs/{blog2.Id}/bookmark", null);

            var response = await _client.GetAsync("/api/Blogs/me/bookmarks");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedBlogs>>(JsonOpts);

            body!.Data!.TotalCount.Should().Be(2);
            body.Data.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyBookmarks_OnlyReturnsCurrentUsersBookmarks()
        {
            var blog = await SeedBlogAsync(_userA.Id);

            // UserB bookmarks the blog
            SetAuth(_tokenB);
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            // UserA has no bookmarks
            SetAuth(_tokenA);
            var response = await _client.GetAsync("/api/Blogs/me/bookmarks");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedBlogs>>(JsonOpts);

            body!.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyBookmarks_AfterUnbookmark_DoesNotReturnRemovedBlog()
        {
            var blog = await SeedBlogAsync(_userB.Id);

            SetAuth(_tokenA);
            // Toggle ON then OFF
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);
            await _client.PostAsync($"/api/Blogs/{blog.Id}/bookmark", null);

            var response = await _client.GetAsync("/api/Blogs/me/bookmarks");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedBlogs>>(JsonOpts);

            body!.Data!.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMyBookmarks_Pagination_ReturnsCorrectPage()
        {
            var blogs = new List<Blog>();
            for (var i = 0; i < 7; i++)
                blogs.Add(await SeedBlogAsync(_userB.Id));

            SetAuth(_tokenA);
            foreach (var b in blogs)
                await _client.PostAsync($"/api/Blogs/{b.Id}/bookmark", null);

            var response = await _client.GetAsync("/api/Blogs/me/bookmarks?pageNumber=2&pageSize=3");
            var body     = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedBlogs>>(JsonOpts);

            body!.Data!.TotalCount.Should().Be(7);
            body.Data.PageNumber.Should().Be(2);
            body.Data.Items.Should().HaveCount(3);
        }

        #endregion
    }
}
