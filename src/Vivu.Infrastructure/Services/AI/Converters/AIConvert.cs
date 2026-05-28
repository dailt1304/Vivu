using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Interfaces.AI;

namespace Vivu.Infrastructure.Services.AI.Converters
{
    public class AIConvert : IAIConvert
    {
        public TimeSpan? ConvertTimeOnlyToTimeSpan(TimeOnly? timeOnly)
        {
            if (!timeOnly.HasValue)
                return null;

            return new TimeSpan(timeOnly.Value.Hour, timeOnly.Value.Minute, timeOnly.Value.Second);
        }
    }
}
