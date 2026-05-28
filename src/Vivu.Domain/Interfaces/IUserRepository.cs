using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetByIdWithRolesTrackedAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<User> GetAllQuery();

    // Statistics
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<int> GetNewUsersCountAsync(DateTime since, CancellationToken ct = default);
}