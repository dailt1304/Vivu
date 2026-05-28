namespace Vivu.Application.DTOs.Responses.Trips;

public class TripImageDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAvatarUrl { get; set; }
}
