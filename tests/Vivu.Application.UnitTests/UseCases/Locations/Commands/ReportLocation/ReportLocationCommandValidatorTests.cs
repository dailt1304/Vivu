using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Locations.Commands.ReportLocation;
using Vivu.Domain.Enums;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Locations.Commands.ReportLocation
{
    public class ReportLocationCommandValidatorTests
    {
        private readonly ReportLocationCommandValidator _validator;

        public ReportLocationCommandValidatorTests()
        {
            _validator = new ReportLocationCommandValidator();
        }

        #region LocationId Validation

        [Fact]
        public void Validate_ValidLocationId_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.LocationId);
        }

        [Fact]
        public void Validate_EmptyLocationId_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.Empty,
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId)
                .WithErrorMessage("Location ID is required");
        }

        #endregion

        #region ReportType Validation

        [Theory]
        [InlineData(ReportType.WRONG_INFO)]
        [InlineData(ReportType.CLOSED)]
        public void Validate_ValidReportTypes_PassesValidation(ReportType reportType)
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = reportType,
                Reason = "Test reason"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.ReportType);
        }

        [Fact]
        public void Validate_InvalidReportType_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = (ReportType)999,
                Reason = "Test reason"
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ReportType)
                .WithErrorMessage("Invalid report type. Valid values are: WRONG_INFO, CLOSED");
        }

        #endregion

        #region Reason Validation

        [Fact]
        public void Validate_ValidReason_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "The address information is incorrect"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Validate_EmptyReason_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = ""
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Reason)
                .WithErrorMessage("Reason is required");
        }

        [Fact]
        public void Validate_ReasonAtMaxLength_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = new string('A', 500)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void Validate_ReasonExceedsMaxLength_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = new string('A', 501)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Reason)
                .WithErrorMessage("Reason must not exceed 500 characters");
        }

        #endregion

        #region Description Validation

        [Fact]
        public void Validate_NullDescription_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason",
                Description = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionAtMaxLength_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason",
                Description = new string('A', 2000)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Validate_DescriptionExceedsMaxLength_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "Test reason",
                Description = new string('A', 2001)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 2000 characters");
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_CompleteValidCommand_PassesValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.NewGuid(),
                ReportType = ReportType.WRONG_INFO,
                Reason = "The address is incorrect",
                Description = "The actual address should be 123 Main Street"
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleValidationErrors_FailsValidation()
        {
            var command = new ReportLocationCommand
            {
                LocationId = Guid.Empty,
                ReportType = (ReportType)999,
                Reason = "",
                Description = new string('A', 2001)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.LocationId);
            result.ShouldHaveValidationErrorFor(x => x.ReportType);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        #endregion
    }
}
