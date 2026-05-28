using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion
{
    public class ApproveLocationSuggestionCommand : IRequest<Result<LocationDto>>
    {
        public Guid LocationId { get; set; }
        public string? AdminNote { get; set; }
    }
}
