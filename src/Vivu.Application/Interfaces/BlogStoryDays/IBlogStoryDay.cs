using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Domain.Entities;

namespace Vivu.Application.Interfaces.BlogStoryDays
{
    public interface IBlogStoryDay
    {
        Task UpdateStoryDaysAsync(Blog blog,List<UpdateBlogStoryDayDto> storyDays,CancellationToken cancellationToken);
    }
}
