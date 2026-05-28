using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Vivu.Application.UseCases.Trips.Commands.CreateTrip;
using Vivu.Domain.Interfaces;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Trips.Commands.CreateTrip
{
    public class CreateTripCommandValidatorTests
    {
        private readonly CreateTripCommandValidator _validator;
        private readonly Mock<ICityRepository> _cityRepositoryMock;

        public CreateTripCommandValidatorTests()
        {
            _cityRepositoryMock = new Mock<ICityRepository>();
            _validator = new CreateTripCommandValidator(_cityRepositoryMock.Object);
        }

        #region Valid Input Tests

        [Fact]
        public void Validate_ValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "My Trip to Paris",
                Description = "A wonderful trip to Paris",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(14),
                TripSize = 5,
                IsPublic = false,
                GenerateInviteCode = true
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MinimalValidCommand_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "My Trip"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_TitleWithExactMaxLength_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = new string('A', 200) // Exactly 200 characters
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_DescriptionWithExactMaxLength_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = new string('A', 2000) // Exactly 2000 characters
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_NullDescription_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_EmptyDescription_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region Title Validation Tests

        [Fact]
        public void Validate_EmptyTitle_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Fact]
        public void Validate_NullTitle_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = null!
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Fact]
        public void Validate_WhitespaceTitle_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "   "
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_TitleExceedsMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = new string('A', 201) // 201 characters, exceeds 200
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 200 characters");
        }

        [Theory]
        [InlineData("A")] // 1 character
        [InlineData("Trip")] // Normal length
        [InlineData("This is a valid trip title with some detail")] // Medium length
        public void Validate_TitleWithinMaxLength_ShouldNotHaveError(string title)
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = title
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        #endregion

        #region Description Validation Tests

        [Fact]
        public void Validate_DescriptionExceedsMaxLength_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = new string('A', 2001) // 2001 characters, exceeds 2000
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 2000 characters");
        }

        [Theory]
        [InlineData("Short description")]
        [InlineData("A medium length description that describes the trip in more detail")]
        public void Validate_DescriptionWithinMaxLength_ShouldNotHaveError(string description)
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                Description = description
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region StartDate Validation Tests

        [Fact]
        public void Validate_StartDateInPast_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddDays(-1) // Yesterday
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .WithErrorMessage("Start date must be in the future");
        }

        [Fact]
        public void Validate_StartDateIsNow_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddSeconds(-1)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate);
        }

        [Fact]
        public void Validate_StartDateInFuture_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StartDate);
        }

        [Fact]
        public void Validate_NullStartDate_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StartDate);
        }

        #endregion

        #region EndDate Validation Tests

        [Fact]
        public void Validate_EndDateBeforeStartDate_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5) // Before start date
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                .WithErrorMessage("End date must be after start date");
        }

        [Fact]
        public void Validate_EndDateEqualStartDate_ShouldHaveError()
        {
            // Arrange
            var fixedDate = DateTime.UtcNow.AddDays(5);
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = fixedDate,
                EndDate = fixedDate // Same as start date
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public void Validate_EndDateAfterStartDate_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = DateTime.UtcNow.AddDays(10)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public void Validate_NullEndDate_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = DateTime.UtcNow.AddDays(5),
                EndDate = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
        }

        [Fact]
        public void Validate_EndDateWithNullStartDate_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                StartDate = null,
                EndDate = DateTime.UtcNow.AddDays(10)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.EndDate);
        }

        #endregion

        #region TripSize Validation Tests

        [Fact]
        public void Validate_TripSizeZero_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                TripSize = 0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripSize)
                .WithErrorMessage("Trip size must be greater than 0");
        }

        [Fact]
        public void Validate_TripSizeNegative_ShouldHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                TripSize = -1
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripSize)
                .WithErrorMessage("Trip size must be greater than 0");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void Validate_TripSizePositive_ShouldNotHaveError(int tripSize)
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                TripSize = tripSize
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripSize);
        }

        [Fact]
        public void Validate_NullTripSize_ShouldNotHaveError()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                TripSize = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripSize);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Fact]
        public void Validate_MultipleInvalidFields_ShouldHaveMultipleErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = string.Empty,
                Description = new string('A', 2001),
                StartDate = DateTime.UtcNow.AddDays(-1),
                TripSize = 0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title);
            result.ShouldHaveValidationErrorFor(x => x.Description);
            result.ShouldHaveValidationErrorFor(x => x.StartDate);
            result.ShouldHaveValidationErrorFor(x => x.TripSize);

            result.Errors.Count.Should().BeGreaterThanOrEqualTo(4);
        }

        [Fact]
        public void Validate_TitleAndDatesInvalid_ShouldHaveSpecificErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = new string('A', 201),
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5)
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 200 characters");
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                .WithErrorMessage("End date must be after start date");
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData("Trip with special chars !@#$%^&*()")]
        [InlineData("Trip với tiếng Việt")]
        [InlineData("Trip with numbers 123")]
        [InlineData("🌍 Emoji Trip 🛫")]
        public void Validate_TitleWithSpecialCharacters_ShouldNotHaveError(string title)
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = title
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Validate_BooleanFieldsDefaultValues_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Valid Title",
                IsPublic = false,
                GenerateInviteCode = false
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_AllOptionalFieldsNull_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CreateTripCommand
            {
                Title = "Required Title Only",
                Description = null,
                CoverUrl = null,
                StartDate = null,
                EndDate = null,
                TripSize = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
