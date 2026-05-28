using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Queries.GetUsageStats
{
    public record GetUsageStatsQuery : IRequest<Result<UsageStatsDto>> { }

}
