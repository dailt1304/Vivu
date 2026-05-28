using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.LoginUser
{
    public class LoginUserCommandValidatorTests
    {
        private readonly LoginUserCommandValidator _validator;

        public LoginUserCommandValidatorTests()
        {
            _validator = new LoginUserCommandValidator();
        }

        #region Email Validation Tests

        [Fact]
        public void Validate_ValidEmail_PassesValidation()
        {
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("user@domain.com")]
        [InlineData("user.name@domain.com")]
        [InlineData("user+tag@domain.co.uk")]
        [InlineData("user123@subdomain.domain.com")]
        [InlineData("a@b.c")]
        public void Validate_ValidEmailFormats_PassesValidation(string email)
        {
            var command = new LoginUserCommand
            {
                Email = email,
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmptyEmail_FailsValidation()
        {
            var command = new LoginUserCommand
            {
                Email = "",
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_NullEmail_FailsValidation()
        {
            var command = new LoginUserCommand
            {
                Email = null,
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_WhitespaceEmail_FailsValidation()
        {
            var command = new LoginUserCommand
            {
                Email = "   ",
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@domain.com")]
        [InlineData("user@")]
        [InlineData("user domain@example.com")]
        [InlineData("user@domain")]
        [InlineData("user@@domain.com")]
        [InlineData("user@domain..com")]
        public void Validate_InvalidEmailFormats_FailsValidation(string invalidEmail)
        {
            var command = new LoginUserCommand
            {
                Email = invalidEmail,
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format");
        }

        #endregion

        #region Password Validation Tests

        [Fact]
        public void Validate_ValidPassword_PassesValidation()
        {
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Theory]
        [InlineData("a")]
        [InlineData("12345")]
        [InlineData("password")]
        [InlineData("P@ssw0rd123!@#")]
        public void Validate_NonEmptyPassword_PassesValidation(string password)
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = password
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_EmptyPassword_FailsValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = ""
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        [Fact]
        public void Validate_NullPassword_FailsValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = null
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        [Fact]
        public void Validate_WhitespacePassword_FailsValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "   "
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        #endregion

        #region Optional Fields Validation Tests

        [Fact]
        public void Validate_WithoutIpAddress_PassesValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                IpAddress = null
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveValidationErrorFor(x => x.IpAddress);
        }

        [Fact]
        public void Validate_WithoutDeviceType_PassesValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                DeviceType = null
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveValidationErrorFor(x => x.DeviceType);
        }

        [Fact]
        public void Validate_WithoutDeviceName_PassesValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                DeviceName = null
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveValidationErrorFor(x => x.DeviceName);
        }

        [Fact]
        public void Validate_WithAllOptionalFields_PassesValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "test@example.com",
                Password = "Password123!",
                IpAddress = "192.168.1.1",
                DeviceType = "Mobile",
                DeviceName = "iPhone 14"
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Combined Validation Tests

        [Fact]
        public void Validate_ValidCommand_PassesAllValidations()
        {
            
            var command = new LoginUserCommand
            {
                Email = "user@example.com",
                Password = "SecureP@ssw0rd!",
                IpAddress = "192.168.1.100",
                DeviceType = "Desktop",
                DeviceName = "Windows PC"
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_InvalidEmailAndPassword_FailsBothValidations()
        {
            
            var command = new LoginUserCommand
            {
                Email = "notanemail",
                Password = ""
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
            result.Errors.Should().HaveCount(3);
        }

        [Fact]
        public void Validate_AllFieldsNull_FailsRequiredValidations()
        {
            
            var command = new LoginUserCommand
            {
                Email = null,
                Password = null
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_EmailWithMaxLength_PassesValidation()
        {
            var localPart = new string('a', 64);
            var domain = new string('b', 63) + ".com";
            var longEmail = $"{localPart}@{domain}";

            var command = new LoginUserCommand
            {
                Email = longEmail,
                Password = "Password123!"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmailWithUnicodeCharacters_FailsValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "tést@éxample.com",
                Password = "Password123!"
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_CaseSensitiveEmail_PassesValidation()
        {
            
            var command = new LoginUserCommand
            {
                Email = "Test@Example.COM",
                Password = "Password123!"
            };

            
            var result = _validator.TestValidate(command);

            
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        #endregion
    }
}
