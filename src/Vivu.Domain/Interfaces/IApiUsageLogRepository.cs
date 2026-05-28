using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface IApiUsageLogRepository : IGenericRepository<ApiUsageLog>
    {
        Task<int> GetUserUsageWithDateRange(DateTime startDate, DateTime endDate, Guid userId, CancellationToken cancellationToken);
    }
}
