using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.TripMembers.Queries.GetTripMembers;
using Xunit;

namespace Vivu.Application.Tests.UseCases.TripMembers.Queries.GetTripMembers
{
    public class GetTripMembersQueryValidatorTests
    {
        private readonly GetTripMembersQueryValidator _validator;

        public GetTripMembersQueryValidatorTests()
        {
            _validator = new GetTripMembersQueryValidator();
        }

        #region Valid Input Tests

        [Fact]
        public void Validate_ValidQuery_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.NewGuid(),
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidQueryWithCustomPagination_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.NewGuid(),
                PageNumber = 5,
                PageSize = 20
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region TripId Validation Tests

        [Fact]
        public void Validate_EmptyTripId_ShouldHaveError()
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.Empty,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("Trip ID is required");
        }

        [Fact]
        public void Validate_ValidGuidTripId_ShouldNotHaveError()
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.NewGuid(),
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_MinimalValidQuery_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(1, 50)]
        [InlineData(5, 20)]
        [InlineData(100, 5)]
        public void Validate_DifferentPaginationValues_ShouldNotHaveErrors(int pageNumber, int pageSize)
        {
            // Arrange
            var query = new GetTripMembersQuery
            {
                TripId = Guid.NewGuid(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
