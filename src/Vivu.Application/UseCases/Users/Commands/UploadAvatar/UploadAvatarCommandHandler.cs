using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.Files;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<string>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly ICloudinaryService _cloudinaryService;

        public UploadAvatarCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            ICloudinaryService cloudinaryService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<Result<string>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                return Result<string>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var user = await _userRepository.GetAllQuery()
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            if (user == null)
            {
                return Result<string>.Failure(DomainErrors.User.NotFoundById(currentUserId));
            }

            if (user.IsBanned())
            {
                return Result<string>.Failure(DomainErrors.User.Banned);
            }

            using var stream = request.Avatar.OpenReadStream();
            var avatarUrl = await _cloudinaryService.UploadImageAsync(stream, request.Avatar.FileName, "avatars");

            if (user.UserProfile == null)
            {
                user.UserProfile = new Domain.Entities.UserProfile
                {
                    UserId = user.Id,
                    AvatarUrl = avatarUrl
                };
            }
            else
            {
                user.UserProfile.AvatarUrl = avatarUrl;
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(avatarUrl);
        }
    }
}
