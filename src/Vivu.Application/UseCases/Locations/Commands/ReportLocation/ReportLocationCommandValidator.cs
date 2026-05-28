using FluentValidation;
using Vivu.Domain.Enums;

namespace Vivu.Application.UseCases.Locations.Commands.ReportLocation;

public class ReportLocationCommandValidator : AbstractValidator<ReportLocationCommand>
{
    public ReportLocationCommandValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("Location ID is required");

        RuleFor(x => x.ReportType)
            .IsInEnum().WithMessage("Invalid report type. Valid values are: WRONG_INFO, CLOSED, OTHER")
            .Must(t => t != ReportType.NEW_LOCATION)
            .WithMessage("NEW_LOCATION is reserved for the location submission flow.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.EvidenceImages)
            .Must(images => images == null || images.Count <= 3)
            .WithMessage("You can upload a maximum of 3 evidence images.")
            .ForEach(imageRule =>
            {
                imageRule.Must(BeAValidImage).WithMessage("Only JPEG, PNG and WEBP images are allowed.");
                imageRule.Must(BeWithinFileSizeLimit).WithMessage("Each image must be less than 5MB.");
            });
    }

    private bool BeAValidImage(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null) return false;
        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()) && allowedExtensions.Contains(extension);
    }

    private bool BeWithinFileSizeLimit(Microsoft.AspNetCore.Http.IFormFile file)
    {
        if (file == null) return false;
        return file.Length <= 5 * 1024 * 1024; // 5MB limit
    }
}
