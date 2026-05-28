using MediatR;
using Vivu.Application.DTOs.Responses.Statistics;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Statictis.Queries.GetAdminDashboard;

public class GetAdminDashboardQuery : IRequest<Result<AdminDashboardDto>> { }
