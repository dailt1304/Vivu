using FluentValidation;

namespace Vivu.Application.UseCases.Locations.Commands.CreateLocation
{
    public class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
    {
        public CreateLocationCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên địa điểm không được để trống")
                .MaximumLength(200);

            RuleFor(x => x.Description).MaximumLength(2000);
            RuleFor(x => x.Address)
                .NotEmpty().WithMessage("Địa chỉ không được để trống")
                .MaximumLength(300);

            RuleFor(x => x.CityId)
                .NotNull().WithMessage("Vui lòng chọn Tỉnh/Thành phố")
                .NotEqual(Guid.Empty).WithMessage("Vui lòng chọn Tỉnh/Thành phố");

            RuleFor(x => x)
                .Must(x =>
                    (x.Latitude.HasValue && x.Longitude.HasValue) ||
                    (!x.Latitude.HasValue && !x.Longitude.HasValue))
                .WithMessage("Latitude and Longitude must be provided together.");

            When(x => x.Latitude.HasValue, () =>
            {
                RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90, 90);
            });

            When(x => x.Longitude.HasValue, () =>
            {
                RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180, 180);
            });

            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.Website).MaximumLength(300);
            RuleFor(x => x.Tags).MaximumLength(500);

            When(x => x.Images != null && x.Images.Any(), () =>
            {
                RuleForEach(x => x.Images)
                    .Must(file => file != null && file.Length <= 5 * 1024 * 1024)
                    .WithMessage("Each image must be smaller than 5MB");
            });
        }
    }
}
