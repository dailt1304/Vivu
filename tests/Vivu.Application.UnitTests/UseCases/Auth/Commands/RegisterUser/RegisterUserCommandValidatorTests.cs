using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandValidatorTests
    {
        private readonly RegisterUserCommandValidator _validator;

        public RegisterUserCommandValidatorTests()
        {
            _validator = new RegisterUserCommandValidator();
        }

        #region Email Validation Tests

        [Fact]
        public void Validate_ValidEmail_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe"
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
        [InlineData("test.email+alex@leetcode.com")]
        public void Validate_ValidEmailFormats_PassesValidation(string email)
        {
            var command = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmptyEmail_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "",
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_NullEmail_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = null,
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_InvalidEmailFormatNoAtSymbol_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "notanemailformat",
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmailExceedsMaxLength_FailsValidation()
        {
            var longEmail = string.Concat(Enumerable.Repeat("a", 250)) + "@example.com";
            var command = new RegisterUserCommand
            {
                Email = longEmail,
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        #endregion

        #region Password Validation Tests

        [Fact]
        public void Validate_ValidPassword_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Validate_EmptyPassword_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        [Fact]
        public void Validate_NullPassword_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = null,
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password is required");
        }

        [Fact]
        public void Validate_PasswordTooShort_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "Short1!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must be at least 8 characters");
        }

        [Fact]
        public void Validate_PasswordWithoutUppercase_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "lowercasepassword123",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one uppercase letter");
        }

        [Fact]
        public void Validate_PasswordWithoutLowercase_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "UPPERCASEPASSWORD123",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one lowercase letter");
        }

        [Fact]
        public void Validate_PasswordWithoutNumbers_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "PasswordWithoutNumbers!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Password)
                .WithErrorMessage("Password must contain at least one number");
        }

        [Theory]
        [InlineData("ValidPassword123")]
        [InlineData("AnotherPassword456")]
        [InlineData("P@ssw0rd")]
        [InlineData("Secure123Password")]
        public void Validate_VariousValidPasswords_PassesValidation(string password)
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = password,
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        #endregion

        #region FullName Validation Tests

        [Fact]
        public void Validate_ValidFullName_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        }

        [Fact]
        public void Validate_EmptyFullName_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = ""
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.FullName)
                .WithErrorMessage("First name is required");
        }

        [Fact]
        public void Validate_NullFullName_FailsValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.FullName)
                .WithErrorMessage("First name is required");
        }

        [Fact]
        public void Validate_FullNameExceedsMaxLength_FailsValidation()
        {
            var longFullName = string.Concat(Enumerable.Repeat("a", 101));
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = longFullName
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [Theory]
        [InlineData("A")]
        [InlineData("John")]
        [InlineData("John Doe")]
        [InlineData("Mary Jane Watson")]
        public void Validate_VariousValidFullNames_PassesValidation(string fullName)
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = fullName
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        }

        #endregion

        #region Phone Validation Tests

        [Fact]
        public void Validate_ValidPhoneNumber_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = "+84912345678"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Theory]
        [InlineData("+84912345678")]
        [InlineData("0912345678")]
        [InlineData("+11234567890")]
        [InlineData("1234567890")]
        public void Validate_VariousValidPhoneNumbers_PassesValidation(string phone)
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = phone
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_EmptyPhoneNumber_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = ""
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Validate_NullPhoneNumber_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Theory]
        [InlineData("123")] // Too short
        [InlineData("abc")] // Invalid characters
        [InlineData("phone number")] // Contains spaces
        public void Validate_InvalidPhoneFormat_FailsValidation(string phone)
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = phone
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Phone)
                .WithErrorMessage("Invalid phone number");
        }

        #endregion

        #region Combined Field Tests

        [Fact]
        public void Validate_AllFieldsValid_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe",
                Phone = "+84912345678"
            };

            var result = _validator.TestValidate(command);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_MultipleFieldsInvalid_ReturnsMultipleErrors()
        {
            var command = new RegisterUserCommand
            {
                Email = "invalidemail",
                Password = "weak",
                FullName = "",
                Phone = "invalid"
            };

            var result = _validator.TestValidate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public void Validate_OnlyRequiredFieldsProvided_PassesValidation()
        {
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Validate_EmailWithMaxLength_PassesValidation()
        {
            var email = "user" + string.Concat(Enumerable.Repeat("a", 237)) + "@example.com";
            var command = new RegisterUserCommand
            {
                Email = email,
                Password = "SecurePassword123!",
                FullName = "John Doe"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_FullNameWithMaxLength_PassesValidation()
        {
            var fullName = string.Concat(Enumerable.Repeat("a", 100));
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "SecurePassword123!",
                FullName = fullName
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.FullName);
        }

        #endregion
    }
}
