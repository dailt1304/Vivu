using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Commands.RejectLocationSuggestion;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.RejectLocationSuggestion
{
    public class RejectLocationSuggestionCommandValidatorTests
    {
        private readonly RejectLocationSuggestionCommandValidator _validator;

        public RejectLocationSuggestionCommandValidatorTests()
        {
            _validator = new RejectLocationSuggestionCommandValidator();
        }

        #region LocationId Validation

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Test admin note"
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
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.Empty,
                AdminNote = "Test admin note"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("Location ID is required.");
        }

        #endregion

        #region AdminNote Validation

        [Fact]
        public void Validate_ValidAdminNote_PassesValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "This location suggestion has been rejected due to policy violations."
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_NullAdminNote_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AdminNote)
                .WithErrorMessage("Admin note is required.");
        }

        [Fact]
        public void Validate_EmptyAdminNote_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AdminNote)
                .WithErrorMessage("Admin note is required.");
        }

        [Fact]
        public void Validate_WhitespaceAdminNote_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "   "
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.AdminNote)
                .WithErrorMessage("Admin note is required.");
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = "Rejected due to inappropriate content"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_AllFieldsEmpty_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.Empty,
                AdminNote = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId);
            result.ShouldHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_OnlyLocationIdValid_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.NewGuid(),
                AdminNote = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
            result.ShouldHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_OnlyAdminNoteValid_FailsValidation()
        {
            // Arrange
            var command = new RejectLocationSuggestionCommand
            {
                LocationId = Guid.Empty,
                AdminNote = "Valid note"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.LocationId);
            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        #endregion
    }
}
