using FluentValidation.TestHelper;
using Vivu.Application.UseCases.LocationReports.Commands.ReviewLocationReport;
using Vivu.Domain.Enums;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.LocationReports.Commands.ReviewLocationReport
{
    public class ReviewLocationReportCommandValidatorTests
    {
        private readonly ReviewLocationReportCommandValidator _validator;

        public ReviewLocationReportCommandValidatorTests()
        {
            _validator = new ReviewLocationReportCommandValidator();
        }

        #region ReportId Validation

        [Fact]
        public void Validate_ValidReportId_PassesValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.ReportId);
        }

        [Fact]
        public void Validate_EmptyReportId_FailsValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.Empty,
                Status = ReportStatus.APPROVED
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ReportId)
                .WithErrorMessage("Report ID is required");
        }

        #endregion

        #region Status Validation

        [Theory]
        [InlineData(ReportStatus.APPROVED)]
        [InlineData(ReportStatus.REJECTED)]
        public void Validate_ValidStatuses_PassesValidation(ReportStatus status)
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = status
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Status);
        }

        [Fact]
        public void Validate_PendingStatus_FailsValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.PENDING
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Cannot set status back to PENDING");
        }

        [Fact]
        public void Validate_InvalidEnumStatus_FailsValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = (ReportStatus)999
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Status)
                .WithErrorMessage("Invalid report status");
        }

        #endregion

        #region AdminNote Validation

        [Fact]
        public void Validate_NullAdminNote_PassesValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED,
                AdminNote = null
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_AdminNoteAtMaxLength_PassesValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED,
                AdminNote = new string('A', 1000)
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.AdminNote);
        }

        [Fact]
        public void Validate_AdminNoteExceedsMaxLength_FailsValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED,
                AdminNote = new string('A', 1001)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.AdminNote)
                .WithErrorMessage("Admin note cannot exceed 1000 characters");
        }

        #endregion

        #region Complete Command Validation

        [Fact]
        public void Validate_CompleteValidCommand_PassesValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.NewGuid(),
                Status = ReportStatus.APPROVED,
                AdminNote = "Report verified and approved."
            };

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_MultipleValidationErrors_FailsValidation()
        {
            var command = new ReviewLocationReportCommand
            {
                ReportId = Guid.Empty,
                Status = ReportStatus.PENDING,
                AdminNote = new string('A', 1001)
            };

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.ReportId);
            result.ShouldHaveValidationErrorFor(x => x.Status);
            result.ShouldHaveValidationErrorFor(x => x.AdminNote);
        }

        #endregion
    }
}
