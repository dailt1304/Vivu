using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Commands.RateTrip;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.RateTrip
{
    public class RateTripCommandValidatorTests
    {
        private readonly RateTripCommandValidator _validator = new();

        #region TripId

        [Fact]
        public void Validate_WhenTripIdIsEmpty_HasError()
        {
            var result = _validator.TestValidate(new RateTripCommand { TripId = Guid.Empty, Rating = 3 });
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                  .WithErrorMessage("Trip ID is required");
        }

        [Fact]
        public void Validate_WhenTripIdIsValid_HasNoError()
        {
            var result = _validator.TestValidate(new RateTripCommand { TripId = Guid.NewGuid(), Rating = 3 });
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region Rating

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_WhenRatingBelowMinimum_HasError(int rating)
        {
            var result = _validator.TestValidate(new RateTripCommand { TripId = Guid.NewGuid(), Rating = rating });
            result.ShouldHaveValidationErrorFor(x => x.Rating)
                  .WithErrorMessage("Rating must be between 1 and 5");
        }

        [Theory]
        [InlineData(6)]
        [InlineData(10)]
        [InlineData(100)]
        public void Validate_WhenRatingAboveMaximum_HasError(int rating)
        {
            var result = _validator.TestValidate(new RateTripCommand { TripId = Guid.NewGuid(), Rating = rating });
            result.ShouldHaveValidationErrorFor(x => x.Rating)
                  .WithErrorMessage("Rating must be between 1 and 5");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Validate_WhenRatingInValidRange_HasNoError(int rating)
        {
            var result = _validator.TestValidate(new RateTripCommand { TripId = Guid.NewGuid(), Rating = rating });
            result.ShouldNotHaveValidationErrorFor(x => x.Rating);
        }

        #endregion

        #region ReviewContent

        [Fact]
        public void Validate_WhenReviewContentExceedsMaxLength_HasError()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 3,
                ReviewContent = new string('A', 1001)
            });
            result.ShouldHaveValidationErrorFor(x => x.ReviewContent)
                  .WithErrorMessage("Review content must not exceed 1000 characters");
        }

        [Fact]
        public void Validate_WhenReviewContentIsExactlyMaxLength_HasNoError()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 3,
                ReviewContent = new string('A', 1000)
            });
            result.ShouldNotHaveValidationErrorFor(x => x.ReviewContent);
        }

        [Fact]
        public void Validate_WhenReviewContentIsNull_HasNoError()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 3,
                ReviewContent = null
            });
            result.ShouldNotHaveValidationErrorFor(x => x.ReviewContent);
        }

        [Fact]
        public void Validate_WhenReviewContentIsEmpty_HasNoError()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 3,
                ReviewContent = string.Empty
            });
            result.ShouldNotHaveValidationErrorFor(x => x.ReviewContent);
        }

        #endregion

        #region Full Valid Command

        [Fact]
        public void Validate_WhenAllFieldsValid_HasNoErrors()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 4,
                ReviewContent = "Wonderful trip!"
            });
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WhenValidCommandWithoutReview_HasNoErrors()
        {
            var result = _validator.TestValidate(new RateTripCommand
            {
                TripId = Guid.NewGuid(),
                Rating = 5,
                ReviewContent = null
            });
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
