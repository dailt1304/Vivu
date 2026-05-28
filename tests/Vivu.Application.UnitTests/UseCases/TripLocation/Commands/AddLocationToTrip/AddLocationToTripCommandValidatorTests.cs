using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripLocation.Commands.AddLocationToTrip;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.AddLocationToTrip
{
    public class AddLocationToTripCommandValidatorTests
    {
        private readonly AddLocationToTripValidator _validator;

        public AddLocationToTripCommandValidatorTests()
        {
            _validator = new AddLocationToTripValidator();
        }

        #region TripDayId Validation Tests

        [Fact]
        public void Validate_ValidTripDayId_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.TripDayId);
        }

        [Fact]
        public void Validate_EmptyTripDayId_FailsValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.Empty,
                LocationId = Guid.NewGuid(),
                OrderIndex = 0
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required");
        }

        #endregion

        #region LocationId Validation Tests

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
        }

        [Fact]
        public void Validate_EmptyLocationId_FailsValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.Empty,
                OrderIndex = 0
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("LocationId is required");
        }

        #endregion

        #region OrderIndex Validation Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void Validate_ValidOrderIndex_PassesValidation(int orderIndex)
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = orderIndex
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.OrderIndex);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-5)]
        [InlineData(-100)]
        public void Validate_NegativeOrderIndex_FailsValidation(int orderIndex)
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = orderIndex
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.OrderIndex)
                .WithErrorMessage("OrderIndex must be greater than or equal to 0");
        }

        #endregion

        #region Time Validation Tests

        [Fact]
        public void Validate_StartTimeBeforeEndTime_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
        }

        [Fact]
        public void Validate_StartTimeAfterEndTime_FailsValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = new TimeSpan(17, 0, 0),
                EndTime = new TimeSpan(9, 0, 0)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.StartTime)
                .WithErrorMessage("StartTime must be less than EndTime");
        }

        [Fact]
        public void Validate_StartTimeEqualToEndTime_FailsValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(9, 0, 0)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.StartTime)
                .WithErrorMessage("StartTime must be less than EndTime");
        }

        [Fact]
        public void Validate_OnlyStartTimeProvided_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
        }

        [Fact]
        public void Validate_OnlyEndTimeProvided_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = null,
                EndTime = new TimeSpan(17, 0, 0)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
        }

        [Fact]
        public void Validate_BothTimesNull_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                StartTime = null,
                EndTime = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
            result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
        }

        #endregion

        #region Optional Fields Validation Tests

        [Fact]
        public void Validate_WithNote_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                Note = "This is a test note"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithTransportMode_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                TransportMode = "Car"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NullOptionalFields_PassesValidation()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 0,
                Note = null,
                TransportMode = null,
                StartTime = null,
                EndTime = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Complete Validation Tests

        [Fact]
        public void Validate_CompleteValidCommand_PassesAllValidations()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.NewGuid(),
                LocationId = Guid.NewGuid(),
                OrderIndex = 1,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Note = "Visit the museum",
                TransportMode = "Walking"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleValidationErrors_ReturnsAllErrors()
        {
            var command = new AddLocationToTripCommand
            {
                TripDayId = Guid.Empty,
                LocationId = Guid.Empty,
                OrderIndex = -1,
                StartTime = new TimeSpan(17, 0, 0),
                EndTime = new TimeSpan(9, 0, 0)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.TripDayId);
            result.ShouldHaveValidationErrorFor(x => x.LocationId);
            result.ShouldHaveValidationErrorFor(x => x.OrderIndex);
            result.ShouldHaveValidationErrorFor(x => x.StartTime);
        }

        #endregion
    }
}
