using System;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripDay.Commands.UpdateTripDay;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.UpdateTripDay
{
    public class UpdateTripDayCommandValidatorTests
    {
        private readonly UpdateTripDayValidator _validator;

        public UpdateTripDayCommandValidatorTests()
        {
            _validator = new UpdateTripDayValidator();
        }

        #region TripDayId Validation Tests

        [Fact]
        public void Validate_ValidTripDayId_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Updated Day"
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
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.Empty,
                Title = "Updated Day"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required.");
        }

        #endregion

        #region Title Validation Tests

        [Fact]
        public void Validate_ValidTitle_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1: Tokyo Adventure"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_NullTitle_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_EmptyTitle_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_WhitespaceTitle_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "   "
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleWith200Characters_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = new string('A', 200)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleWith201Characters_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = new string('A', 201)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must be at most 200 characters.");
        }

        [Fact]
        public void Validate_TitleWith300Characters_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = new string('B', 300)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must be at most 200 characters.");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(199)]
        public void Validate_TitleWithinMaxLength_PassesValidation(int length)
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = new string('X', length)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        #endregion

        #region DayDate Validation Tests

        [Fact]
        public void Validate_ValidDayDate_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_NullDayDate_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_PastDayDate_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(-10)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_FutureDayDate_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(100)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_ValidCommandWithAllProperties_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Day 1: Explore Tokyo",
                DayDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithOnlyTripDayId_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = null,
                DayDate = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithTitleOnly_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = "Updated Title",
                DayDate = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithDayDateOnly_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = null,
                DayDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyTripDayIdWithValidTitle_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.Empty,
                Title = "Valid Title"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId);
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_ValidTripDayIdWithLongTitle_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripDayCommand
            {
                TripDayId = Guid.NewGuid(),
                Title = new string('Z', 250)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripDayId);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        #endregion
    }
}
