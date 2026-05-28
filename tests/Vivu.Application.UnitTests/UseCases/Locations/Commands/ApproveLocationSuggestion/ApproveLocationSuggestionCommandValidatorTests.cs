using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Commands.ApproveLocationSuggestion;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.ApproveLocationSuggestion
{
    public class ApproveLocationSuggestionCommandValidatorTests
    {
        private readonly ApproveLocationSuggestionCommandValidator _validator;

        public ApproveLocationSuggestionCommandValidatorTests()
        {
            _validator = new ApproveLocationSuggestionCommandValidator();
        }

        #region LocationId Validation

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
        }

        [Fact]
        public void Validate_EmptyLocationId_FailsValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("LocationId is required");
        }

        #endregion

        #region AdminNote Validation

        [Fact]
        public void Validate_ValidAdminNote_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "This location has been reviewed and approved."
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_NullAdminNote_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_EmptyAdminNote_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Approved after verification"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithoutAdminNote_PassesValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyCommand_FailsValidation()
        {
            // Arrange
            var command = new ApproveLocationSuggestionCommand
            {
                LocationId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId);
        }

        #endregion
    }
}
