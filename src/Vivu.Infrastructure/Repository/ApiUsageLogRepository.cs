using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class ApiUsageLogRepository : GenericRepository<ApiUsageLog>, IApiUsageLogRepository
    {
        public ApiUsageLogRepository(VivuDbContext context) : base(context) { }
        public async Task<int> GetUserUsageWithDateRange(DateTime startDate, DateTime endDate, Guid userId, CancellationToken cancellationToken)
        {
            var currentCount = await _context.ApiUsageLogs
                        .Where(log => log.UserId == userId &&
                                     log.CreatedDate >= startDate &&
                                     log.CreatedDate < endDate)
                        .CountAsync(cancellationToken);
            return currentCount;
        }
    }
}
