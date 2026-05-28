using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vivu.Domain.Interfaces;

namespace Vivu.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly VivuDbContext _context;

        public UnitOfWork(VivuDbContext context)
        {
            _context = context;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }
    }
}
