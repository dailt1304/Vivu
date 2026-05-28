using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.DTOs.Responses.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone {  get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public Guid? CountryId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Role { get; set; }
    }
}
