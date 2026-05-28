using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vivu.Application.UseCases.Blogs.Commands.CreateBlogFromTrip;
using Vivu.Application.UseCases.Blogs.Commands.CreateComment;
using Vivu.Application.UseCases.Blogs.Commands.DeleteBlog;
using Vivu.Application.UseCases.Blogs.Commands.DeleteComment;
using Vivu.Application.UseCases.Blogs.Commands.BookmarkBlog;
using Vivu.Application.UseCases.Blogs.Commands.LikeBlog;
using Vivu.Application.UseCases.Blogs.Commands.PublishBlog;
using Vivu.Application.UseCases.Blogs.Commands.ReportBlog;
using Vivu.Application.UseCases.Blogs.Commands.FlagBlog;
using Vivu.Application.UseCases.Blogs.Commands.UpdateBlog;
using Vivu.Application.UseCases.BlogReports.Commands.BanBlog;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogDetail;
using Vivu.Application.UseCases.Blogs.Queries.GetBlogs;
using Vivu.Application.UseCases.Blogs.Queries.GetComments;
using Vivu.Application.UseCases.Blogs.Queries.GetMyBookmarks;
using Vivu.Application.UseCases.Blogs.Queries.GetUserBlogs;
using Vivu.Application.UseCases.Blogs.Queries.GetMyDrafts;
using Vivu.Application.UseCases.Blogs.Queries.SearchPublicBlogs;
using Vivu.Domain.Errors;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogsController : ApiControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs(
            [FromQuery] string? sortBy = "newest",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            var isAdmin = User.Identity?.IsAuthenticated == true && (User.IsInRole("MODERATOR") || User.IsInRole("ADMIN"));

            var query = new GetBlogsQuery
            {
                SortBy = sortBy ?? "newest",
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = status,
                IsAdmin = isAdmin
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicBlogs(
            [FromQuery] string q,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new SearchPublicBlogsQuery
            {
                SearchTerm = q,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyBlogs(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user token." });
            }

            var query = new GetUserBlogsQuery
            {
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpGet("me/drafts")]
        [Authorize]
        public async Task<IActionResult> GetMyDrafts(CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetMyDraftsQuery(), cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{idOrSlug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogDetail([FromRoute] string idOrSlug)
        {
            var query = new GetBlogDetailQuery { IdOrSlug = idOrSlug };
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        [HttpPost("from-trip")]
        [Authorize]
        public async Task<IActionResult> CreateBlogFromTrip(
            [FromForm] CreateBlogFromTripCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPut("{blogId}")]
        public async Task<IActionResult> UpdateBlog(
            Guid blogId,
            [FromForm] UpdateBlogCommand command,
            CancellationToken cancellationToken)
        {
            command.BlogId = blogId;
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpDelete("{blogId}")]
        public async Task<IActionResult> DeleteBlog(
            [FromRoute] Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new DeleteBlogCommand { BlogId = blogId });
            return HandleResult(result);
        }

        [HttpPatch("{blogId}/publish")]
        public async Task<IActionResult> PublishBlog(
            Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new PublishBlogCommand { BlogId = blogId }, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{blogId}/like")]
        [Authorize]
        public async Task<IActionResult> LikeBlog(
            [FromRoute] Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new LikeBlogCommand { BlogId = blogId }, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{blogId}/bookmark")]
        [Authorize]
        public async Task<IActionResult> BookmarkBlog(
            [FromRoute] Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new BookmarkBlogCommand { BlogId = blogId }, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("me/bookmarks")]
        [Authorize]
        public async Task<IActionResult> GetMyBookmarks(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetMyBookmarksQuery { PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken);
            return HandleResult(result);
        }

        // ── Reports ──────────────────────────────────────────────────────
        
        [HttpPost("{blogId}/report")]
        [Authorize]
        public async Task<IActionResult> ReportBlog(
            [FromRoute] Guid blogId,
            [FromBody] ReportBlogCommand command,
            CancellationToken cancellationToken)
        {
            command.BlogId = blogId;
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{blogId}/flag")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> FlagBlog(
            [FromRoute] Guid blogId,
            [FromBody] FlagBlogCommand command,
            CancellationToken cancellationToken)
        {
            command.BlogId = blogId;
            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        [HttpPatch("{blogId}/ban")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> BanBlog(
            [FromRoute] Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new BanBlogCommand { BlogId = blogId },
                cancellationToken);
            return HandleResult(result);
        }

        [HttpPatch("{blogId}/unban")]
        [Authorize(Roles = "MODERATOR")]
        public async Task<IActionResult> UnbanBlog(
            [FromRoute] Guid blogId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new Vivu.Application.UseCases.BlogReports.Commands.UnbanBlog.UnbanBlogCommand { BlogId = blogId },
                cancellationToken);
            return HandleResult(result);
        }

        // ── Comments ──────────────────────────────────────────────────────

        [HttpPost("{blogId}/comments")]
        [Authorize]
        public async Task<IActionResult> CreateComment(
            [FromRoute] Guid blogId,
            [FromBody] CreateCommentCommand command,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                command with { BlogId = blogId },
                cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{blogId}/comments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComments(
            [FromRoute] Guid blogId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetCommentsQuery { BlogId = blogId, PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("{blogId}/comments/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(
            [FromRoute] Guid blogId,
            [FromRoute] Guid commentId,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new DeleteCommentCommand { CommentId = commentId },
                cancellationToken);
            return HandleResult(result);
        }
    }

}
