using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Domain.Entities;

namespace Vivu.Domain.Interfaces
{
    public interface ICityRepository : IGenericRepository<City>
    { 
        IQueryable<City> GetSearchQuery(string searchText);
        IQueryable<City> GetByCountryIdQuery(Guid countryId);
        IQueryable<City> GetAllQuery();
    }
}
