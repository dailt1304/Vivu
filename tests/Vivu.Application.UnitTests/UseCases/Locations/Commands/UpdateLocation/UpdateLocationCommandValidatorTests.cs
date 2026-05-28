using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Commands.UpdateLocation;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.UpdateLocation
{
    public class UpdateLocationCommandValidatorTests
    {
        private readonly UpdateLocationCommandValidator _validator;

        public UpdateLocationCommandValidatorTests()
        {
            _validator = new UpdateLocationCommandValidator();
        }

        #region LocationId Validation

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = "Test Location"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
        }

        [Fact]
        public void Validate_EmptyLocationId_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.Empty,
                Name = "Test Location"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("Location ID is required.");
        }

        #endregion

        #region Name Validation

        [Fact]
        public void Validate_ValidName_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = "Valid Location Name"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_NullName_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_EmptyName_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = string.Empty
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Validate_NameExceedsMaxLength_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = new string('a', 201)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name must not exceed 200 characters.");
        }

        [Fact]
        public void Validate_NameAtMaxLength_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = new string('a', 200)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Validate_ValidDescription_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Description = "This is a valid description with reasonable length"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_NullDescription_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Description = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionExceedsMaxLength_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Description = new string('a', 2001)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 2000 characters.");
        }

        [Fact]
        public void Validate_DescriptionAtMaxLength_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Description = new string('a', 2000)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        #endregion

        #region Address Validation

        [Fact]
        public void Validate_ValidAddress_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Address = "123 Main Street, City"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void Validate_NullAddress_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Address = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        [Fact]
        public void Validate_AddressExceedsMaxLength_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Address = new string('a', 501)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Address)
                .WithErrorMessage("Address must not exceed 500 characters.");
        }

        [Fact]
        public void Validate_AddressAtMaxLength_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Address = new string('a', 500)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Address);
        }

        #endregion

        #region Phone Validation

        [Fact]
        public void Validate_ValidPhone_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Phone = "0123456789"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_NullPhone_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Phone = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_PhoneExceedsMaxLength_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Phone = new string('1', 21)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Phone)
                .WithErrorMessage("Phone must not exceed 20 characters.");
        }

        [Fact]
        public void Validate_PhoneAtMaxLength_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Phone = new string('1', 20)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        #endregion

        #region Website Validation

        [Fact]
        public void Validate_ValidWebsite_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Website = "https://www.example.com"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Validate_NullWebsite_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Website = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        [Fact]
        public void Validate_WebsiteExceedsMaxLength_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Website = "https://" + new string('a', 494) // Total 501 chars
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Website)
                .WithErrorMessage("Website must not exceed 500 characters.");
        }

        [Fact]
        public void Validate_WebsiteAtMaxLength_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Website = new string('a', 500)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Website);
        }

        #endregion

        #region Multiple Fields Validation

        [Fact]
        public void Validate_AllFieldsValid_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = "Test Location",
                Description = "Test Description",
                Address = "123 Test St",
                Phone = "0123456789",
                Website = "https://test.com",
                Latitude = 10.762622,
                Longitude = 106.660172,
                CategoryId = Guid.NewGuid(),
                OpeningHours = "9:00-22:00",
                Tags = "restaurant,cafe",
                Images = "image1.jpg,image2.jpg",
                IsVerified = true
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleFieldsInvalid_FailsValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.Empty,
                Name = new string('a', 201),
                Description = new string('b', 2001),
                Address = new string('c', 501),
                Phone = new string('1', 21),
                Website = new string('w', 501)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.Description);
            result.ShouldHaveValidationErrorFor(x => x.Address);
            result.ShouldHaveValidationErrorFor(x => x.Phone);
            result.ShouldHaveValidationErrorFor(x => x.Website);
        }

        #endregion

        #region Optional Fields

        [Fact]
        public void Validate_OnlyRequiredField_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid()
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_OptionalFieldsNull_PassesValidation()
        {
            var command = new UpdateLocationCommand
            {
                LocationId = Guid.NewGuid(),
                Name = null,
                Description = null,
                Address = null,
                Latitude = null,
                Longitude = null,
                CategoryId = null,
                OpeningHours = null,
                Phone = null,
                Website = null,
                Tags = null,
                Images = null,
                IsVerified = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
