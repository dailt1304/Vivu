using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripMembers.Commands.LeaveTrip;
using Xunit;

namespace Vivu.Application.Tests.UseCases.TripMembers.Commands.LeaveTrip
{
    public class LeaveTripCommandValidatorTests
    {
        private readonly LeaveTripCommandValidator _validator;

        public LeaveTripCommandValidatorTests()
        {
            _validator = new LeaveTripCommandValidator();
        }

        #region Valid Input Tests

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithSpecificGuid_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = new Guid("12345678-1234-1234-1234-123456789012")
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region TripId Validation Tests

        [Fact]
        public void Validate_EmptyTripId_ShouldHaveError()
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("Trip ID is required");
        }

        [Fact]
        public void Validate_ValidGuidTripId_ShouldNotHaveError()
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000001")]
        [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff")]
        [InlineData("12345678-90ab-cdef-1234-567890abcdef")]
        public void Validate_DifferentValidGuids_ShouldNotHaveErrors(string guidString)
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = Guid.Parse(guidString)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NewlyGeneratedGuid_ShouldNotHaveError()
        {
            // Arrange
            var command = new LeaveTripCommand
            {
                TripId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
            result.Errors.Should().BeEmpty();
        }

        #endregion
    }
}
