using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.ForgotPassword;
using Xunit;

namespace Vivu.Application.Tests.UseCases.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidatorTests
    {
        private readonly ForgotPasswordCommandValidator _validator;

        public ForgotPasswordCommandValidatorTests()
        {
            _validator = new ForgotPasswordCommandValidator();
        }

        #region Valid Cases

        [Fact]
        public void Validate_ValidEmail_PassesValidation()
        {
            // Arrange
            var command = new ForgotPasswordCommand("test@example.com");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("user@domain.com")]
        [InlineData("test.user@example.co.uk")]
        [InlineData("first+last@example.com")]
        [InlineData("123@example.com")]
        public void Validate_VariousValidEmails_PassesValidation(string email)
        {
            // Arrange
            var command = new ForgotPasswordCommand(email);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Invalid Cases

        [Fact]
        public void Validate_EmptyEmail_FailsValidation()
        {
            // Arrange
            var command = new ForgotPasswordCommand("");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        [Fact]
        public void Validate_NullEmail_FailsValidation()
        {
            // Arrange
            var command = new ForgotPasswordCommand(null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_WhitespaceEmail_FailsValidation()
        {
            // Arrange
            var command = new ForgotPasswordCommand("   ");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required.");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        public void Validate_InvalidEmailFormat_FailsValidation(string email)
        {
            // Arrange
            var command = new ForgotPasswordCommand(email);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format.");
        }

        [Fact]
        public void Validate_EmailExceedsMaximumLength_FailsValidation()
        {
            // Arrange
            var longEmail = new string('a', 250) + "@example.com"; // Exceeds 255
            var command = new ForgotPasswordCommand(longEmail);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        #endregion
    }
}
