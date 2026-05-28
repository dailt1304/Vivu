using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Commands.DeleteTrip;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.DeleteTrip
{
    public class DeleteTripCommandValidatorTests
    {
        private readonly DeleteTripCommandValidator _validator = new();

        private static DeleteTripCommand Make(Guid? tripId = null)
            => new DeleteTripCommand { TripId = tripId ?? Guid.NewGuid() };

        [Fact]
        public void Validate_WhenTripIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(Make(tripId: Guid.Empty));
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                  .WithErrorMessage("Trip ID is required.");
        }

        [Fact]
        public void Validate_WhenTripIdIsValid_HasNoError()
        {
            var result = _validator.TestValidate(Make());
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }
    }
}
