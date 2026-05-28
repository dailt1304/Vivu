using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Shared;

namespace Vivu.Domain.Entities
{
    public class RefreshToken : Entity<Guid>
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }

        public Guid? ReplacedByTokenId { get; set; }

        public string? IpAddress { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceName { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual RefreshToken? ReplacedByToken { get; set; }

        public static RefreshToken Create(
            Guid userId,
            string token,
            DateTime expiresAt,
            string ipAddress = null,
            string deviceType = null,
            string deviceName = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token is required", nameof(token));

            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt,
                IsUsed = false,
                IsRevoked = false,
                IpAddress = ipAddress,
                DeviceType = deviceType,
                DeviceName = deviceName,
                CreatedDate = DateTime.UtcNow
            };
        }

        public void MarkAsUsed(Guid? replaceByToken = null)
        {
            IsUsed = true;
            ReplacedByTokenId = replaceByToken;
        }

        public void Revoke(string reason)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }

        public bool IsValid()
        {
            return !IsUsed && !IsRevoked && !IsExpired();
        }
    }
}
