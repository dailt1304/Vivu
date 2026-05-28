using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;

namespace Vivu.Domain.Interfaces
{
    public interface IOtpRepository : IGenericRepository<Otp>
    {
        Task<Otp?> GetValidOtpAsync(string email, string code, OtpType type);
        Task<int> CountRecentOtpRequestsAsync(string email, OtpType type, int minutes);
        Task<Otp?> GetVerifiedOtpAsync(string email, OtpType type);
    }
}
