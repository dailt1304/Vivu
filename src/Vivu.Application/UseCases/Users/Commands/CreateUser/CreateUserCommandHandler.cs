using AutoMapper;
using MediatR;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;
using UserRoleEnum = Vivu.Domain.Enums.UserRole;

namespace Vivu.Application.UseCases.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;

        public CreateUserCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IMapper mapper)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
        }

        public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
         {
            if (await _userRepository.FindByEmailAsync(request.Email) != null)
                return Result<UserDto>.Failure(DomainErrors.User.EmailAlreadyExists);

            var roleName = request.Role switch
            {
                UserRoleEnum.Admin => Role.Names.Admin,
                UserRoleEnum.Moderator => Role.Names.Moderator,
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(roleName))
                return Result<UserDto>.Failure(DomainErrors.Role.NotFoundByRole(request.Role.ToString()));

            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role == null)
                return Result<UserDto>.Failure(DomainErrors.Role.NotFoundByRole(roleName));

            var hash = _passwordHasher.HashPassword(request.Password);

            var user = User.Create(
                email: request.Email,
                passwordHash: hash,
                phone: request.Phone,
                fullName: request.FullName,
                avatarUrl: request.AvatarUrl,
                bio: request.Bio,
                dateOfBirth: request.DateOfBirth,
                gender: request.Gender
            );

            if (user.UserProfile != null)
            {
                user.UserProfile.CountryId = request.CountryId;
            }

            user.AssignRole(role.Id);

            var createdUser = await _userRepository.AddAsync(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var userDto = _mapper.Map<UserDto>(createdUser);
            return Result<UserDto>.Success(userDto);
        }
    }
}

