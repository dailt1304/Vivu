using MediatR;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Cities.Commands.DeleteCity
{
    public record DeleteCityCommand : IRequest<Result<bool>>
    {
        public Guid CityId { get; set; }
    }
}
