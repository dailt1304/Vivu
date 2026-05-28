using System;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripDay.Commands.AddTripDay;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.AddTripDay
{
    public class AddTripDayCommandValidatorTests
    {
        private readonly AddTripDayValidator _validator;

        public AddTripDayCommandValidatorTests()
        {
            _validator = new AddTripDayValidator();
        }

        #region TripId Validation Tests

        [Fact]
        public void Validate_ValidTripId_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        [Fact]
        public void Validate_EmptyTripId_FailsValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.Empty,
                Title = "Day 1"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("TripId is required.");
        }

        #endregion

        #region Title Validation Tests

        [Fact]
        public void Validate_ValidTitle_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1: Visit Tokyo"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleWithExactly200Characters_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = new string('A', 200)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleExceeds200Characters_FailsValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = new string('A', 201)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must be at most 200 characters.");
        }

        [Fact]
        public void Validate_NullTitle_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_EmptyTitle_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = string.Empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_WhitespaceTitle_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "   "
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        #endregion

        #region DayDate Validation Tests

        [Fact]
        public void Validate_ValidDayDate_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(1)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_NullDayDate_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_PastDayDate_PassesValidation()
        {
            // Note: The validator doesn't check if the date is in the past
            // This is handled by the handler logic
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(-1)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        [Fact]
        public void Validate_FutureDayDate_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(10)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.DayDate);
        }

        #endregion

        #region Complete Validation Tests

        [Fact]
        public void Validate_CompleteValidCommand_PassesAllValidations()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1: Explore the City",
                DayDate = DateTime.UtcNow.AddDays(5)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MinimalValidCommand_PassesAllValidations()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_CommandWithAllOptionalFields_PassesValidation()
        {
            var command = new AddTripDayCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Day 1",
                DayDate = DateTime.UtcNow.AddDays(1)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
