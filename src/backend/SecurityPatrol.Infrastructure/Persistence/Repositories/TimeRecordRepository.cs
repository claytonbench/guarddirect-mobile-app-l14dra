using Microsoft.EntityFrameworkCore; // Microsoft.EntityFrameworkCore 8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System; // System 8.0.0
using System.Collections.Generic; // System.Collections.Generic 8.0.0
using System.Linq; // System.Linq 8.0.0
using System.Threading.Tasks; // System.Threading.Tasks 8.0.0

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the ITimeRecordRepository interface for handling 
    /// TimeRecord entity persistence operations using Entity Framework Core.
    /// </summary>
    public class TimeRecordRepository : ITimeRecordRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the TimeRecordRepository class.
        /// </summary>
        /// <param name="context">The database context to use for data operations.</param>
        /// <exception cref="ArgumentNullException">Thrown if context is null.</exception>
        public TimeRecordRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<TimeRecord> GetByIdAsync(int id)
        {
            return await _context.TimeRecords
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <inheritdoc/>
        public async Task<TimeRecord> GetByRemoteIdAsync(string remoteId)
        {
            if (string.IsNullOrEmpty(remoteId))
                return null;

            return await _context.TimeRecords
                .FirstOrDefaultAsync(t => t.RemoteId == remoteId);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TimeRecord>> GetAllAsync()
        {
            return await _context.TimeRecords
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<PaginatedList<TimeRecord>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            var query = _context.TimeRecords
                .OrderByDescending(t => t.Timestamp);

            return await PaginatedList<TimeRecord>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TimeRecord>> GetByUserIdAsync(string userId)
        {
            return await _context.TimeRecords
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<PaginatedList<TimeRecord>> GetPaginatedByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _context.TimeRecords
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp);

            return await PaginatedList<TimeRecord>.CreateAsync(query, pageNumber, pageSize);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TimeRecord>> GetByUserIdAndDateAsync(string userId, DateTime date)
        {
            // Get records for the specific date (midnight to midnight)
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _context.TimeRecords
                .Where(t => t.UserId == userId && t.Timestamp >= startDate && t.Timestamp < endDate)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TimeRecord>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            // Ensure endDate is inclusive by setting it to the end of the day
            var adjustedEndDate = endDate.Date.AddDays(1);

            return await _context.TimeRecords
                .Where(t => t.UserId == userId && t.Timestamp >= startDate && t.Timestamp < adjustedEndDate)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<TimeRecord>> GetUnsyncedAsync()
        {
            return await _context.TimeRecords
                .Where(t => !t.IsSynced)
                .OrderBy(t => t.Timestamp) // Order by oldest first for syncing
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<TimeRecord> GetLatestByUserIdAsync(string userId)
        {
            return await _context.TimeRecords
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc/>
        public async Task<string> GetCurrentStatusAsync(string userId)
        {
            var latestRecord = await GetLatestByUserIdAsync(userId);
            
            if (latestRecord == null)
                return "out"; // Default status if no records exist
            
            return latestRecord.Type.ToLower();
        }

        /// <inheritdoc/>
        public async Task<TimeRecord> AddAsync(TimeRecord timeRecord)
        {
            if (timeRecord == null)
                throw new ArgumentNullException(nameof(timeRecord));

            await _context.TimeRecords.AddAsync(timeRecord);
            await _context.SaveChangesAsync();
            
            return timeRecord;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(TimeRecord timeRecord)
        {
            if (timeRecord == null)
                throw new ArgumentNullException(nameof(timeRecord));

            _context.TimeRecords.Update(timeRecord);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(int id)
        {
            var timeRecord = await GetByIdAsync(id);
            if (timeRecord != null)
            {
                _context.TimeRecords.Remove(timeRecord);
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateSyncStatusAsync(int id, bool isSynced)
        {
            var timeRecord = await GetByIdAsync(id);
            if (timeRecord != null)
            {
                timeRecord.IsSynced = isSynced;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<int> DeleteOlderThanAsync(DateTime date, bool onlySynced)
        {
            var query = _context.TimeRecords.Where(t => t.Timestamp < date);
            
            if (onlySynced)
            {
                query = query.Where(t => t.IsSynced);
            }

            var recordsToDelete = await query.ToListAsync();
            
            if (recordsToDelete.Any())
            {
                _context.TimeRecords.RemoveRange(recordsToDelete);
                await _context.SaveChangesAsync();
            }
            
            return recordsToDelete.Count;
        }

        /// <inheritdoc/>
        public async Task<int> CountAsync()
        {
            return await _context.TimeRecords.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> CountByUserIdAsync(string userId)
        {
            return await _context.TimeRecords
                .Where(t => t.UserId == userId)
                .CountAsync();
        }
    }
}