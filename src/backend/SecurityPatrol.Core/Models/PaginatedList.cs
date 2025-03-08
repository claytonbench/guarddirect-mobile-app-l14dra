using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace SecurityPatrol.Core.Models
{
    /// <summary>
    /// A generic class that provides pagination functionality for collections of items.
    /// It includes both the items for the current page and metadata about the pagination state.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    public class PaginatedList<T>
    {
        /// <summary>
        /// Gets the collection of items for the current page.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// Gets the current page number.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the total number of pages available.
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Gets the total count of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets a value indicating whether there is a previous page available.
        /// </summary>
        public bool HasPreviousPage { get; }

        /// <summary>
        /// Gets a value indicating whether there is a next page available.
        /// </summary>
        public bool HasNextPage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class with the specified items and pagination metadata.
        /// </summary>
        /// <param name="items">The items for the current page.</param>
        /// <param name="count">The total count of items across all pages.</param>
        /// <param name="pageNumber">The current page number.</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PaginatedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
        {
            Items = items.ToList();
            PageNumber = pageNumber;
            TotalPages = (count + pageSize - 1) / pageSize;
            TotalCount = count;
            HasPreviousPage = PageNumber > 1;
            HasNextPage = PageNumber < TotalPages;
        }

        /// <summary>
        /// Creates a new <see cref="PaginatedList{TSource}"/> from an IQueryable by applying pagination parameters.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source.</typeparam>
        /// <param name="source">The source IQueryable to paginate.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation, containing the paginated list.</returns>
        /// <exception cref="ArgumentException">Thrown when pageNumber or pageSize is less than 1.</exception>
        public static async Task<PaginatedList<TSource>> CreateAsync<TSource>(
            IQueryable<TSource> source, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            var count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            
            return new PaginatedList<TSource>(items, count, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a new <see cref="PaginatedList{TSource}"/> from an IEnumerable by applying pagination parameters.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source.</typeparam>
        /// <param name="source">The source IEnumerable to paginate.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A paginated list containing the items for the specified page.</returns>
        /// <exception cref="ArgumentException">Thrown when pageNumber or pageSize is less than 1.</exception>
        public static PaginatedList<TSource> Create<TSource>(
            IEnumerable<TSource> source, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
            
            if (pageSize < 1)
                throw new ArgumentException("Page size must be greater than 0.", nameof(pageSize));

            var sourceList = source.ToList();
            var count = sourceList.Count;
            var items = sourceList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            
            return new PaginatedList<TSource>(items, count, pageNumber, pageSize);
        }
    }
}