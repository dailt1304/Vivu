using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(VivuDbContext context) : base(context) { }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.RoleName == roleName.ToUpper());
        }
    }
}
