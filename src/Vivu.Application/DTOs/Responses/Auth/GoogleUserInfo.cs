using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.Auth
{
    public class GoogleUserInfo
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? Name { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Picture { get; set; }
    }
}
