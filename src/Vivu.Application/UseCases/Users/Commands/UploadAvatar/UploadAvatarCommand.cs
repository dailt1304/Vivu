using MediatR;
using Microsoft.AspNetCore.Http;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommand : IRequest<Result<string>>
    {
        public IFormFile Avatar { get; set; } = null!;
    }
}
