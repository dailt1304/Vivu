using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Application.Common.Models;

namespace Vivu.Application.Common.Extensions
{
    public static class PaginationExtensions
    {
        private static readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = new();

        public static IQueryable<T> Paginate<T>(
            this IQueryable<T> query,
            PaginationRequest request,
            Expression<Func<T, object>>? orderBy = null, 
            bool ascending = true)
        {
            if (orderBy != null)
            {
                query = ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);
            }

            return query.Skip(request.Skip).Take(request.Take);
        }

        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var count = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }

        public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> query,
            PaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            return await query.ToPaginatedListAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);
        }

        public static IQueryable<T> OrderByDynamic<T>(
        this IQueryable<T> query,
        string? sortColumn,
        bool descending = false)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                return query;
            }

            var cacheKey = $"{typeof(T).FullName}.{sortColumn}";
            var propertyInfo = _propertyCache.GetOrAdd(cacheKey, _ =>
                typeof(T).GetProperty(
                    sortColumn,
                    BindingFlags.IgnoreCase |
                    BindingFlags.Public |
                    BindingFlags.Instance)
            );

            if (propertyInfo == null)
            {
                return query; 
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);

            var converted = Expression.Convert(property, typeof(object));
            var selector = Expression.Lambda<Func<T, object>>(converted, parameter);

            return descending
                ? query.OrderByDescending(selector)
                : query.OrderBy(selector);
        }

        public static IQueryable<T> OrderByDynamic<T>(
            this IQueryable<T> query,
            string? primarySort,
            string? secondarySort = null,
            bool descending = false)
        {
            query = query.OrderByDynamic(primarySort, descending);

            if (!string.IsNullOrWhiteSpace(secondarySort))
            {
                query = query.ThenByDynamic(secondarySort, descending);
            }

            return query;
        }


        public static IOrderedQueryable<T> ThenByDynamic<T>(
            this IQueryable<T> query,
            string? sortColumn,
            bool descending = false)
        {
            if (string.IsNullOrWhiteSpace(sortColumn) || query is not IOrderedQueryable<T> orderedQuery)
            {
                return (IOrderedQueryable<T>)query;
            }

            var cacheKey = $"{typeof(T).FullName}.{sortColumn}";
            var propertyInfo = _propertyCache.GetOrAdd(cacheKey, _ =>
                typeof(T).GetProperty(
                    sortColumn,
                    BindingFlags.IgnoreCase |
                    BindingFlags.Public |
                    BindingFlags.Instance)
            );

            if (propertyInfo == null)
            {
                return orderedQuery;
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var converted = Expression.Convert(property, typeof(object));
            var selector = Expression.Lambda<Func<T, object>>(converted, parameter);

            return descending
                ? orderedQuery.ThenByDescending(selector)
                : orderedQuery.ThenBy(selector);
        }
    }
}
