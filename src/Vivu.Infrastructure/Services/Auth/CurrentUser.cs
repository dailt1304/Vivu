using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Vivu.Application.Interfaces.Auth;

namespace Vivu.Infrastructure.Services.Auth
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? Id => _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value
                             ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                             ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        public string? TraceId => _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}
