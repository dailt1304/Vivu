using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Vivu.Application.DTOs.Responses.Trips;
using Vivu.Application.DTOs.Responses.TripLocation;
using Vivu.Application.DTOs.Responses.Locations;
using Vivu.Application.DTOs.Responses.Users;
using Vivu.Application.Interfaces.Trips;
using Vivu.Domain.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vivu.Application.DTOs.Responses.TripDay;
using Vivu.Application.DTOs.Responses.Cities;
using Vivu.Application.DTOs.Responses.LocationCategories;
using Vivu.Application.DTOs.Responses.LocationDetails;
using Vivu.Application.DTOs.Responses.Countries;
using Vivu.Application.DTOs.Responses.LocationReports;
using Vivu.Application.DTOs.Responses.BlogReports;
using Vivu.Domain.Enums;
using System.Text.Json;
using NetTopologySuite.Geometries;
using Vivu.Application.DTOs.Responses.ChatMessages;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.DTOs.Responses.Blogs;
using Vivu.Application.DTOs.Responses.Payments;
using Vivu.Application.DTOs.Responses.SubscriptionPackages;
using Vivu.Application.DTOs.Responses.Notifications;
using Vivu.Application.DTOs.Responses.Collections;

namespace Vivu.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Notification, NotificationDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate));

            CreateMap<Transaction, TransactionDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.PackageName, opt => opt.MapFrom(s => s.Package != null ? s.Package.Name : null));

            CreateMap<Transaction, CreatePaymentResponse>()
                .ForMember(d => d.TransactionId, opt => opt.MapFrom(s => s.Id));

            CreateMap<SubscriptionPackage, SubscriptionPackageDto>().ReverseMap();
            CreateMap<UserSubscription, UserSubscriptionDto>()
                .ForMember(d => d.PackageName, opt => opt.MapFrom(s => s.Package.Name))
                .ForMember(d => d.PackageCode, opt => opt.MapFrom(s => s.Package.Code))
                .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Package.Price))
                .ForMember(d => d.DurationDays, opt => opt.MapFrom(s => s.Package.DurationDays))
                .ForMember(d => d.MaxAiRequestPerDay, opt => opt.MapFrom(s => s.Package.MaxAiRequestPerDay));

            CreateMap<User, UserDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.UserProfile != null ? (s.UserProfile.FullName ?? string.Empty) : string.Empty))
                .ForMember(d => d.AvatarUrl, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.AvatarUrl : null))
                .ForMember(d => d.Bio, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.Bio : null))
                .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.DateOfBirth : null))
                .ForMember(d => d.Gender, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.Gender : null))
                .ForMember(d => d.CountryId, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.CountryId : null))
                .ForMember(d => d.EmailConfirmed, opt => opt.MapFrom(s => s.IsEmailVerified))
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault()));

            CreateMap<Trip, TripDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.Rating, opt => opt.MapFrom(s => s.TripRating != null ? (int?)s.TripRating.Rating : null))
                .ForMember(d => d.ReviewContent, opt => opt.MapFrom(s => s.TripRating != null ? s.TripRating.ReviewContent : null))
                .ForMember(d => d.IsOwner, opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    if (context.TryGetItems(out var items) && items.TryGetValue("CurrentUserId", out var userIdObj) && userIdObj is Guid currentUserId)
                    {
                        return src.UserId == currentUserId;
                    }
                    return false;
                }));

            CreateMap<Trip, PublicTripDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.Owner, opt => opt.MapFrom(s => s.User))
                .ForMember(d => d.MemberCount, opt => opt.MapFrom(s => s.TripMembers != null ? s.TripMembers.Count : 0))
                .ForMember(d => d.FavoritesCount, opt => opt.MapFrom(s => s.TripFavorites != null ? s.TripFavorites.Count : 0))
                .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : null))
                .ForMember(d => d.CountryName, opt => opt.MapFrom(s => s.City != null && s.City.Country != null ? s.City.Country.Name : null))
                .ForMember(d => d.DurationDays, opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    if (context.Items.TryGetValue("PublicTripService", out var serviceObj) && serviceObj is IPublicTripService service)
                    {
                        return service.CalculateDurationDays(src.StartDate, src.EndDate);
                    }
                    return null;
                }))
                .ForMember(d => d.TrendingScore, opt => opt.MapFrom((src, dest, destMember, context) =>
                {
                    if (context.Items.TryGetValue("PublicTripService", out var serviceObj) && serviceObj is IPublicTripService service)
                    {
                        return service.CalculateTrendingScore(
                            src.TripFavorites != null ? src.TripFavorites.Count : 0,
                            0, 
                            src.CreatedDate);
                    }
                    return null;
                }));

            CreateMap<Trip, TripPlanResponse>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
                .ForMember(dest => dest.Start, opt => opt.MapFrom(src =>
                    src.StartDate.HasValue
                        ? DateOnly.FromDateTime(src.StartDate.Value)
                        : DateOnly.MinValue))
                .ForMember(dest => dest.End, opt => opt.MapFrom(src =>
                    src.EndDate.HasValue
                        ? DateOnly.FromDateTime(src.EndDate.Value)
                        : DateOnly.MinValue))
                .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.TripSize ?? 1))
                .ForMember(dest => dest.Days, opt => opt.MapFrom(src =>
                    src.TripDays.OrderBy(d => d.DayIndex).ToList()))
                .ForMember(dest => dest.UsageMetrics, opt => opt.Ignore());

            CreateMap<TripDay, DayPlanDto>()
                .ForMember(dest => dest.DayIndex, opt => opt.MapFrom(src => src.DayIndex))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title ?? string.Empty))
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src =>
                    src.DayDate.HasValue
                        ? DateOnly.FromDateTime(src.DayDate.Value)
                        : DateOnly.MinValue))
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src =>
                    src.TripLocations.OrderBy(l => l.OrderIndex).ToList()));

            CreateMap<TripLocation, LocationPlanDto>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src =>
                    src.Location != null ? src.Location.Name : string.Empty))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Note ?? string.Empty))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src =>
                    src.StartTime.HasValue
                        ? TimeOnly.FromTimeSpan(src.StartTime.Value)
                        : TimeOnly.MinValue))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src =>
                    src.EndTime.HasValue
                        ? TimeOnly.FromTimeSpan(src.EndTime.Value)
                        : TimeOnly.MinValue))
                .ForMember(dest => dest.TransportMode, opt => opt.MapFrom(src => src.TransportMode))
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.OrderIndex))
                .ForMember(dest => dest.Alternatives, opt => opt.MapFrom(src => src.Alternatives));

            CreateMap<TripLocationAlternative, AlternativeLocationDto>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : string.Empty))
                .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason ?? string.Empty))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority));

            CreateMap<TripLocation, TripLocationResponse>()
                .ForMember(d => d.LocationName, opt => opt.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty))
                .ForMember(d => d.LocationAddress, opt => opt.MapFrom(s => s.Location != null ? s.Location.Address : null))
                .ForMember(d => d.Images, opt => opt.MapFrom(s => (s.Location != null && s.Location.LocationDetail != null) ? s.Location.LocationDetail.Images : null))
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate));

            CreateMap<Trip, DetailedTripDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.Owner, opt => opt.MapFrom(s => s.User))
                .ForMember(d => d.Members, opt => opt.MapFrom(s => s.TripMembers))
                .ForMember(d => d.TripDays, opt => opt.MapFrom(s => s.TripDays));

            CreateMap<User, TripMemberDto>()
                .ForMember(d => d.UserId, opt => opt.MapFrom(s => s.Id))
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.UserProfile != null ? (s.UserProfile.FullName ?? string.Empty) : string.Empty))
                .ForMember(d => d.AvatarUrl, opt => opt.MapFrom(s => s.UserProfile != null ? s.UserProfile.AvatarUrl : null))
                .ForMember(d => d.Role, opt => opt.Ignore())
                .ForMember(d => d.JoinedAt, opt => opt.Ignore());

            CreateMap<TripMember, TripMemberDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.User.UserProfile != null ? (s.User.UserProfile.FullName ?? string.Empty) : string.Empty))
                .ForMember(d => d.AvatarUrl, opt => opt.MapFrom(s => s.User.UserProfile != null ? s.User.UserProfile.AvatarUrl : null))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.User.Email))
                .ForMember(d => d.Title, opt => opt.MapFrom(s => s.Trip.Title));

            CreateMap<TripDay, TripDayDto>()
                .ForMember(d => d.Locations, opt => opt.MapFrom(s => s.TripLocations));

            CreateMap<TripLocation, TripLocationDto>()
                .ForMember(d => d.DayNumber, opt => opt.MapFrom(s => s.TripDay.DayIndex))
                .ForMember(d => d.Location, opt => opt.MapFrom(s => s.Location))
                .ForMember(d => d.Alternatives, opt => opt.MapFrom(s => s.Alternatives));

            CreateMap<TripLocationAlternative, TripLocationAlternativeDto>()
                .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty))
                .ForMember(d => d.LocationAddress, o => o.MapFrom(s => s.Location != null ? s.Location.Address : null))
                .ForMember(d => d.Images, o => o.MapFrom(s => s.Location != null && s.Location.LocationDetail != null ? s.Location.LocationDetail.Images : null))
                .ForMember(d => d.Latitude, o => o.MapFrom(s => s.Location != null ? s.Location.Latitude : (double?)null))
                .ForMember(d => d.Longitude, o => o.MapFrom(s => s.Location != null ? s.Location.Longitude : (double?)null));

            CreateMap<TripLocationAlternative, TripLocationAlternativeResponse>()
                .ForMember(d => d.LocationName, o => o.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty))
                .ForMember(d => d.LocationAddress, o => o.MapFrom(s => s.Location != null ? s.Location.Address : null))
                .ForMember(d => d.Images, o => o.MapFrom(s => s.Location != null && s.Location.LocationDetail != null ? s.Location.LocationDetail.Images : null))
                .ForMember(d => d.Latitude, o => o.MapFrom(s => s.Location != null ? s.Location.Latitude : (double?)null))
                .ForMember(d => d.Longitude, o => o.MapFrom(s => s.Location != null ? s.Location.Longitude : (double?)null));

            CreateMap<Domain.Entities.Location, LocationDto>()
                .ForMember(d => d.SubmittedByUserId, opt => opt.MapFrom(s => 
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString()) != null 
                    ? s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.UserId 
                    : null))
                .ForMember(d => d.SubmittedByUserName, opt => opt.MapFrom(s => 
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString()) != null 
                        && s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User != null
                        && s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User.UserProfile != null
                    ? s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User.UserProfile.FullName 
                    : null))
                .ForMember(d => d.SubmittedByUserEmail, opt => opt.MapFrom(s => 
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString()) != null 
                        && s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User != null
                    ? s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User.Email 
                    : null))
                .ForMember(d => d.SubmittedByUserAvatar, opt => opt.MapFrom(s =>
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString()) != null &&
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User != null &&
                    s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User!.UserProfile != null
                    ? s.LocationReports.FirstOrDefault(r => r.Status == ReportStatus.PENDING.ToString())!.User!.UserProfile!.AvatarUrl
                    : null));

            CreateMap<Domain.Entities.Location, PopularLocationDto>()
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City != null ? src.City.Name : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.CategoryIconUrl, opt => opt.MapFrom(src => src.Category != null ? src.Category.IconUrl : null))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.LocationDetail != null && !string.IsNullOrWhiteSpace(src.LocationDetail.Images) ? GetFirstImage(src.LocationDetail.Images) : null));

            CreateMap<City, CityDto>()
                .ForMember(d => d.LocationCount, opt => opt.MapFrom(s => s.Locations != null ? s.Locations.Count : 0));
            CreateMap<ChatMessage, ChatMessageDto>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.SenderName, opt => opt.MapFrom(s => s.Sender.UserProfile != null && !string.IsNullOrEmpty(s.Sender.UserProfile.FullName) ? s.Sender.UserProfile.FullName : "user"))
                .ForMember(d => d.SenderAvatar,opt => opt.MapFrom(s => s.Sender.UserProfile != null && !string.IsNullOrEmpty(s.Sender.UserProfile.AvatarUrl) ? s.Sender.UserProfile.AvatarUrl : null))
                .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.Files != null && s.Files.Any() ? s.Files.First().Url : null))
                .ForMember(d => d.ImageFileName, opt => opt.MapFrom(s => s.Files != null && s.Files.Any() ? s.Files.First().FileName : null));

            CreateMap<Country, CountryDto>();

            CreateMap<LocationCategory, LocationCategoryDto>()
                .ForMember(d => d.LocationCount, opt => opt.MapFrom(s => s.Locations != null ? s.Locations.Count : 0))
                .ForMember(d => d.CategoryType, opt => opt.MapFrom(s => (int)s.CategoryType))
                .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.IsActive));

            CreateMap<LocationDetail, LocationDetailDto>();

            CreateMap<TripDay, TripDayResponse>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.DayDate, opt => opt.MapFrom(s => s.DayDate))
                .ForMember(d => d.DateIndex, opt => opt.MapFrom(s => s.DayIndex))
                .ReverseMap();

            CreateMap<LocationReport, ReportLocationResponse>()
                .ForMember(d => d.Reason, opt => opt.MapFrom(s => s.ReportReason))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.ReportDescription));

            CreateMap<LocationReport, LocationReportDto>()
                .ForMember(d => d.LocationName, opt => opt.MapFrom(s => s.Location != null ? s.Location.Name : string.Empty))
                .ForMember(d => d.LocationAddress, opt => opt.MapFrom(s => s.Location != null ? s.Location.Address : null))
                .ForMember(d => d.ReporterId, opt => opt.MapFrom(s => s.UserId))
                .ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.User != null && s.User.UserProfile != null ? s.User.UserProfile.FullName : null))
                .ForMember(d => d.ReporterEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email : null))
                .ForMember(d => d.ReporterAvatar, opt => opt.MapFrom(s => s.User != null && s.User.UserProfile != null ? s.User.UserProfile.AvatarUrl : null))
                .ForMember(d => d.Reason, opt => opt.MapFrom(s => s.ReportReason))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.ReportDescription));

            CreateMap<Domain.Entities.Location, NearbyLocationDto>()
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.City != null ? src.City.Name : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.CategoryIconUrl, opt => opt.MapFrom(src => src.Category != null ? src.Category.IconUrl : null))
                .ForMember(dest => dest.ThumbnailUrl, opt => opt.MapFrom(src => src.LocationDetail != null && !string.IsNullOrWhiteSpace(src.LocationDetail.Images)
                                                                                ? GetFirstImage(src.LocationDetail.Images)
                                                                                : null))
                .ForMember(dest => dest.DistanceInMeters, opt => opt.Ignore());

            CreateMap<Collection, CollectionDto>()
                .ForMember(dest => dest.LocationCount, opt => opt.MapFrom(src => src.CollectionLocations.Count))
                .ForMember(dest => dest.ThumbnailUrls, opt => opt.MapFrom(src => 
                    src.CollectionLocations
                        .OrderByDescending(cl => cl.AddedAt)
                        .Take(3)
                        .Select(cl => cl.Location != null && cl.Location.LocationDetail != null && !string.IsNullOrWhiteSpace(cl.Location.LocationDetail.Images) 
                            ? GetFirstImage(cl.Location.LocationDetail.Images) 
                            : null)
                        .Where(url => url != null)
                        .ToList()));

            CreateMap<Collection, CollectionDetailDto>()
                .ForMember(dest => dest.LocationCount, opt => opt.MapFrom(src => src.CollectionLocations.Count))
                .ForMember(dest => dest.Locations, opt => opt.MapFrom(src => src.CollectionLocations));

            CreateMap<CollectionLocation, CollectionLocationDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Location.Name))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Location.Address))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => 
                    src.Location.LocationDetail != null && !string.IsNullOrWhiteSpace(src.Location.LocationDetail.Images) 
                        ? GetFirstImage(src.Location.LocationDetail.Images) 
                        : null))
                .ForMember(dest => dest.RatingAverage, opt => opt.MapFrom(src => src.Location.RatingAverage))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Location.Category != null ? src.Location.Category.Name : null))
                .ForMember(dest => dest.CityName, opt => opt.MapFrom(src => src.Location.City != null ? src.Location.City.Name : null))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Location.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Location.Longitude));

            CreateMap<Collection, CollectionSummaryDto>()
                .ForMember(dest => dest.LocationCount, opt => opt.MapFrom(src => src.CollectionLocations.Count));

            CreateMap<Blog, BlogDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.TripId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
                .ForMember(dest => dest.ShortDescription, opt => opt.MapFrom(src => src.ShortDescription))
                .ForMember(dest => dest.TravelDateStart, opt => opt.MapFrom(src => src.TravelDateStart))
                .ForMember(dest => dest.TravelDateEnd, opt => opt.MapFrom(src => src.TravelDateEnd))
                .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
                .ForMember(dest => dest.GroupSize, opt => opt.MapFrom(src => src.GroupSize))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.BlogStoryDays, opt => opt.MapFrom(src =>
                    src.BlogStoryDays.OrderBy(d => d.DisplayOrder).ToList()));

            CreateMap<Blog, PublicBlogDto>()
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikeCount))
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.ViewCount))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentCount))
                .ForMember(dest => dest.SaveCount, opt => opt.MapFrom(src => src.SaveCount))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.FullName : null))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.AvatarUrl : null));

            CreateMap<Blog, BlogDetailDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.FullName : null))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.AvatarUrl : null))
                .ForMember(dest => dest.BlogStoryDays, opt => opt.MapFrom(src =>
                    src.BlogStoryDays.OrderBy(d => d.DisplayOrder).ToList()))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                    src.BlogPostTags.Select(pt => pt.Tag).ToList()))
                .ForMember(dest => dest.TripLocations, opt => opt.MapFrom(src =>
                    src.Trip != null
                        ? src.Trip.TripDays
                            .OrderBy(d => d.DayIndex)
                            .SelectMany(d => d.TripLocations.OrderBy(l => l.OrderIndex))
                            .ToList()
                        : new List<TripLocation>()));

            CreateMap<BlogTag, BlogTagDto>();

            CreateMap<BlogComment, BlogCommentDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifiedDate))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.FullName : null))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src =>
                    src.User != null && src.User.UserProfile != null ? src.User.UserProfile.AvatarUrl : null));

            CreateMap<BlogReport, ReportBlogResponse>()
                .ForMember(d => d.Reason, opt => opt.MapFrom(s => s.ReportReason))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.ReportDescription));

            CreateMap<BlogReport, BlogReportDto>()
                .ForMember(d => d.BlogTitle, opt => opt.MapFrom(s => s.Blog != null ? s.Blog.Title : string.Empty))
                .ForMember(d => d.ReporterId, opt => opt.MapFrom(s => s.ReporterId))
                .ForMember(d => d.ReporterName, opt => opt.MapFrom(s => s.Reporter != null && s.Reporter.UserProfile != null ? s.Reporter.UserProfile.FullName : null))
                .ForMember(d => d.ReporterEmail, opt => opt.MapFrom(s => s.Reporter != null ? s.Reporter.Email : null))
                .ForMember(d => d.ReporterAvatar, opt => opt.MapFrom(s => s.Reporter != null && s.Reporter.UserProfile != null ? s.Reporter.UserProfile.AvatarUrl : null))
                .ForMember(d => d.Reason, opt => opt.MapFrom(s => s.ReportReason))
                .ForMember(d => d.Description, opt => opt.MapFrom(s => s.ReportDescription));


            CreateMap<BlogStoryDay, BlogStoryDayDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.BlockType, opt => opt.MapFrom(src => src.BlockType))
                .ForMember(dest => dest.DayNumber, opt => opt.MapFrom(src => src.DayNumber))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content))
                .ForMember(dest => dest.DestinationName, opt => opt.MapFrom(src => src.DestinationName))
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder));
        }
        private string? GetFirstImage(string imagesJson)
        {
            try
            {
                // Try parsing as array of objects with "url" property (current format)
                using var doc = JsonDocument.Parse(imagesJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    // Object format: [{"url": "...", "order": 0, "isPrimary": true}]
                    if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("url", out var urlProp))
                    {
                        return urlProp.GetString();
                    }
                    // String format: ["https://..."]
                    if (first.ValueKind == JsonValueKind.String)
                    {
                        return first.GetString();
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        private double CalculateDistanceInMeters(Point? locationPoint, Point? userPoint)
        {
            if (locationPoint == null || userPoint == null) return 0;

            var dLat = ToRadians(locationPoint.Y - userPoint.Y);
            var dLon = ToRadians(locationPoint.X - userPoint.X);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(userPoint.Y)) * Math.Cos(ToRadians(locationPoint.Y)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            const double earthRadiusMeters = 6371000;
            return earthRadiusMeters * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}
