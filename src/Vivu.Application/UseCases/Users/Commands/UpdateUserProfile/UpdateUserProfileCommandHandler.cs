using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;

        public UpdateUserProfileCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var currentUserId))
            {
                return Result<UserDto>.Failure(DomainErrors.Auth.InvalidToken);
            }

            var user = await _userRepository.GetAllQuery()
                .Include(u => u.UserProfile)
                .ThenInclude(up => up.Country)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            if (user == null)
            {
                return Result<UserDto>.Failure(DomainErrors.User.NotFoundById(currentUserId));
            }

            if (user.IsBanned())
            {
                return Result<UserDto>.Failure(DomainErrors.User.Banned);
            }

            if (user.UserProfile == null)
            {
                user.UserProfile = new Domain.Entities.UserProfile
                {
                    UserId = user.Id
                };
            }

            user.UserProfile.Update(
                fullName: request.FullName,
                bio: request.Bio,
                dateOfBirth: request.DateOfBirth.HasValue 
                    ? DateTime.SpecifyKind(request.DateOfBirth.Value.Date, DateTimeKind.Utc) 
                    : null,
                gender: request.Gender,
                countryId: request.CountryId,
                avatarUrl: request.AvatarUrl
            );

            user.UpdatePhone(request.Phone);

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
    }
}
