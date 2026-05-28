using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Vivu.Application.Common.Models
{
    public class PaginatedList<T>
    {
        public List<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages { get; }
        public bool HasPreviousPage { get; }
        public bool HasNextPage { get; }

        public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = count;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            HasPreviousPage = pageNumber > 1;
            HasNextPage = pageNumber < TotalPages;
        }

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var count = await source.CountAsync(cancellationToken);
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }


        public static PaginatedList<T> Create(
            List<T> items,
            int totalCount,
            int pageNumber,
            int pageSize)
        {
            return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
        }


        public PaginatedList<TDestination> Map<TDestination>(
            Func<T, TDestination> mapper)
        {
            var mappedItems = Items.Select(mapper).ToList();
            return new PaginatedList<TDestination>(
                mappedItems,
                TotalCount,
                PageNumber,
                PageSize);
        }
    }
}
