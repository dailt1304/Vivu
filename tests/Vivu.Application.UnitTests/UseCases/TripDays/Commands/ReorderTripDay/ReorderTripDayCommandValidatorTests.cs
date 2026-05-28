using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripDay.Commands.ReorderTripDay;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.TripDays.Commands.ReorderTripDay
{
    public class ReorderTripDayCommandValidatorTests
    {
        private readonly ReorderTripDayValidator _validator;

        public ReorderTripDayCommandValidatorTests()
        {
            _validator = new ReorderTripDayValidator();
        }

        #region TripId Validation Tests

        [Fact]
        public void Validate_ValidTripId_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        [Fact]
        public void Validate_EmptyTripId_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.Empty,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("TripId is required");
        }

        [Fact]
        public void Validate_DefaultGuidTripId_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = default,
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("TripId is required");
        }

        #endregion

        #region OrderedTripDayIds Validation Tests

        [Fact]
        public void Validate_ValidOrderedTripDayIds_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.OrderedTripDayIds);
        }

        [Fact]
        public void Validate_EmptyOrderedTripDayIds_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripDayIds)
                .WithErrorMessage("OrderedTripDayIds must not be empty.");
        }

        [Fact]
        public void Validate_OrderedTripDayIdsWithEmptyGuid_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid(), Guid.Empty, Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("OrderedTripDayIds[1]")
                .WithErrorMessage("TripDayId must not be empty.");
        }

        [Fact]
        public void Validate_OrderedTripDayIdsWithMultipleEmptyGuids_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.Empty, Guid.NewGuid(), Guid.Empty }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("OrderedTripDayIds[0]");
            result.ShouldHaveValidationErrorFor("OrderedTripDayIds[2]");
        }

        [Fact]
        public void Validate_OrderedTripDayIdsWithDuplicates_FailsValidation()
        {
            // Arrange
            var duplicateId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid(), duplicateId, Guid.NewGuid(), duplicateId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripDayIds)
                .WithErrorMessage("OrderedTripDayIds contains duplicate ids.");
        }

        [Fact]
        public void Validate_OrderedTripDayIdsWithAllDuplicates_FailsValidation()
        {
            // Arrange
            var duplicateId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { duplicateId, duplicateId, duplicateId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripDayIds)
                .WithErrorMessage("OrderedTripDayIds contains duplicate ids.");
        }

        [Fact]
        public void Validate_SingleTripDayId_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> { Guid.NewGuid() }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.OrderedTripDayIds);
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_ValidCommand_PassesValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = new List<Guid> 
                { 
                    Guid.NewGuid(), 
                    Guid.NewGuid(), 
                    Guid.NewGuid() 
                }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_InvalidTripIdAndEmptyList_FailsValidation()
        {
            // Arrange
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.Empty,
                OrderedTripDayIds = new List<Guid>()
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        }

        [Fact]
        public void Validate_InvalidTripIdAndDuplicateIds_FailsValidation()
        {
            // Arrange
            var duplicateId = Guid.NewGuid();
            var command = new ReorderTripDayCommand
            {
                TripId = Guid.Empty,
                OrderedTripDayIds = new List<Guid> { duplicateId, duplicateId }
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(x => x.TripId);
            result.ShouldHaveValidationErrorFor(x => x.OrderedTripDayIds);
        }

        [Fact]
        public void Validate_LargeListOfUniqueIds_PassesValidation()
        {
            // Arrange
            var orderedIds = new List<Guid>();
            for (int i = 0; i < 100; i++)
            {
                orderedIds.Add(Guid.NewGuid());
            }

            var command = new ReorderTripDayCommand
            {
                TripId = Guid.NewGuid(),
                OrderedTripDayIds = orderedIds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion
    }
}
