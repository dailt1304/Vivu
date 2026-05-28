using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.VerifyResetOtp
{
    public class VerifyResetOtpCommandValidatorTests
    {
        private readonly VerifyResetOtpCommandValidator _validator;

        public VerifyResetOtpCommandValidatorTests()
        {
            _validator = new VerifyResetOtpCommandValidator();
        }

        #region Valid Cases

        [Fact]
        public void Validate_ValidOtpCommand_PassesValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("000000")]
        [InlineData("999999")]
        [InlineData("123456")]
        [InlineData("654321")]
        public void Validate_VariousValidOtpCodes_PassesValidation(string code)
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", code);

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
            var command = new VerifyResetOtpCommand("", "123456");

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
            var command = new VerifyResetOtpCommand("notanemail", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
        }

        [Fact]
        public void Validate_WhitespaceEmail_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("   ", "123456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        #endregion

        #region OTP Code Validation

        [Fact]
        public void Validate_EmptyCode_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code is required.");
        }

        [Fact]
        public void Validate_CodeTooShort_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "12345");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must be 6 digits.");
        }

        [Fact]
        public void Validate_CodeTooLong_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "1234567");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must be 6 digits.");
        }

        [Fact]
        public void Validate_CodeWithNonDigits_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "12345a");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must contain only digits.");
        }

        [Fact]
        public void Validate_CodeWithSpecialCharacters_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "123@56");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must contain only digits.");
        }

        [Fact]
        public void Validate_CodeWithSpaces_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("test@example.com", "12 456");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must contain only digits.");
        }

        #endregion

        #region Multiple Validation Errors

        [Fact]
        public void Validate_BothFieldsEmpty_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("", "");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void Validate_InvalidEmailAndInvalidCode_FailsValidation()
        {
            // Arrange
            var command = new VerifyResetOtpCommand("invalidemail", "abc123");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
            result.ShouldHaveValidationErrorFor(x => x.Code)
                .WithErrorMessage("OTP code must contain only digits.");
        }

        #endregion
    }
}
