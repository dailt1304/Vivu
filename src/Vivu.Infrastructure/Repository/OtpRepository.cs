using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class OtpRepository : GenericRepository<Otp>, IOtpRepository
    {
        private readonly VivuDbContext _context;

        public OtpRepository(VivuDbContext context) : base(context) { }

        public async Task<Otp?> GetValidOtpAsync(string email, string code, OtpType type)
        {
            var normalizedEmail = email.ToLower().Trim();
            var normalizedCode = code.Trim();
            return await _dbSet
                .Where(o => o.Email == normalizedEmail
                    && o.Code == normalizedCode
                    && o.Type == type
                    && !o.IsUsed
                    && o.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountRecentOtpRequestsAsync(string email, OtpType type, int minutes)
        {
            var timeThreshold = DateTime.UtcNow.AddMinutes(-minutes);
            var normalizedEmail = email.ToLower().Trim();
            return await _dbSet
                .Where(o => o.Email == normalizedEmail
                    && o.Type == type
                    && o.CreatedDate >= timeThreshold)
                .CountAsync();
        }
        public async Task<Otp?> GetVerifiedOtpAsync(string email, OtpType type)
        {
            var normalizedEmail = email.ToLower().Trim();

            return await _dbSet
                .Where(o => o.Email == normalizedEmail
                    && o.Type == type
                    && o.IsVerified
                    && !o.IsUsed
                    && o.ExpiredAt > DateTime.UtcNow)
                .OrderByDescending(o => o.VerifiedAt)
                .FirstOrDefaultAsync();
        }
    }
}
