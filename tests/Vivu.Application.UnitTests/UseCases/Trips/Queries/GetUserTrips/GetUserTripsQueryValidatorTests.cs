using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Queries.GetUserTrips;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Queries.GetUserTrips
{
    public class GetUserTripsQueryValidatorTests
    {
        private readonly GetUserTripsQueryValidator _validator;

        public GetUserTripsQueryValidatorTests()
        {
            _validator = new GetUserTripsQueryValidator();
        }

        #region UserId Validation Tests

        [Fact]
        public void Validate_ValidUserId_PassesValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Fact]
        public void Validate_EmptyUserId_FailsValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.Empty,
                Status = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId)
                .WithErrorMessage("UserId is required.");
        }

        [Fact]
        public void Validate_DefaultUserId_FailsValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = default,
                Status = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserId);
        }

        #endregion

        #region Status Validation Tests - Valid Statuses

        [Theory]
        [InlineData("planning")]
        [InlineData("ongoing")]
        [InlineData("completed")]
        public void Validate_ValidStatus_PassesValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("PLANNING")]
        [InlineData("ONGOING")]
        [InlineData("COMPLETED")]
        [InlineData("Planning")]
        [InlineData("Ongoing")]
        [InlineData("Completed")]
        public void Validate_ValidStatusDifferentCase_PassesValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_NullStatus_PassesValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_EmptyStatus_PassesValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = ""
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_WhitespaceStatus_PassesValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = "   "
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        #endregion

        #region Status Validation Tests - Invalid Statuses

        [Theory]
        [InlineData("invalid")]
        [InlineData("pending")]
        [InlineData("cancelled")]
        [InlineData("draft")]
        [InlineData("active")]
        [InlineData("finished")]
        [InlineData("in-progress")]
        public void Validate_InvalidStatus_FailsValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Status must be one of: planning, ongoing, completed.");
        }

        [Theory]
        [InlineData("plan")]
        [InlineData("planninggg")]
        [InlineData("complete")]
        [InlineData("on-going")]
        public void Validate_MisspelledStatus_FailsValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
        }

        [Theory]
        [InlineData("planning ")]
        [InlineData(" planning")]
        [InlineData(" planning ")]
        public void Validate_StatusWithSpaces_FailsValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_ValidQueryWithStatus_PassesAllValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = "completed",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_ValidQueryWithoutStatus_PassesAllValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = null,
                PageNumber = 1,
                PageSize = 20
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EmptyUserIdAndInvalidStatus_FailsBothValidations()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.Empty,
                Status = "invalid-status"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ShouldHaveValidationErrorFor(x => x.UserId);
            result.ShouldHaveValidationErrorFor(x => x.Status);
            result.Errors.Should().HaveCount(2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_NewGuidUserId_PassesValidation()
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = "planning"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("abc")]
        [InlineData("!@#$%")]
        [InlineData("planning123")]
        public void Validate_NumericOrSpecialCharStatus_FailsValidation(string status)
        {
            // Arrange
            var query = new GetUserTripsQuery
            {
                UserId = Guid.NewGuid(),
                Status = status
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Status);
        }

        #endregion
    }
}
