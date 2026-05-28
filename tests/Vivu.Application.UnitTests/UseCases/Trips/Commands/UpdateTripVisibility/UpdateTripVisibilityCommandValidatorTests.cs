using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Commands.UpdateTripVisibility;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateTripVisibility
{
    public class UpdateTripVisibilityCommandValidatorTests
    {
        private readonly UpdateTripVisibilityCommandValidator _validator;

        public UpdateTripVisibilityCommandValidatorTests()
        {
            _validator = new UpdateTripVisibilityCommandValidator();
        }

        #region TripId Validation

        [Fact]
        public void Validate_EmptyTripId_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.Empty,
                IsPublic = true
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("Trip ID is required.");
        }

        [Fact]
        public void Validate_ValidTripId_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = true
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region IsPublic Validation

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Validate_AnyBooleanValue_PassesValidation(bool isPublic)
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = isPublic
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.IsPublic);
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_CompleteValidCommand_PassesAllValidations()
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = true
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MakePrivateCommand_PassesAllValidations()
        {
            // Arrange
            var command = new UpdateTripVisibilityCommand
            {
                TripId = Guid.NewGuid(),
                IsPublic = false
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
