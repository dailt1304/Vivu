using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
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

namespace Vivu.Application.UseCases.Blogs.Commands.CreateBlogFromTrip
{
    public class CreateBlogFromTripCommandHandler : IRequestHandler<CreateBlogFromTripCommand, Result<BlogDto>>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBlogRepository _blogRepository;
        private readonly IBlogStoryDay _blogStoryDayService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<CreateBlogFromTripCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IBlogTagRepository _blogTagRepository;
        private readonly IBlogPostTagRepository _blogPostTagRepository;
        private readonly ISlugService _slugService;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateBlogFromTripCommandHandler(
            ITripRepository tripRepository,
            IBlogRepository blogRepository,
            IBlogStoryDay blogStoryDayService,
            ISlugService slugService,
            IBlogPostTagRepository blogPostTagRepository,
            IBlogTagRepository blogTagRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ILogger<CreateBlogFromTripCommandHandler> logger,
            IMapper mapper,
            ICloudinaryService cloudinaryService) 
        {
            _tripRepository = tripRepository;
            _blogRepository = blogRepository;
            _blogStoryDayService = blogStoryDayService;
            _blogTagRepository = blogTagRepository;
            _blogPostTagRepository = blogPostTagRepository;
            _unitOfWork = unitOfWork;
            _slugService = slugService;
            _currentUser = currentUser;
            _logger = logger;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<Result<BlogDto>> Handle(
            CreateBlogFromTripCommand request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.Id, out var userId))
            {
                return Result<BlogDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var trip = await _tripRepository.GetTripByIdWithDetailsAsync(request.TripId, cancellationToken);

            if (trip == null || trip.IsDeleted)
            {
                return Result<BlogDto>.Failure(DomainErrors.Trip.NotFound);
            }
            if (trip.UserId != userId)
            {
                return Result<BlogDto>.Failure(DomainErrors.Trip.NotOwner);
            }
            if (!trip.IsPublic)
            {
                trip.IsPublic = true;
            }

            var blogTitle = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : trip.Title;
            var slug = _slugService.GenerateSlug(blogTitle);

            var existingSlug = await _blogRepository.IsSlugExistsAsync(slug, cancellationToken);
            if (existingSlug)
            {
                slug = $"{slug}-{DateTime.UtcNow.Ticks}";
            }

            string? uploadedCoverUrl = null;
            if (request.CoverImage != null && request.CoverImage.Length > 0)
            {
                using var stream = request.CoverImage.OpenReadStream();

                uploadedCoverUrl = await _cloudinaryService.UploadImageAsync(
                    stream,
                    request.CoverImage.FileName,
                    "blogs/covers"
                );
            }

            var blog = Blog.Create(
                userId: userId,
                tripId: trip.Id,
                title: blogTitle,
                slug: slug,
                coverImageUrl: uploadedCoverUrl,
                shortDescription: request.ShortDescription,
                travelDateStart: trip.StartDate,
                travelDateEnd: trip.EndDate,
                totalCost: request.TotalCost,
                groupSize: trip.TripSize
            );

            await _blogRepository.AddAsync(blog);

            if (request.TagNames != null && request.TagNames.Any())
            {
                foreach (var tagName in request.TagNames.Distinct())
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

            if (request.StoryDays != null && request.StoryDays.Any())
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
            else
            {
                int displayOrder = 0;
                foreach (var tripDay in trip.TripDays.OrderBy(d => d.DayIndex))
                {
                    foreach (var tripLoc in tripDay.TripLocations.OrderBy(l => l.OrderIndex))
                    {
                        var storyDay = BlogStoryDay.Create(
                            blogId: blog.Id,
                            blockType: "location",
                            dayNumber: tripDay.DayIndex,
                            title: tripDay.Title ?? $"Ngày {tripDay.DayIndex}",
                            content: null,
                            destinationName: tripLoc.Location?.Name,
                            locationId: tripLoc.LocationId,
                            imageUrl: null,
                            displayOrder: displayOrder++
                        );

                        blog.BlogStoryDays.Add(storyDay);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Blog created from trip {TripId} by user {UserId}, BlogId: {BlogId}",
                request.TripId, userId, blog.Id);

            var dto = _mapper.Map<BlogDto>(blog);
            return Result<BlogDto>.Success(dto);
        }
    }
}
