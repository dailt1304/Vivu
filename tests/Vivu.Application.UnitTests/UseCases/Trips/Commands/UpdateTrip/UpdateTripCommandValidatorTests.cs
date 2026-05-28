using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Commands.UpdateTrip;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateTrip
{
    public class UpdateTripCommandValidatorTests
    {
        private readonly UpdateTripCommandValidator _validator;

        public UpdateTripCommandValidatorTests()
        {
            _validator = new UpdateTripCommandValidator();
        }

        #region TripId Validation

        [Fact]
        public void Validate_EmptyTripId_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.Empty,
                Title = "Test Trip",
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("Trip ID is required.");
        }

        [Fact]
        public void Validate_ValidTripId_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region Title Validation

        [Fact]
        public void Validate_EmptyTitle_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = string.Empty,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void Validate_NullTitle_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = null!,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleTooLong_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = new string('a', 201), // 201 characters
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 200 characters.");
        }

        [Fact]
        public void Validate_TitleExactly200Characters_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = new string('a', 200),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_ValidTitle_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Valid Trip Title",
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Validate_DescriptionTooLong_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Description = new string('a', 2001),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 2000 characters.");
        }

        [Fact]
        public void Validate_DescriptionExactly2000Characters_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Description = new string('a', 2000),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_NullDescription_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Description = null,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_EmptyDescription_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Description = string.Empty,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region Status Validation

        [Fact]
        public void Validate_EmptyStatus_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status is required.");
        }

        [Fact]
        public void Validate_NullStatus_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = null!
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        [InlineData("completed")]
        public void Validate_ValidStatus_PassesValidation(string status)
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = status
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("PLANNING")]
        [InlineData("Ongoing")]
        [InlineData("COMPLETED")]
        [InlineData("PlAnNiNg")]
        public void Validate_StatusCaseInsensitive_PassesValidation(string status)
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = status
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("pending")]
        [InlineData("cancelled")]
        [InlineData("active")]
        public void Validate_InvalidStatus_FailsValidation(string status)
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                Status = status
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status must be one of: planning, ongoing, completed.");
        }

        #endregion

        #region Date Validation

        [Fact]
        public void Validate_StartDateAfterEndDate_FailsValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Start date must be earlier than end date.");
        }

        [Fact]
        public void Validate_StartDateEqualsEndDate_FailsValidation()
        {
            // Arrange
            var sameDate = DateTime.UtcNow.AddDays(5);
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = sameDate,
                EndDate = sameDate,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Start date must be earlier than end date.");
        }

        [Fact]
        public void Validate_ValidDateRange_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(10),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_NullDates_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = null,
                EndDate = null,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_OnlyStartDateProvided_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = null,
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_OnlyEndDateProvided_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Test Trip",
                StartDate = null,
                EndDate = DateTime.UtcNow.AddDays(10),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_CompleteValidCommand_PassesValidation()
        {
            // Arrange
            var command = new UpdateTripCommand
            {
                TripId = Guid.NewGuid(),
                Title = "Amazing Trip to Paris",
                Description = "A wonderful journey through the city of lights",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(15),
                Status = "planning",
                CoverUrl = "https://example.com/cover.jpg",
                TripSize = 4
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
