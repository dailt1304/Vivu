using FluentValidation.TestHelper;
using Vivu.Application.UseCases.Trips.Commands.UpdateMemberRole;
using Vivu.Domain.Enums;
using Xunit;

namespace Vivu.Application.UnitTests.UseCases.Trips.Commands.UpdateMemberRole
{
    public class UpdateMemberRoleCommandValidatorTests
    {
        private readonly UpdateMemberRoleCommandValidator _validator;

        public UpdateMemberRoleCommandValidatorTests()
        {
            _validator = new UpdateMemberRoleCommandValidator();
        }

        #region TripId Validation

        [Fact]
        public void Validate_EmptyTripId_FailsValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.Empty,
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId)
                .WithErrorMessage("TripId is required.");
        }

        [Fact]
        public void Validate_ValidTripId_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.TripId);
        }

        #endregion

        #region MemberUserId Validation

        [Fact]
        public void Validate_EmptyMemberUserId_FailsValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.Empty,
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MemberUserId)
                .WithErrorMessage("MemberUserId is required.");
        }

        [Fact]
        public void Validate_ValidMemberUserId_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.MemberUserId);
        }

        #endregion

        #region NewRole Validation

        [Fact]
        public void Validate_ValidNewRole_Owner_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Owner
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewRole);
        }

        [Fact]
        public void Validate_ValidNewRole_Editor_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewRole);
        }

        [Fact]
        public void Validate_ValidNewRole_Viewer_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Viewer
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.NewRole);
        }

        [Fact]
        public void Validate_InvalidNewRole_FailsValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = (TripRole)999 // Invalid enum value
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.NewRole)
                .WithErrorMessage("NewRole must be a valid TripRole enum value.");
        }

        #endregion

        #region Complete Valid Command

        [Fact]
        public void Validate_CompleteValidCommand_PassesValidation()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.NewGuid(),
                MemberUserId = Guid.NewGuid(),
                NewRole = TripRole.Editor
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

        #region Multiple Errors

        [Fact]
        public void Validate_AllFieldsInvalid_FailsWithMultipleErrors()
        {
            // Arrange
            var command = new UpdateMemberRoleCommand
            {
                TripId = Guid.Empty,
                MemberUserId = Guid.Empty,
                NewRole = (TripRole)999
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TripId);
            result.ShouldHaveValidationErrorFor(x => x.MemberUserId);
            result.ShouldHaveValidationErrorFor(x => x.NewRole);
        }

        #endregion
    }
}
