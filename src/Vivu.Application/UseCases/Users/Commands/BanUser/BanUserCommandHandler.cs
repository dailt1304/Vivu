using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Users.Commands.BanUser
{
    public class BanUserCommandHandler : IRequestHandler<BanUserCommand, Result<UserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<BanUserCommandHandler> _logger;

        public BanUserCommandHandler(
            IUserRepository userRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<BanUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<UserDto>> Handle(BanUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to ban user with ID: {UserId}", request.UserId);

            var user = await _userRepository.GetByIdAsync(request.UserId);

            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", request.UserId);
                return Result<UserDto>.Failure(DomainErrors.User.NotFoundById(request.UserId));
            }

            if (user.IsBanned())
            {
                _logger.LogWarning("User {UserId} is already banned", request.UserId);
                return Result<UserDto>.Failure(DomainErrors.User.AlreadyBanned);
            }

            user.Ban();

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User {UserId} ({Email}) has been banned. Reason: {Reason}", 
                user.Id, 
                user.Email, 
                request.Reason ?? "No reason provided");

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
    }
}
