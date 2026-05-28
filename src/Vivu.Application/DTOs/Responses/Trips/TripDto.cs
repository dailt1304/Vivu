namespace Vivu.Application.DTOs.Responses.Trips
{
    public class TripDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CityName { get; set; }

        public Guid OwnerId { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerAvatar { get; set; }
        public string? CoverUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? InviteCode { get; set; }
        public int? TripSize { get; set; }
        public string Status { get; set; } = "planning";
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? FavoritedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsOwner { get; set; }
        public int SaveCount { get; set; }
        public int MemberCount { get; set; }
        public int? Rating { get; set; }
        public string? ReviewContent { get; set; }
    }
}
