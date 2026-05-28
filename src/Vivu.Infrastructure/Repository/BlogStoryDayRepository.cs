using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.Interfaces.BlogStoryDays;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class BlogStoryDayRepository : GenericRepository<BlogStoryDay>, IBlogStoryDayRepository, IBlogStoryDay
    {
        public BlogStoryDayRepository(VivuDbContext context) : base(context) { }

        public async Task UpdateStoryDaysAsync(Blog blog, List<UpdateBlogStoryDayDto> storyDays, CancellationToken cancellationToken)
        {
            var existingDays = blog.BlogStoryDays.ToList();

            var incomingIds = storyDays
                .Where(d => d.Id.HasValue)
                .Select(d => d.Id!.Value)
                .ToHashSet();

            var daysToRemove = existingDays
                .Where(d => !incomingIds.Contains(d.Id))
                .ToList();

            foreach (var day in daysToRemove)
            {
                _dbSet.Remove(day);
            }

            foreach (var dayDto in storyDays)
            {
                var existing = dayDto.Id.HasValue ? existingDays.FirstOrDefault(d => d.Id == dayDto.Id.Value) : null;
                
                if (existing != null)
                {
                    existing.Title = dayDto.Title;
                    existing.Content = dayDto.Content;
                    existing.DestinationName = dayDto.DestinationName;
                    existing.DayNumber = dayDto.DayNumber;
                    existing.DisplayOrder = dayDto.DisplayOrder;
                    existing.BlockType = dayDto.BlockType;
                    existing.LocationId = dayDto.LocationId;
                    existing.ImageUrl = dayDto.ImageUrl;
                    existing.QuoteAuthor = dayDto.QuoteAuthor;
                    _dbSet.Update(existing);
                }
                else
                {
                    var newDay = BlogStoryDay.Create(
                        blogId: blog.Id,
                        blockType: dayDto.BlockType,
                        dayNumber: dayDto.DayNumber,
                        title: dayDto.Title,
                        content: dayDto.Content,
                        destinationName: dayDto.DestinationName,
                        locationId: dayDto.LocationId,
                        imageUrl: dayDto.ImageUrl,
                        displayOrder: dayDto.DisplayOrder,
                        quoteAuthor: dayDto.QuoteAuthor
                    );
                    await _dbSet.AddAsync(newDay);
                }
            }
        }
    }
}
