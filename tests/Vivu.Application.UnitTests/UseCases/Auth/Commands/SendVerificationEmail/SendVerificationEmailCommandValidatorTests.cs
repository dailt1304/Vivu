using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail;

namespace Vivu.Application.UnitTests.UseCases.Auth.Commands.SendVerificationEmail
{
    public class SendVerificationEmailCommandValidatorTests
    {
        private readonly SendVerificationEmailValidator _validator;

        public SendVerificationEmailCommandValidatorTests()
        {
            _validator = new SendVerificationEmailValidator();
        }

        #region Email Validation - Valid Cases

        [Fact]
        public void Validate_ValidEmail_PassesValidation()
        {
            var command = new SendVerificationEmailCommand("test@example.com");

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("user.name@domain.com")]
        [InlineData("user+tag@domain.co.uk")]
        [InlineData("user123@subdomain.domain.com")]
        [InlineData("test.email.with+symbol@example4u.net")]
        public void Validate_VariousValidEmailFormats_PassValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        #endregion

        #region Email Validation - Empty/Null Cases

        [Fact]
        public void Validate_EmptyEmail_FailsValidation()
        {
             
            var command = new SendVerificationEmailCommand("");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_NullEmail_FailsValidation()
        {
            var command = new SendVerificationEmailCommand(null);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        [Fact]
        public void Validate_WhitespaceEmail_FailsValidation()
        {
            var command = new SendVerificationEmailCommand("   ");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email is required");
        }

        #endregion

        #region Email Validation - Invalid Format Cases

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@domain.com")]
        [InlineData("user@")]
        [InlineData("user domain@example.com")]
        [InlineData("user@domain")]
        [InlineData("user@@domain.com")]
        [InlineData("user@domain..com")]
        [InlineData("user@.com")]
        [InlineData(".user@domain.com")]
        [InlineData("user.@domain.com")]
        public void Validate_InvalidEmailFormats_FailValidation(string invalidEmail)
        {
            var command = new SendVerificationEmailCommand(invalidEmail);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format");
        }

        #endregion

        #region Email Validation - Special Cases

        [Fact]
        public void Validate_EmailWithLeadingSpaces_FailsValidation()
        {
            var command = new SendVerificationEmailCommand("  test@example.com");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmailWithTrailingSpaces_FailsValidation()
        {
            var command = new SendVerificationEmailCommand("test@example.com  ");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("test@example.com")]
        [InlineData("TEST@EXAMPLE.COM")]
        [InlineData("Test@Example.Com")]
        [InlineData("TeSt@ExAmPlE.cOm")]
        public void Validate_EmailWithDifferentCasing_PassesValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_VeryLongValidEmail_PassesValidation()
        {
            var localPart = new string('a', 64); 
            var domain = new string('b', 63) + ".com"; 
            var longEmail = $"{localPart}@{domain}";
            var command = new SendVerificationEmailCommand(longEmail);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Validate_EmailExceedingMaxLength_FailsValidation()
        {
            var localPart = new string('a', 100);
            var domain = new string('b', 160) + ".com";
            var tooLongEmail = $"{localPart}@{domain}"; 
            var command = new SendVerificationEmailCommand(tooLongEmail);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Email must not exceed 254 characters");
        }

        #endregion

        #region Email Validation - Special Characters

        [Theory]
        [InlineData("user+filter@example.com")]
        [InlineData("user.name@example.com")]
        [InlineData("user_name@example.com")]
        [InlineData("user-name@example.com")]
        [InlineData("123@example.com")]
        public void Validate_EmailWithAllowedSpecialCharacters_PassesValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("user name@example.com")] 
        public void Validate_EmailWithDisallowedSpecialCharacters_FailsValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email)
                .WithErrorMessage("Invalid email format");
        }

        #endregion

        #region Email Validation - Domain Rules

        [Theory]
        [InlineData("user@gmail.com")]
        [InlineData("user@yahoo.com")]
        [InlineData("user@outlook.com")]
        [InlineData("user@hotmail.com")]
        [InlineData("user@company.co.uk")]
        [InlineData("user@sub.domain.example.com")]
        public void Validate_CommonEmailProviders_PassValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Theory]
        [InlineData("user@domain")] 
        [InlineData("user@.domain.com")] 
        [InlineData("user@domain..com")] 
        [InlineData("user@domain.c")] 
        public void Validate_InvalidDomainFormats_FailValidation(string email)
        {
            var command = new SendVerificationEmailCommand(email);

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        #endregion

        #region Comprehensive Validation Tests

        [Fact]
        public void Validate_AllValidationRulesApplied_ForInvalidInput()
        {
            var command = new SendVerificationEmailCommand("");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void Validate_MultipleValidationErrors_ShowsFirstError()
        {
            var command = new SendVerificationEmailCommand("notanemail");

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Email);
            result.Errors.Should().HaveCount(3); 
        }

        #endregion
    }
}
