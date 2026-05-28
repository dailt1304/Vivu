using Vivu.Domain.Enums;
using Vivu.Domain.Errors;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities;

public class Otp : Entity<Guid>
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public OtpType Type { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime ExpiredAt { get; set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public int VerifyAttempts { get; private set; }

    public virtual User? User { get; set; }

    private Otp() { }

    public static Otp Create(string email, OtpType type, Guid? userId = null)
    {
        return new Otp
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email.ToLower().Trim(),
            Code = GenerateCode(),
            Type = type,
            IsUsed = false,
            IsVerified = false,
            VerifyAttempts = 0,
            ExpiredAt = DateTime.UtcNow.AddMinutes(10), 
            CreatedDate = DateTime.UtcNow
        };
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiredAt;

    public bool IsValid(string code) => !IsUsed && !IsExpired() && Code == code;

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
    public bool CanVerify() => !IsUsed && !IsExpired() && VerifyAttempts < 5;

    public bool IsValidCode(string code) => Code == code.Trim();

    public Result VerifyCode(string code)
    {
        if (IsUsed)
            return Result.Failure(DomainErrors.Auth.OtpAlreadyUsed);

        if (IsExpired())
            return Result.Failure(DomainErrors.Auth.OtpExpired);

        if (VerifyAttempts >= 5)
            return Result.Failure(DomainErrors.Auth.TooManyOtpVerifyAttempts);

        VerifyAttempts++;

        if (!IsValidCode(code))
            return Result.Failure(DomainErrors.Auth.OtpInvalid);

        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        return Result.Success();
    }
    private static string GenerateCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
