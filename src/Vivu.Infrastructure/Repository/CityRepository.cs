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
    public class CityRepository : GenericRepository<City>, ICityRepository
    {
        public CityRepository(VivuDbContext context) : base(context) { }

        public override async Task<City?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Country)
                .Include(c => c.Locations)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public IQueryable<City> GetSearchQuery(string searchText)
        {
            var normalizedSearch = searchText.ToLower();
            return _dbSet
                .AsNoTracking()
                .Where(c => c.Name.ToLower().Contains(normalizedSearch) ||
                            (c.NameAscii != null && c.NameAscii.ToLower().Contains(normalizedSearch)))
                .Include(c => c.Country)
                .OrderBy(c => c.Name);
        }

        public IQueryable<City> GetByCountryIdQuery(Guid countryId)
        {
            return _dbSet
                .AsNoTracking()
                .Where(c => c.CountryId == countryId)
                .Include(c => c.Country)
                .OrderBy(c => c.Name);
        }

        public IQueryable<City> GetAllQuery()
        {
            return _dbSet
                .AsNoTracking()
                .OrderBy(c => c.Name);
        }
    }
}
