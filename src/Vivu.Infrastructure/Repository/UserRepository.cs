using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;
using Vivu.Domain.Interfaces;
using Vivu.Infrastructure.Data;

namespace Vivu.Infrastructure.Repository
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(VivuDbContext context) : base(context)
        {
        }

        public override async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbSet.AsNoTracking()
                .Include(u => u.UserProfile)
                .Include(u => u.UserSettings)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking()
                .Include(u => u.UserProfile)
                .Include(u => u.UserSettings)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _dbSet.AsNoTracking()
                .Include(u => u.UserProfile)
                .Include(u => u.UserSettings)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
        }
        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _dbSet
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User?> GetByIdWithRolesTrackedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public IQueryable<User> GetAllQuery()
        {
            return _dbSet.AsNoTracking()
                .Include(u => u.UserProfile)
                .Include(u => u.UserSettings)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);
        }

        // Statistics
        public async Task<int> GetTotalCountAsync(CancellationToken ct = default)
            => await _dbSet.CountAsync(ct);

        public async Task<int> GetNewUsersCountAsync(DateTime since, CancellationToken ct = default)
            => await _dbSet.CountAsync(u => u.CreatedDate >= since, ct);
    }
}