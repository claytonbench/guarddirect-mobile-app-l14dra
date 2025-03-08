using Microsoft.EntityFrameworkCore; // v8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the IReportRepository interface using Entity Framework Core for data access operations on Report entities.
    /// Provides methods for creating, retrieving, updating, and deleting reports, as well as specialized queries for report management and synchronization.
    /// </summary>
    public class ReportRepository : IReportRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportRepository"/> class with the specified database context.
        /// </summary>
        /// <param name="context">The database context for accessing Report entities.</param>
        /// <exception cref="ArgumentNullException">Thrown if the context is null.</exception>
        public ReportRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a report by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the report if found, otherwise null.</returns>
        public async Task<Report> GetByIdAsync(int id)
        {
            return await _context.Reports.FindAsync(id);
        }

        /// <summary>
        /// Retrieves a report by its remote identifier (assigned by the backend system).
        /// </summary>
        /// <param name="remoteId">The remote identifier of the report.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the report if found, otherwise null.</returns>
        public async Task<Report> GetByRemoteIdAsync(string remoteId)
        {
            return await _context.Reports.FirstOrDefaultAsync(r => r.RemoteId == remoteId);
        }

        /// <summary>
        /// Retrieves all reports.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all reports.</returns>
        public async Task<IEnumerable<Report>> GetAllAsync()
        {
            return await _context.Reports
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of reports.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paginated list of reports.</returns>
        public async Task<PaginatedList<Report>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            var query = _context.Reports
                .OrderByDescending(r => r.Timestamp)
                .AsQueryable();

            return await PaginatedList<Report>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Retrieves all reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of reports for the specified user.</returns>
        public async Task<IEnumerable<Report>> GetByUserIdAsync(string userId)
        {
            return await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a paginated list of reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of reports per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paginated list of reports for the specified user.</returns>
        public async Task<PaginatedList<Report>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Timestamp)
                .AsQueryable();

            return await PaginatedList<Report>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <summary>
        /// Retrieves reports that have not been synced.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of unsynced reports.</returns>
        public async Task<IEnumerable<Report>> GetUnsyncedAsync()
        {
            return await _context.Reports
                .Where(r => !r.IsSynced)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves reports created within a specific date range.
        /// </summary>
        /// <param name="startDate">The start date of the range (inclusive).</param>
        /// <param name="endDate">The end date of the range (inclusive).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of reports created within the specified date range.</returns>
        public async Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Reports
                .Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new report to the repository.
        /// </summary>
        /// <param name="report">The report to add.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added report with its assigned ID.</returns>
        public async Task<Report> AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
            return report;
        }

        /// <summary>
        /// Updates an existing report in the repository.
        /// </summary>
        /// <param name="report">The report to update.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a report from the repository.
        /// </summary>
        /// <param name="id">The unique identifier of the report to delete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task DeleteAsync(int id)
        {
            var report = await GetByIdAsync(id);
            if (report != null)
            {
                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Updates the sync status of a report.
        /// </summary>
        /// <param name="id">The unique identifier of the report.</param>
        /// <param name="isSynced">The sync status to set.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task UpdateSyncStatusAsync(int id, bool isSynced)
        {
            var report = await GetByIdAsync(id);
            if (report != null)
            {
                report.IsSynced = isSynced;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets the total count of reports in the repository.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of reports.</returns>
        public async Task<int> CountAsync()
        {
            return await _context.Reports.CountAsync();
        }

        /// <summary>
        /// Gets the count of reports for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of reports for the specified user.</returns>
        public async Task<int> CountByUserIdAsync(string userId)
        {
            return await _context.Reports.CountAsync(r => r.UserId == userId);
        }
    }
}