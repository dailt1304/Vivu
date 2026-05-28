using MediatR;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.TripImages.Queries.GetTripImages;

public class GetTripImagesQueryHandler
    : IRequestHandler<GetTripImagesQuery, Result<List<TripImageDto>>>
{
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly ITripMemberRepository _tripMemberRepository;
    private readonly ICurrentUser _currentUser;

    public GetTripImagesQueryHandler(
        IChatMessageRepository chatMessageRepository,
        ITripMemberRepository tripMemberRepository,
        ICurrentUser currentUser)
    {
        _chatMessageRepository = chatMessageRepository;
        _tripMemberRepository = tripMemberRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<List<TripImageDto>>> Handle(
        GetTripImagesQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var userId))
            return Result<List<TripImageDto>>.Failure(DomainErrors.Auth.InvalidToken);

        // Verify user is a member of the trip
        var isMember = await _tripMemberRepository.GetByTripAndUserAsync(
            request.TripId, userId, cancellationToken);
        if (isMember == null)
            return Result<List<TripImageDto>>.Failure(DomainErrors.TripMember.NotMember);

        // Query all chat messages with files for this trip
        // GetByTripId already includes Sender/UserProfile/Files
        var images = await _chatMessageRepository.GetByTripId(request.TripId, cancellationToken)
            .Where(m => m.Files.Any())
            .SelectMany(m => m.Files.Select(f => new TripImageDto
            {
                Id = f.Id,
                Url = f.Url,
                FileName = f.FileName,
                FileType = f.FileType,
                FileSize = f.FileSize,
                UploadedAt = f.CreatedDate,
                SenderId = m.SenderId,
                SenderName = m.Sender.UserProfile != null
                    && !string.IsNullOrEmpty(m.Sender.UserProfile.FullName)
                        ? m.Sender.UserProfile.FullName
                        : "user",
                SenderAvatarUrl = m.Sender.UserProfile != null
                    ? m.Sender.UserProfile.AvatarUrl
                    : null
            }))
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync(cancellationToken);

        return Result<List<TripImageDto>>.Success(images);
    }
}
