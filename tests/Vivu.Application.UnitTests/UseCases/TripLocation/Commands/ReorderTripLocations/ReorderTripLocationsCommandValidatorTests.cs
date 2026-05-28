using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripLocation.Commands.ReorderTripLocations;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripLocation.Commands.ReorderTripLocations
{
    public class ReorderTripLocationsCommandValidatorTests
    {
        private readonly ReorderTripLocationValidator _validator;

        public ReorderTripLocationsCommandValidatorTests()
        {
            _validator = new ReorderTripLocationValidator();
        }

        #region TripDayId Validation Tests

        [Fact]
        public void Validate_ValidTripDayId_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
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
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.Empty,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required.");
        }

        [Fact]
        public void Validate_DefaultGuidTripDayId_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = default,
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required.");
        }

        #endregion

        #region OrderedTripLocationIds Validation Tests

        [Fact]
        public void Validate_ValidOrderedTripLocationIds_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.OrderedTripLocationIds);
        }

        [Fact]
        public void Validate_EmptyOrderedTripLocationIds_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds)
                .WithErrorMessage("OrderedTripLocationIds must not be empty.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithEmptyGuid_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid(), Guid.Empty, Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("OrderedTripLocationIds[1]")
                .WithErrorMessage("TripLocationId must not be empty.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithDefaultGuid_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid(), default, Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("OrderedTripLocationIds[1]")
                .WithErrorMessage("TripLocationId must not be empty.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithAllEmptyGuids_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.Empty, Guid.Empty }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("OrderedTripLocationIds[0]")
                .WithErrorMessage("TripLocationId must not be empty.");
            result.ShouldHaveValidationErrorFor("OrderedTripLocationIds[1]")
                .WithErrorMessage("TripLocationId must not be empty.");
        }

        #endregion

        #region Duplicate Validation Tests

        [Fact]
        public void Validate_OrderedTripLocationIdsWithDuplicates_FailsValidation()
        {
            // Arrange
            var duplicateId = Guid.NewGuid();
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid(), duplicateId, duplicateId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds)
                .WithErrorMessage("OrderedTripLocationIds contains duplicate ids.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithMultipleDuplicates_FailsValidation()
        {
            // Arrange
            var duplicate1 = Guid.NewGuid();
            var duplicate2 = Guid.NewGuid();
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { duplicate1, duplicate2, duplicate1, duplicate2 }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds)
                .WithErrorMessage("OrderedTripLocationIds contains duplicate ids.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithAllSameId_FailsValidation()
        {
            // Arrange
            var sameId = Guid.NewGuid();
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { sameId, sameId, sameId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds)
                .WithErrorMessage("OrderedTripLocationIds contains duplicate ids.");
        }

        [Fact]
        public void Validate_OrderedTripLocationIdsWithNoDuplicates_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> 
                { 
                    Guid.NewGuid(), 
                    Guid.NewGuid(), 
                    Guid.NewGuid(),
                    Guid.NewGuid()
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.OrderedTripLocationIds);
        }

        #endregion

        #region Single Location Validation Tests

        [Fact]
        public void Validate_OrderedTripLocationIdsWithSingleLocation_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Multiple Fields Validation Tests

        [Fact]
        public void Validate_ValidCommandWithMultipleLocations_PassesAllValidations()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.NewGuid(),
                OrderedTripLocationIds = new List<Guid> 
                { 
                    Guid.NewGuid(), 
                    Guid.NewGuid(), 
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid()
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_InvalidCommandWithAllErrors_FailsAllValidations()
        {
            // Arrange
            var duplicateId = Guid.NewGuid();
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.Empty,
                OrderedTripLocationIds = new List<Guid> { Guid.Empty, duplicateId, duplicateId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId);
            result.ShouldHaveValidationErrorFor("OrderedTripLocationIds[0]");
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds);
        }

        [Fact]
        public void Validate_CommandWithEmptyTripDayIdAndEmptyList_FailsMultipleValidations()
        {
            // Arrange
            var command = new ReorderTripLocationsCommand
            {
                TripDayId = Guid.Empty,
                OrderedTripLocationIds = new List<Guid>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripDayId)
                .WithErrorMessage("TripDayId is required.");
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripLocationIds)
                .WithErrorMessage("OrderedTripLocationIds must not be empty.");
        }

        #endregion
    }
}
