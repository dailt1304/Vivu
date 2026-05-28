using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Common;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;
using Vivu.Domain.Entities;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRoleRepository _roleRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(IUserRepository userRepo, IUnitOfWork unitOfWork, IMapper mapper, IPasswordHasher hasher
                                          , IRoleRepository roleRepository, IOtpRepository otpRepository, ILogger<RegisterUserCommandHandler> logger)
        {
            _userRepository = userRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _passwordHasher = hasher;
            _roleRepository = roleRepository;
            _otpRepository = otpRepository;
            _logger = logger;
        }

        public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Registration attempt for email: {Email}, FullName: {FullName}",
                request.Email,
                request.FullName);
            var otp = await _otpRepository.GetValidOtpAsync(request.Email, request.OtpCode, Domain.Enums.OtpType.EmailVerification);

            if (otp == null)
            {
                _logger.LogWarning(
                    "Registration failed: Invalid OTP. Email: {Email}",
                    request.Email);
                return Result<UserDto>.Failure(DomainErrors.Auth.OtpInvalid);
            }
            _logger.LogDebug("OTP found for registration. Email: {Email}, OtpId: {OtpId}", request.Email, otp.Id);


            var verifyResult = otp.VerifyCode(request.OtpCode);
            if (verifyResult.IsFailure)
            {
                _logger.LogWarning(
                    "Registration failed: OTP verification failed. Email: {Email}, Error: {Error}",
                    request.Email,
                    verifyResult.Error.Code);
                return Result<UserDto>.Failure(verifyResult.Error);
            }

            otp.MarkAsUsed();
            _logger.LogDebug("OTP verified and marked as used. OtpId: {OtpId}", otp.Id);


            if (await _userRepository.FindByEmailAsync(request.Email) != null)
            {
                _logger.LogWarning(
                    "Registration failed: Email already exists. Email: {Email}",
                    request.Email);
                return Result<UserDto>.Failure(DomainErrors.User.EmailAlreadyExists);
            }
            _logger.LogDebug("Email available for registration. Email: {Email}", request.Email);


            var hash = _passwordHasher.HashPassword(request.Password);
            _logger.LogDebug("Password hashed successfully");

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
            user.VerifyEmail();
            _logger.LogDebug("User entity created. Email: {Email}", request.Email);

            var createdUser = await _userRepository.AddAsync(user);
            _logger.LogDebug("User added to repository. UserId: {UserId}", createdUser.Id);

            var userRole = await _roleRepository.GetByNameAsync(Role.Names.User);
            if (userRole == null)
            {
                _logger.LogError(
                    "Registration failed: User role not found in system. RoleName: {RoleName}",
                    Role.Names.User);
                return Result<UserDto>.Failure(DomainErrors.Role.NotFoundByRole(Role.Names.User));
            }
            user.AssignRole(userRole.Id);
            _logger.LogDebug("Role assigned to user. UserId: {UserId}, RoleId: {RoleId}", createdUser.Id, userRole.Id);


            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("User saved to database. UserId: {UserId}", createdUser.Id);

            var userDto = _mapper.Map<UserDto>(createdUser);
            userDto.FullName = request.FullName;
            _logger.LogInformation(
               "Registration successful. UserId: {UserId}, Email: {Email}, FullName: {FullName}, Role: {Role}",
               createdUser.Id,
               request.Email,
               request.FullName,
               Role.Names.User);
            return Result<UserDto>.Success(userDto);
        }
    }
}
