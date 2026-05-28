namespace Vivu.Application.DTOs.Responses.Trips
{
    public class DetailedTripDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? InviteCode { get; set; }
        public int? TripSize { get; set; }
        public string Status { get; set; } = "planning";
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public TripMemberDto Owner { get; set; } = null!;
        public List<TripMemberDto> Members { get; set; } = new List<TripMemberDto>();
        public List<TripDayDto> TripDays { get; set; } = new List<TripDayDto>();

        public int MemberCount => Members.Count;
        public int DayCount => TripDays.Count;
        public int LocationCount => TripDays.Sum(d => d.Locations.Count);
    }
}
