using FluentValidation;

namespace Vivu.Application.UseCases.Users.Commands.UploadAvatar
{
    public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
    {
        public UploadAvatarCommandValidator()
        {
            RuleFor(x => x.Avatar)
                .NotNull().WithMessage("File image is required")
                .Must(x => x is not null && x.Length <= 5 * 1024 * 1024).WithMessage("File size must not exceed 5MB")
                .Must(x => 
                {
                    if (x is null) return false;
                    var ext = System.IO.Path.GetExtension(x.FileName).ToLower();
                    return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
                }).WithMessage("Only JPG, JPEG, PNG, WEBP files are allowed");
        }
    }
}
