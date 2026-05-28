using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripDay.Commands.RemoveTripDay;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.RemoveTripDay
{
    public class RemoveTripDayCommandValidatorTests
    {
        private readonly RemoveTripDayValidator _validator;

        public RemoveTripDayCommandValidatorTests()
        {
            _validator = new RemoveTripDayValidator();
        }

        #region TripDayId Validation Tests

        [Fact]
        public void Validate_ValidTripDayId_PassesValidation()
        {
            // Arrange
            var command = new RemoveTripDayCommand
            {
                TripDayId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripDayId);
        }

        [Fact]
        public void Validate_EmptyTripDayId_FailsValidation()
        {
            // Arrange
            var command = new RemoveTripDayCommand
            {
                TripDayId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required");
        }

        [Fact]
        public void Validate_DefaultGuidTripDayId_FailsValidation()
        {
            // Arrange
            var command = new RemoveTripDayCommand
            {
                TripDayId = default
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required");
        }

        #endregion

        #region Complete Object Validation Tests

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            // Arrange
            var command = new RemoveTripDayCommand
            {
                TripDayId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_CommandWithInvalidTripDayId_HasOneValidationError()
        {
            // Arrange
            var command = new RemoveTripDayCommand
            {
                TripDayId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
        }

        #endregion
    }
}
