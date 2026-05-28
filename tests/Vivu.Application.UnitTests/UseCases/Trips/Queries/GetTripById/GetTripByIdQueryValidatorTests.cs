using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Queries.GetTripById;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetTripById
{
    public class GetTripByIdQueryValidatorTests
    {
        private readonly GetTripByIdQueryValidator _validator = new();

        #region TripId

        [Fact]
        public void Validate_WhenTripIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.Empty,
                RequestUserId = Guid.NewGuid()
            });
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                  .WithErrorMessage("Trip ID is required.");
        }

        [Fact]
        public void Validate_WhenTripIdIsValid_HasNoError()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.NewGuid(),
                RequestUserId = Guid.NewGuid()
            });
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region RequestUserId

        [Fact]
        public void Validate_WhenRequestUserIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.NewGuid(),
                RequestUserId = Guid.Empty
            });
            result.ShouldHaveValidationErrorFor(x => x.RequestUserId)
                  .WithErrorMessage("User ID is required.");
        }

        [Fact]
        public void Validate_WhenRequestUserIdIsValid_HasNoError()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.NewGuid(),
                RequestUserId = Guid.NewGuid()
            });
            result.ShouldNotHaveValidationErrorFor(x => x.RequestUserId);
        }

        #endregion

        #region Combined

        [Fact]
        public void Validate_WhenBothIdsAreEmpty_HasBothErrors()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.Empty,
                RequestUserId = Guid.Empty
            });
            result.ShouldHaveValidationErrorFor(x => x.TripId);
            result.ShouldHaveValidationErrorFor(x => x.RequestUserId);
        }

        [Fact]
        public void Validate_WhenBothIdsAreValid_HasNoErrors()
        {
            var result = _validator.TestValidate(new GetTripByIdQuery
            {
                TripId        = Guid.NewGuid(),
                RequestUserId = Guid.NewGuid()
            });
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
