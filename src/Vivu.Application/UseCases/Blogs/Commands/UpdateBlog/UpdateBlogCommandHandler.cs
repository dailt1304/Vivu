using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Blogs;
using Vivu.Application.Interfaces.BlogStoryDays;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Blogs.Commands.UpdateBlog
{
    public class UpdateBlogCommandHandler
    : IRequestHandler<UpdateBlogCommand, Result<BlogDto>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogStoryDayRepository _blogStoryDayRepository;
        private readonly IBlogStoryDay _blogStoryDayService;
        private readonly IBlogTagRepository _blogTagRepository;
        private readonly IBlogPostTagRepository _blogPostTagRepository;
        private readonly ISlugService _slugService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<UpdateBlogCommandHandler> _logger;

        public UpdateBlogCommandHandler(
            IBlogRepository blogRepository,
            ISlugService slugService,
            IBlogStoryDayRepository blogStoryDayRepository,
            IBlogStoryDay blogStoryDayService,
            ICloudinaryService cloudinaryService,
            IBlogTagRepository blogTagRepository,
            IBlogPostTagRepository blogPostTagRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper,
            ILogger<UpdateBlogCommandHandler> logger)
        {
            _blogRepository = blogRepository;
            _blogStoryDayService = blogStoryDayService;
            _cloudinaryService = cloudinaryService;
            _blogStoryDayRepository = blogStoryDayRepository;
            _slugService = slugService;
            _blogTagRepository = blogTagRepository;
            _blogPostTagRepository = blogPostTagRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<BlogDto>> Handle(
            UpdateBlogCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<BlogDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var blog = await _blogRepository.GetBlogWithDetailsAsync(
                request.BlogId, cancellationToken);

            if (blog == null)
            {
                return Result<BlogDto>.Failure(DomainErrors.Blog.NotFound);

            }
            if (blog.UserId != userId)
            {
                return Result<BlogDto>.Failure(DomainErrors.Blog.NotOwner);
            }

            if (blog.Status != "draft" && blog.Status != "published")
            {
                return Result<BlogDto>.Failure(DomainErrors.Blog.CannotUpdate);
            }

            if (!string.IsNullOrEmpty(request.Title))
            {
                blog.Title = request.Title;
                if (blog.Status == "draft")
                {
                    var newSlug = _slugService.GenerateSlug(request.Title);
                    var slugExists = await _blogRepository.IsSlugExistsAsync(newSlug, cancellationToken);
                    blog.Slug = slugExists ? $"{newSlug}-{DateTime.UtcNow.Ticks}" : newSlug;
                }
            }

            if (request.ShortDescription != null)
            {
                blog.ShortDescription = request.ShortDescription;
            }

            if (request.TotalCost.HasValue)
            {
                blog.TotalCost = request.TotalCost;
            }

            if (request.GroupSize.HasValue)
            {
                blog.GroupSize = request.GroupSize;
            }

            if (request.CoverImage != null && request.CoverImage.Length > 0)
            {
                using var stream = request.CoverImage.OpenReadStream();

                blog.CoverImageUrl = await _cloudinaryService.UploadImageAsync(
                    stream,
                    request.CoverImage.FileName,
                    "blogs/covers"
                );
            }

            blog.UpdatedAt = DateTime.UtcNow;
            blog.ModifiedDate = DateTime.UtcNow;
            blog.ModifiedBy = userId.ToString();

            if (request.StoryDays != null)
            {
                foreach (var day in request.StoryDays)
                {
                    if (day.Image != null && day.Image.Length > 0)
                    {
                        using var stream = day.Image.OpenReadStream();
                        day.ImageUrl = await _cloudinaryService.UploadImageAsync(
                            stream,
                            day.Image.FileName,
                            "blogs/stories"
                        );
                    }
                }
                await _blogStoryDayService.UpdateStoryDaysAsync(blog, request.StoryDays, cancellationToken);
            }

            if (request.TagNames != null)
            {
                var existingPostTags = blog.BlogPostTags.ToList();
                var tagNames = request.TagNames;
                foreach (var postTag in existingPostTags)
                {
                    _blogPostTagRepository.Remove(postTag);
                }

                foreach (var tagName in tagNames.Distinct())
                {
                    var normalizedName = tagName.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(normalizedName)) continue;

                    var tag = await _blogTagRepository.GetByNameAsync(normalizedName, cancellationToken);

                    if (tag == null)
                    {
                        tag = BlogTag.Create(name: normalizedName);
                        await _blogTagRepository.AddAsync(tag);
                    }
                    else
                    {
                        tag.UseCount++;
                        _blogTagRepository.Update(tag);
                    }

                    var postTag = new BlogPostTag
                    {
                        BlogId = blog.Id,
                        TagId = tag.Id
                    };
                    await _blogPostTagRepository.AddAsync(postTag);
                }
            }

            _blogRepository.Update(blog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Blog {BlogId} updated by user {UserId}", blog.Id, userId);

            var updatedBlog = await _blogRepository.GetBlogWithDetailsAsync(
                blog.Id, cancellationToken);

            return Result<BlogDto>.Success(_mapper.Map<BlogDto>(updatedBlog));

        }
    }
}

