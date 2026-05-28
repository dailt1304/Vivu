namespace Vivu.Application.DTOs.Responses.Trips
{
    public class TripMemberDto
    {
        public Guid UserId { get; set; }
        public Guid TripId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "member";
        public DateTime JoinedAt { get; set; }
    }
}
