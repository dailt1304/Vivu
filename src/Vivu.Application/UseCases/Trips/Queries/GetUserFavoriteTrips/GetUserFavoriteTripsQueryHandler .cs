using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vivu.Application.Common.Models;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.Interfaces.Auth;
using Vivu.Application.Interfaces.TripFavourites;
using Vivu.Application.UseCases.Trips.Commands.FavouriteTrip;
using Vivu.Domain.Errors;
using Vivu.Domain.Interfaces;
using Vivu.Domain.Shared;

namespace Vivu.Application.UseCases.Trips.Queries.GetUserFavoriteTrips
{
    public class GetUserFavoriteTripsQueryHandler
    : IRequestHandler<GetUserFavoriteTripsQuery, Result<PaginatedList<TripDto>>>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IMapper _mapper;
        private readonly ITripFavoriteRepository _tripFavoriteRepository;
        private readonly IFilterTripFavourite _filterTripFavourite;
        private readonly ILogger<GetUserFavoriteTripsQueryHandler> _logger;


        public GetUserFavoriteTripsQueryHandler(
            IMapper mapper,
            ILogger<GetUserFavoriteTripsQueryHandler> logger,
            IFilterTripFavourite filterTripFavourite,
            ITripFavoriteRepository tripFavoriteRepository,
            ICurrentUser currentUserService)
        {
            _mapper = mapper;
            _tripFavoriteRepository = tripFavoriteRepository;
            _filterTripFavourite = filterTripFavourite;
            _logger = logger;
            _currentUser = currentUserService;
        }

        public async Task<Result<PaginatedList<TripDto>>> Handle(
            GetUserFavoriteTripsQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var userId))
            {
                _logger.LogWarning("Get User Favourite Trips failed: Invalid or missing user ID");
                return Result<PaginatedList<TripDto>>.Failure(DomainErrors.Auth.InvalidToken);
            }
            _logger.LogInformation("Fetching user {id} favourite trips", userId);
            var query = _tripFavoriteRepository.GetUserTripFavourite(userId);

            var totalCount = await query.CountAsync(cancellationToken);
            _logger.LogDebug("Found {totalcount} user trip favourite", totalCount);
            if (totalCount == 0)
            {
                var emptyResult = new PaginatedList<TripDto>(
                    new List<TripDto>(),
                    count: 0,
                    request.PageNumber,
                    request.PageSize);

                return Result<PaginatedList<TripDto>>.Success(emptyResult);
            }

            var sortedQuery = _filterTripFavourite.ApplySorting(query, request);
            _logger.LogDebug("Applied sorting to favourite trip");

            var projectedQuery = sortedQuery.Select(x => new TripDto
            {
                Id = x.Trip.Id,
                UserId = userId,
                Title = x.Trip.Title,
                Description = x.Trip.Description,
                CoverUrl = x.Trip.CoverUrl,
                StartDate = x.Trip.StartDate,
                EndDate = x.Trip.EndDate,
                Status = x.Trip.Status,
                IsPublic = x.Trip.IsPublic,
                CityId = x.Trip.CityId,
                CityName = x.Trip.City != null ? x.Trip.City.Name : null,
                OwnerId = x.Trip.UserId,
                OwnerName = x.Trip.User.UserProfile != null
                ? x.Trip.User.UserProfile.FullName
                : x.Trip.User.Email,
                OwnerAvatar = x.Trip.User.UserProfile != null
                ? x.Trip.User.UserProfile.AvatarUrl
                : null,
                TripSize = x.Trip.TripSize,
                CreatedAt = x.Trip.CreatedDate,
                FavoritedAt = x.CreatedAt,
                SaveCount = x.Trip.TripFavorites.Count,
                MemberCount = x.Trip.TripMembers.Count
            });
            _logger.LogDebug("Mapped query trip favourite to trip dto");

            var items = await projectedQuery
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync(cancellationToken);
            _logger.LogDebug("Found {countitems} after skip {skip} and take {take} trip favourite", items.Count,request.Skip, request.Take);

            var paginatedList = new PaginatedList<TripDto>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize);
            _logger.LogInformation("Fetched {count} user {id} favourite trip", paginatedList.Items.Count, userId);

            return Result<PaginatedList<TripDto>>.Success(paginatedList);
        }

    }
}
