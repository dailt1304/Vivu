using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vivu.Application.DTOs.Responses.Auth;
using Vivu.Application.UseCases.Auth.Commands.ForgotPassword;
using Vivu.Application.UseCases.Auth.Commands.LoginUser;
using Vivu.Application.UseCases.Auth.Commands.RefreshToken;
using Vivu.Application.UseCases.Auth.Commands.RegisterUser;
using Vivu.Application.UseCases.Auth.Commands.ResetPassword;
using Vivu.Application.UseCases.Auth.Commands.SendVerificationEmail;
using Vivu.Application.UseCases.Auth.Commands.VerifyResetOtp;
using Vivu.Application.UseCases.GoogleLogin;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Vivu.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ApiControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginUserCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            command.DeviceType = Request.Headers["User-Agent"].ToString().Contains("Mobile") ? "Mobile" : "Desktop";
            //command.DeviceName = Request.Headers["User-Agent"].ToString();

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("send-verification-email")]
        public async Task<IActionResult> SendVerificationEmail(
        [FromBody] SendVerificationEmailCommand command)
        {
            var result = await Mediator.Send(command);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new
            {
                success = true,
                message = "Verification email has been sent. Please check your inbox."
            });
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            command.DeviceType = Request.Headers["User-Agent"].ToString().Contains("Mobile") ? "Mobile" : "Desktop";
            //command.DeviceName = Request.Headers["User-Agent"].ToString();

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous] 
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            var result = await Mediator.Send(command);
            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new
            {
                success = true,
                message = "If an account exists with this email, you will receive a password reset code shortly."
            });
        }
        [HttpPost("verify-reset-otp")]
        public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpCommand command)
        {
            var result = await Mediator.Send(command);

            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new
            {
                success = true,
                message = "OTP verified successfully. You can now reset your password."
            });
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult>  RefreshToken([FromBody] RefreshTokenCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            command.DeviceType = Request.Headers["User-Agent"].ToString().Contains("Mobile") ? "Mobile" : "Desktop";
            //command.DeviceName = Request.Headers["User-Agent"].ToString();
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            var result = await Mediator.Send(command);
            if (result.IsFailure)
            {
                return HandleFailure(result);
            }

            return Ok(new
            {
                success = true,
                message = "Your password has been reset successfully. You can now log in with your new password."
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token." });
            }

            var result = await Mediator.Send(new Vivu.Application.UseCases.Users.Queries.GetUserById.GetUserByIdQuery 
            { 
                UserId = userId 
            });

            if (!result.IsSuccess)
            {
                return NotFound(new { success = false, message = "User not found." });
            }

            var user = result.Value;

            if (user.Status == "banned")
            {
                return StatusCode(403, new 
                { 
                    success = false, 
                    message = "User.Banned",
                    detail = "Tài khoản của bạn đã bị khóa."
                });
            }
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();

            return Ok(new { success = true, data = new { 
                user.Id,
                user.FullName,
                user.Phone,
                user.AvatarUrl,
                user.Bio,
                user.DateOfBirth,
                user.Gender,
                user.CountryId,
                user.Email,
                user.Status,
                user.EmailConfirmed,
                user.CreatedAt,
                roles
            }});
        }
    }
}
