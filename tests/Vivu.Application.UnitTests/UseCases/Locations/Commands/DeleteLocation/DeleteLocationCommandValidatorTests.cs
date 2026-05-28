using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Commands.DeleteLocation;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.DeleteLocation
{
    public class DeleteLocationCommandValidatorTests
    {
        private readonly DeleteLocationCommandValidator _validator;

        public DeleteLocationCommandValidatorTests()
        {
            _validator = new DeleteLocationCommandValidator();
        }

        #region LocationId Validation

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            var command = new DeleteLocationCommand
            {
                LocationId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
        }

        [Fact]
        public void Validate_EmptyLocationId_FailsValidation()
        {
            var command = new DeleteLocationCommand
            {
                LocationId = Guid.Empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("Location ID is required.");
        }

        #endregion

        #region Multiple Fields Validation

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            var command = new DeleteLocationCommand
            {
                LocationId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyCommand_FailsValidation()
        {
            var command = new DeleteLocationCommand
            {
                LocationId = Guid.Empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId);
        }

        #endregion
    }
}
