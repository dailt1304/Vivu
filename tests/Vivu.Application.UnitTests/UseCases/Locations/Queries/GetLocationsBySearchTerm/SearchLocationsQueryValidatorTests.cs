using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Queries.GetLocationBySearchText;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationsBySearchTerm
{
    public class SearchLocationsQueryValidatorTests
    {
        private readonly SearchLocationsQueryValidator _validator;

        public SearchLocationsQueryValidatorTests()
        {
            _validator = new SearchLocationsQueryValidator();
        }

        #region SearchTerm Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")] 
        public void Should_Have_Error_When_SearchTerm_Is_Null_Or_Empty(string searchTerm)
        {
            var query = new SearchLocationsQuery { SearchTerm = searchTerm };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                  .WithErrorMessage("Search term is required.");
        }

        [Fact]
        public void Should_Have_Error_When_SearchTerm_Is_Too_Short()
        {
            var query = new SearchLocationsQuery { SearchTerm = "a" };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                  .WithErrorMessage("Search term must be at least 2 characters.");
        }

        [Fact]
        public void Should_Have_Error_When_SearchTerm_Is_Too_Long()
        {
            var query = new SearchLocationsQuery { SearchTerm = new string('a', 256) };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.SearchTerm)
                  .WithErrorMessage("Search term cannot exceed 255 characters.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_SearchTerm_Is_Valid()
        {
            var query = new SearchLocationsQuery { SearchTerm = "Da Nang" };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.SearchTerm);
        }

        #endregion

        #region CityId Tests

        [Fact]
        public void Should_Have_Error_When_CityId_Is_Empty_Guid()
        {
            var query = new SearchLocationsQuery { CityId = Guid.Empty };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.CityId)
                  .WithErrorMessage("City ID must be a valid GUID.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_CityId_Is_Null()
        {
            var query = new SearchLocationsQuery { CityId = null };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.CityId);
        }

        [Fact]
        public void Should_Not_Have_Error_When_CityId_Is_Valid()
        {
            var query = new SearchLocationsQuery { CityId = Guid.NewGuid() };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.CityId);
        }

        #endregion

        #region CategoryId Tests

        [Fact]
        public void Should_Have_Error_When_CategoryId_Is_Empty_Guid()
        {
            var query = new SearchLocationsQuery { CategoryId = Guid.Empty };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.CategoryId)
                  .WithErrorMessage("Category ID must be a valid GUID.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_CategoryId_Is_Null()
        {
            var query = new SearchLocationsQuery { CategoryId = null };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        }

        [Fact]
        public void Should_Not_Have_Error_When_CategoryId_Is_Valid()
        {
            var query = new SearchLocationsQuery { CategoryId = Guid.NewGuid() };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        }

        #endregion

        #region MinRating Tests

        [Theory]
        [InlineData(-0.1)]
        [InlineData(-1)]
        [InlineData(5.1)]
        [InlineData(6)]
        public void Should_Have_Error_When_MinRating_Is_Out_Of_Range(decimal invalidRating)
        {
            var query = new SearchLocationsQuery { MinRating = invalidRating };

            var result = _validator.TestValidate(query);

            result.ShouldHaveValidationErrorFor(x => x.MinRating)
                  .WithErrorMessage("Minimum rating must be between 0 and 5.");
        }

        [Fact]
        public void Should_Not_Have_Error_When_MinRating_Is_Null()
        {
            var query = new SearchLocationsQuery { MinRating = null };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2.5)]
        [InlineData(5)]
        public void Should_Not_Have_Error_When_MinRating_Is_Valid(decimal validRating)
        {
            var query = new SearchLocationsQuery { MinRating = validRating };

            var result = _validator.TestValidate(query);

            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        #endregion
    }
}
