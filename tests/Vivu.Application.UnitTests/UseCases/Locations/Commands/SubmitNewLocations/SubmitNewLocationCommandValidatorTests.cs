using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Vivu.Application.UseCases.Locations.Commands.SubmitNewLocation;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.SubmitNewLocations
{
    public class SubmitNewLocationCommandValidatorTests
    {
        private readonly SubmitNewLocationCommandValidator _validator;

        public SubmitNewLocationCommandValidatorTests()
        {
            _validator = new SubmitNewLocationCommandValidator();
        }

        #region Name Validation

        [Fact]
        public void Validate_ValidName_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Location Name",
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_EmptyName_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = string.Empty,
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_NullName_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = null!,
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_NameExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = new string('a', 151),
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_NameAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = new string('a', 150),
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Validate_NullDescription_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Description = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Description = new string('a', 2001),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Description = new string('a', 2000),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region Address Validation

        [Fact]
        public void Validate_ValidAddress_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void Validate_AddressExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = new string('a', 301),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void Validate_AddressAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = new string('a', 300),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        #endregion

        #region Address and Coordinates Validation

        [Fact]
        public void Validate_WithAddressOnly_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = null,
                Longitude = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_WithCoordinatesOnly_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = null,
                Latitude = 10.762622,
                Longitude = 106.660172,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_NoAddressAndNoCoordinates_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = null,
                Latitude = null,
                Longitude = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Provide either Address, or both Latitude and Longitude.");
        }

        [Fact]
        public void Validate_EmptyAddressAndNoCoordinates_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "   ",
                Latitude = null,
                Longitude = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Provide either Address, or both Latitude and Longitude.");
        }

        [Fact]
        public void Validate_LatitudeWithoutLongitude_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = 10.762622,
                Longitude = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Latitude and Longitude must be provided together.");
        }

        [Fact]
        public void Validate_LongitudeWithoutLatitude_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = null,
                Longitude = 106.660172,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x)
                .WithErrorMessage("Latitude and Longitude must be provided together.");
        }

        #endregion

        #region Latitude Validation

        [Theory]
        [InlineData(-90)]
        [InlineData(0)]
        [InlineData(45.5)]
        [InlineData(90)]
        public void Validate_ValidLatitude_PassesValidation(double latitude)
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = latitude,
                Longitude = 100,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(-90.1)]
        [InlineData(90.1)]
        [InlineData(91)]
        [InlineData(180)]
        public void Validate_LatitudeOutOfRange_FailsValidation(double latitude)
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = latitude,
                Longitude = 100,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Latitude!.Value);
        }

        #endregion

        #region Longitude Validation

        [Theory]
        [InlineData(-180)]
        [InlineData(0)]
        [InlineData(106.660172)]
        [InlineData(180)]
        public void Validate_ValidLongitude_PassesValidation(double longitude)
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = 10,
                Longitude = longitude,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(-180.1)]
        [InlineData(180.1)]
        [InlineData(181)]
        [InlineData(360)]
        public void Validate_LongitudeOutOfRange_FailsValidation(double longitude)
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Latitude = 10,
                Longitude = longitude,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Longitude!.Value);
        }

        #endregion

        #region Phone Validation

        [Fact]
        public void Validate_NullPhone_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Phone = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_ValidPhone_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Phone = "0123456789",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_PhoneExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Phone = new string('1', 31),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_PhoneAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Phone = new string('1', 30),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        #endregion

        #region Website Validation

        [Fact]
        public void Validate_NullWebsite_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Website = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Validate_ValidWebsite_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Website = "https://www.example.com",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Validate_WebsiteExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Website = new string('a', 301),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Validate_WebsiteAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Website = new string('a', 300),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        #endregion

        #region Tags Validation

        [Fact]
        public void Validate_NullTags_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Tags = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Tags);
        }

        [Fact]
        public void Validate_ValidTags_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Tags = "restaurant,cafe,food",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Tags);
        }

        [Fact]
        public void Validate_TagsExceedsMaxLength_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Tags = new string('a', 501),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Tags);
        }

        [Fact]
        public void Validate_TagsAtMaxLength_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Tags = new string('a', 500),
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Tags);
        }

        #endregion

        #region Images Validation

        [Fact]
        public void Validate_WithValidImages_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(1024),
                    CreateMockFormFile(2048)
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Images);
        }

        [Fact]
        public void Validate_NullImages_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = null!
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Images)
                .WithErrorMessage("Images is required");
        }

        [Fact]
        public void Validate_EmptyImages_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>()
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Images)
                .WithErrorMessage("At least one image is required");
        }

        [Fact]
        public void Validate_ImageExceeds5MB_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(6 * 1024 * 1024) // 6MB
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Images[0]")
                .WithErrorMessage("Each image must be smaller than 5MB");
        }

        [Fact]
        public void Validate_ImageAt5MB_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(5 * 1024 * 1024) // Exactly 5MB
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor("Images[0]");
        }

        [Fact]
        public void Validate_MultipleImagesWithOneExceeding5MB_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(1024), // Valid
                    CreateMockFormFile(6 * 1024 * 1024), // Invalid - 6MB
                    CreateMockFormFile(2048) // Valid
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Images[1]");
        }

        [Fact]
        public void Validate_NullImageInList_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Valid Name",
                Address = "123 Main Street",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(1024),
                    null!
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor("Images[1]");
        }

        #endregion

        #region Multiple Fields Validation

        [Fact]
        public void Validate_AllFieldsValid_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Beautiful Beach Resort",
                Description = "A wonderful beach resort with amazing views",
                Address = "123 Beach Road, Coastal City",
                Latitude = 10.762622,
                Longitude = 106.660172,
                CityId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                OpeningHours = "9:00 AM - 10:00 PM",
                Phone = "0123456789",
                Website = "https://www.example.com",
                Tags = "beach,resort,hotel",
                Images = new List<IFormFile>
                {
                    CreateMockFormFile(1024),
                    CreateMockFormFile(2048)
                }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleFieldsInvalid_FailsValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = string.Empty, // Invalid
                Description = new string('a', 2001), // Invalid
                Address = new string('a', 301), // Invalid
                Phone = new string('1', 31), // Invalid
                Images = new List<IFormFile>() // Invalid - empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Description);
            result.ShouldHaveValidationErrorFor(x => x.Address);
            result.ShouldHaveValidationErrorFor(x => x.Phone);
            result.ShouldHaveValidationErrorFor(x => x.Images);
        }

        [Fact]
        public void Validate_MinimalValidCommand_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Simple Location",
                Address = "123 Street",
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_OptionalFieldsNull_PassesValidation()
        {
            var command = new SubmitNewLocationCommand
            {
                Name = "Simple Location",
                Address = "123 Street",
                Description = null,
                CityId = null,
                CategoryId = null,
                OpeningHours = null,
                Phone = null,
                Website = null,
                Tags = null,
                Images = new List<IFormFile> { CreateMockFormFile(1024) }
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Helper Methods

        private static IFormFile CreateMockFormFile(long size)
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.Length).Returns(size);
            mock.Setup(f => f.FileName).Returns("test.jpg");
            mock.Setup(f => f.ContentType).Returns("image/jpeg");
            return mock.Object;
        }

        #endregion
    }
}
