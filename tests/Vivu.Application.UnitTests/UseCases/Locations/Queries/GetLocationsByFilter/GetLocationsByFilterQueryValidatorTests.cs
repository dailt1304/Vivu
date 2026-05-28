using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Queries.GetLocationsWithFilters;

namespace Vivu.Application.UnitTests.UseCases.Locations.Queries.GetLocationsByFilter
{
    public class GetLocationsByFilterQueryValidatorTests
    {
        private readonly GetLocationsByFilterQueryValidator _validator;

        public GetLocationsByFilterQueryValidatorTests()
        {
            _validator = new GetLocationsByFilterQueryValidator();
        }

        #region CityId Validation Tests

        [Fact]
        public void Validate_WhenCityIdIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CityId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CityId);
        }

        [Fact]
        public void Validate_WhenCityIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CityId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CityId)
                .WithErrorMessage("City ID must be a valid GUID.");
        }

        [Fact]
        public void Validate_WhenCityIdIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CityId = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CityId);
        }

        #endregion

        #region CategoryId Validation Tests

        [Fact]
        public void Validate_WhenCategoryIdIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CategoryId = Guid.NewGuid()
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        }

        [Fact]
        public void Validate_WhenCategoryIdIsEmpty_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CategoryId = Guid.Empty
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CategoryId)
                .WithErrorMessage("Category ID must be a valid GUID.");
        }

        [Fact]
        public void Validate_WhenCategoryIdIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CategoryId = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CategoryId);
        }

        #endregion

        #region MinRating Validation Tests

        [Fact]
        public void Validate_WhenMinRatingIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 4.5m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        [Fact]
        public void Validate_WhenMinRatingIsZero_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 0m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        [Fact]
        public void Validate_WhenMinRatingIsFive_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 5m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        [Fact]
        public void Validate_WhenMinRatingIsNegative_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = -1m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MinRating)
                .WithErrorMessage("Minimum rating must be at least 0.");
        }

        [Fact]
        public void Validate_WhenMinRatingExceedsFive_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 6m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MinRating)
                .WithErrorMessage("Minimum rating cannot exceed 5.");
        }

        [Fact]
        public void Validate_WhenMinRatingIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        #endregion

        #region UserLatitude Validation Tests

        [Fact]
        public void Validate_WhenLatitudeIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLatitude);
        }

        [Fact]
        public void Validate_WhenLatitudeIsMinusBoundary_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = -90,
                UserLongitude = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLatitude);
        }

        [Fact]
        public void Validate_WhenLatitudeIsPlusBoundary_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 90,
                UserLongitude = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLatitude);
        }

        [Fact]
        public void Validate_WhenLatitudeIsLessThanMinusBoundary_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = -91,
                UserLongitude = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserLatitude)
                .WithErrorMessage("Latitude must be between -90 and 90.");
        }

        [Fact]
        public void Validate_WhenLatitudeIsGreaterThanPlusBoundary_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 91,
                UserLongitude = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserLatitude)
                .WithErrorMessage("Latitude must be between -90 and 90.");
        }

        [Fact]
        public void Validate_WhenLatitudeIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLatitude);
        }

        #endregion

        #region UserLongitude Validation Tests

        [Fact]
        public void Validate_WhenLongitudeIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLongitude);
        }

        [Fact]
        public void Validate_WhenLongitudeIsMinusBoundary_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 0,
                UserLongitude = -180
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLongitude);
        }

        [Fact]
        public void Validate_WhenLongitudeIsPlusBoundary_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 0,
                UserLongitude = 180
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLongitude);
        }

        [Fact]
        public void Validate_WhenLongitudeIsLessThanMinusBoundary_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 0,
                UserLongitude = -181
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserLongitude)
                .WithErrorMessage("Longitude must be between -180 and 180.");
        }

        [Fact]
        public void Validate_WhenLongitudeIsGreaterThanPlusBoundary_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 0,
                UserLongitude = 181
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UserLongitude)
                .WithErrorMessage("Longitude must be between -180 and 180.");
        }

        [Fact]
        public void Validate_WhenLongitudeIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UserLongitude);
        }

        #endregion

        #region Coordinates Pairing Validation Tests

        [Fact]
        public void Validate_WhenBothLatitudeAndLongitudeProvided_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_WhenNeitherLatitudeNorLongitudeProvided_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = null,
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_WhenOnlyLatitudeProvided_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Both latitude and longitude must be provided together.");
        }

        [Fact]
        public void Validate_WhenOnlyLongitudeProvided_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = null,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Both latitude and longitude must be provided together.");
        }

        #endregion

        #region RadiusInMeters Validation Tests

        [Fact]
        public void Validate_WhenRadiusIsValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 5000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.RadiusInMeters);
        }

        [Fact]
        public void Validate_WhenRadiusIsMaxBoundary_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 50000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.RadiusInMeters);
        }

        [Fact]
        public void Validate_WhenRadiusIsZero_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RadiusInMeters)
                .WithErrorMessage("Radius must be greater than 0.");
        }

        [Fact]
        public void Validate_WhenRadiusIsNegative_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = -100
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RadiusInMeters)
                .WithErrorMessage("Radius must be greater than 0.");
        }

        [Fact]
        public void Validate_WhenRadiusExceedsMaxBoundary_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 50001
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.RadiusInMeters)
                .WithErrorMessage("Radius cannot exceed 50,000 meters (50 km).");
        }

        [Fact]
        public void Validate_WhenRadiusIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                RadiusInMeters = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.RadiusInMeters);
        }

        #endregion

        #region Radius Requires Coordinates Validation Tests

        [Fact]
        public void Validate_WhenRadiusProvidedWithCoordinates_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 5000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_WhenRadiusProvidedWithoutCoordinates_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = null,
                UserLongitude = null,
                RadiusInMeters = 5000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when using radius filter.");
        }

        [Fact]
        public void Validate_WhenRadiusProvidedWithOnlyLatitude_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = null,
                RadiusInMeters = 5000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when using radius filter.");
        }

        [Fact]
        public void Validate_WhenRadiusProvidedWithOnlyLongitude_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = null,
                UserLongitude = 105.8542,
                RadiusInMeters = 5000
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when using radius filter.");
        }

        #endregion

        #region SortBy Validation Tests

        [Fact]
        public void Validate_WhenSortByIsRating_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "rating"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsDistance_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = 21.0285,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsRecent_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "recent"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsName_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "name"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsCaseInsensitive_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "RATING"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsInvalid_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "invalid_sort"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SortBy)
                .WithErrorMessage("Sort by must be one of: 'rating', 'distance', 'recent', 'name'.");
        }

        [Fact]
        public void Validate_WhenSortByIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsEmptyString_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = ""
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        [Fact]
        public void Validate_WhenSortByIsWhitespace_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "   "
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortBy);
        }

        #endregion

        #region Distance Sort Requires Coordinates Validation Tests

        [Fact]
        public void Validate_WhenSortByDistanceWithCoordinates_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = 21.0285,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x);
        }

        [Fact]
        public void Validate_WhenSortByDistanceWithoutCoordinates_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = null,
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when sorting by distance.");
        }

        [Fact]
        public void Validate_WhenSortByDistanceCaseInsensitiveWithoutCoordinates_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "DISTANCE",
                UserLatitude = null,
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when sorting by distance.");
        }

        [Fact]
        public void Validate_WhenSortByDistanceWithOnlyLatitude_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = 21.0285,
                UserLongitude = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when sorting by distance.");
        }

        [Fact]
        public void Validate_WhenSortByDistanceWithOnlyLongitude_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortBy = "distance",
                UserLatitude = null,
                UserLongitude = 105.8542
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("User coordinates (latitude and longitude) are required when sorting by distance.");
        }

        #endregion

        #region SortColumn Validation Tests

        [Fact]
        public void Validate_WhenSortColumnIsName_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "Name"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsRatingAverage_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "RatingAverage"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsCreatedAt_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "CreatedAt"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsRatingCount_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "RatingCount"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsCaseInsensitive_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "name"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsInvalid_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "InvalidColumn"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SortColumn)
                .WithErrorMessage("Sort column must be one of: 'Name', 'RatingAverage', 'CreatedAt', 'RatingCount'.");
        }

        [Fact]
        public void Validate_WhenSortColumnIsNull_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = null
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsEmptyString_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = ""
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        [Fact]
        public void Validate_WhenSortColumnIsWhitespace_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                SortColumn = "   "
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortColumn);
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_WithAllValidParameters_ShouldNotHaveAnyValidationErrors()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CityId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                MinRating = 4.5m,
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 5000,
                SortBy = "distance",
                SortColumn = "RatingAverage",
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
        public void Validate_WithMultipleErrors_ShouldHaveMultipleValidationErrors()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                CityId = Guid.Empty,
                CategoryId = Guid.Empty,
                MinRating = -1,
                UserLatitude = 100,
                UserLongitude = 200,
                RadiusInMeters = -100,
                SortBy = "invalid",
                SortColumn = "InvalidColumn"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterThan(5);
        }

        [Fact]
        public void Validate_WithMinimalValidParameters_ShouldNotHaveValidationErrors()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_WhenMultipleCoordinateValidationsFail_ShouldHaveMultipleErrors()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = null,
                RadiusInMeters = 5000,
                SortBy = "distance"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.IsValid.Should().BeFalse();
            // Should have errors for:
            // 1. Latitude/Longitude must be provided together
            // 2. Radius requires coordinates
            // 3. Distance sort requires coordinates
            result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
        }

        [Fact]
        public void Validate_WhenRadiusIsVeryLargeButValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                UserLatitude = 21.0285,
                UserLongitude = 105.8542,
                RadiusInMeters = 49999.99
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.RadiusInMeters);
        }

        [Fact]
        public void Validate_WhenMinRatingIsVerySmallButValid_ShouldNotHaveValidationError()
        {
            // Arrange
            var query = new GetLocationsByFilterQuery
            {
                MinRating = 0.01m
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MinRating);
        }

        #endregion
    }
}
