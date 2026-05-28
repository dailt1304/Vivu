using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.ResetPassword;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidatorTests
    {
        private readonly ResetPasswordCommandValidator _validator;

        public ResetPasswordCommandValidatorTests()
        {
            _validator = new ResetPasswordCommandValidator();
        }

        #region Valid Cases

        [Fact]
        public void Validate_ValidResetPasswordCommand_PassesValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "ValidPassword@123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("ValidPassword@123")]
        [InlineData("MyPassword!456")]
        [InlineData("ComplexP@ss99")]
        [InlineData("Test@Password1")]
        public void Validate_VariousValidPasswords_PassesValidation(string password)
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", password);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Email Validation

        [Fact]
        public void Validate_EmptyEmail_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("", "ValidPassword@123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void Validate_InvalidEmailFormat_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("notanemail", "ValidPassword@123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
        }

        #endregion

        #region Password Validation - Length

        [Fact]
        public void Validate_EmptyPassword_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password is required.");
        }

        [Fact]
        public void Validate_PasswordTooShort_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "Pass@1"); // 6 characters

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must be at least 8 characters.");
        }

        #endregion

        #region Password Validation - Uppercase

        [Fact]
        public void Validate_PasswordWithoutUppercase_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "lowercase@123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must contain at least one uppercase letter.");
        }

        #endregion

        #region Password Validation - Lowercase

        [Fact]
        public void Validate_PasswordWithoutLowercase_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "UPPERCASE@123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must contain at least one lowercase letter.");
        }

        #endregion

        #region Password Validation - Number

        [Fact]
        public void Validate_PasswordWithoutNumber_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NoNumbers@");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must contain at least one number.");
        }

        #endregion

        #region Password Validation - Special Character

        [Fact]
        public void Validate_PasswordWithoutSpecialCharacter_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "NoSpecial123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must contain at least one special character.");
        }

        #endregion

        #region Multiple Validation Errors

        [Fact]
        public void Validate_EmptyBothEmailAndPassword_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Fact]
        public void Validate_WeakPasswordMultipleIssues_FailsValidation()
        {
            // Arrange
            var command = new ResetPasswordCommand("test@example.com", "weak");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewPassword)
                .WithErrorMessage("Password must be at least 8 characters.");
        }

        #endregion
    }
}
