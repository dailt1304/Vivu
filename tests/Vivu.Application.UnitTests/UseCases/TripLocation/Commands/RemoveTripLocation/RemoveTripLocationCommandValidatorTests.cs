using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripLocation.Commands.RemoveTripLocation;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.RemoveTripLocation
{
    public class RemoveTripLocationCommandValidatorTests
    {
        private readonly RemoveTripLocationValidator _validator;

        public RemoveTripLocationCommandValidatorTests()
        {
            _validator = new RemoveTripLocationValidator();
        }

        #region TripLocationId Validation Tests

        [Fact]
        public void Validate_ValidTripLocationId_PassesValidation()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripLocationId);
        }

        [Fact]
        public void Validate_EmptyTripLocationId_FailsValidation()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripLocationId)
                .WithErrorMessage("TripLocationId is required");
        }

        [Fact]
        public void Validate_DefaultGuidTripLocationId_FailsValidation()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = default(Guid)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripLocationId)
                .WithErrorMessage("TripLocationId is required");
        }

        #endregion

        #region Complete Validation Tests

        [Fact]
        public void Validate_ValidCommand_PassesAllValidations()
        {
            // Arrange
            var command = new RemoveTripLocationCommand
            {
                TripLocationId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
