using Microsoft.EntityFrameworkCore; // v8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using System; // v8.0.0
using System.Collections.Generic; // v8.0.0
using System.Linq; // v8.0.0
using System.Threading.Tasks; // v8.0.0

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implementation of the ILocationRecordRepository interface using Entity Framework Core 
    /// for data access operations on LocationRecord entities.
    /// </summary>
    public class LocationRecordRepository : ILocationRecordRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the LocationRecordRepository with the specified database context.
        /// </summary>
        /// <param name="context">The database context for accessing LocationRecord entities.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public LocationRecordRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a location record by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the location record.</param>
        /// <returns>The location record with the specified ID, or null if not found.</returns>
        public async Task<LocationRecord> GetByIdAsync(int id)
        {
            return await _context.LocationRecords.FindAsync(id);
        }

        /// <summary>
        /// Retrieves location records for a specific user with optional limit.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A collection of location records for the specified user.</returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty.</exception>
        public async Task<IEnumerable<LocationRecord>> GetByUserIdAsync(string userId, int limit)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var query = _context.LocationRecords.Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp);

            if (limit > 0)
            {
                query = query.Take(limit);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves location records for a specific user within a time range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="startTime">The start time of the range.</param>
        /// <param name="endTime">The end time of the range.</param>
        /// <returns>A collection of location records for the specified user within the time range.</returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when endTime is earlier than startTime.</exception>
        public async Task<IEnumerable<LocationRecord>> GetByUserIdAndTimeRangeAsync(
            string userId, DateTime startTime, DateTime endTime)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (endTime < startTime)
            {
                throw new ArgumentException("End time must be later than or equal to start time", nameof(endTime));
            }

            return await _context.LocationRecords
                .Where(l => l.UserId == userId && l.Timestamp >= startTime && l.Timestamp <= endTime)
                .OrderBy(l => l.Timestamp)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new location record to the system.
        /// </summary>
        /// <param name="locationRecord">The location record to add.</param>
        /// <returns>The ID of the newly created location record if successful.</returns>
        /// <exception cref="ArgumentNullException">Thrown when locationRecord is null.</exception>
        public async Task<int> AddAsync(LocationRecord locationRecord)
        {
            if (locationRecord == null)
            {
                throw new ArgumentNullException(nameof(locationRecord));
            }

            await _context.LocationRecords.AddAsync(locationRecord);
            await _context.SaveChangesAsync();
            return locationRecord.Id;
        }

        /// <summary>
        /// Adds multiple location records to the system in a batch operation.
        /// Optimized for efficient storage of location data collected in memory,
        /// supporting the batch processing requirement (50 records or 60 seconds).
        /// </summary>
        /// <param name="locationRecords">The collection of location records to add.</param>
        /// <returns>The IDs of successfully added records.</returns>
        /// <exception cref="ArgumentNullException">Thrown when locationRecords is null.</exception>
        public async Task<IEnumerable<int>> AddRangeAsync(IEnumerable<LocationRecord> locationRecords)
        {
            if (locationRecords == null)
            {
                throw new ArgumentNullException(nameof(locationRecords));
            }

            // Convert to list to avoid multiple enumeration
            var records = locationRecords.ToList();
            
            // Skip execution if the collection is empty
            if (records.Count == 0)
            {
                return Enumerable.Empty<int>();
            }

            await _context.LocationRecords.AddRangeAsync(records);
            await _context.SaveChangesAsync();
            
            // Return the IDs of the added records
            return records.Select(r => r.Id).ToList();
        }

        /// <summary>
        /// Updates an existing location record in the system.
        /// </summary>
        /// <param name="locationRecord">The location record to update.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when locationRecord is null.</exception>
        public async Task<bool> UpdateAsync(LocationRecord locationRecord)
        {
            if (locationRecord == null)
            {
                throw new ArgumentNullException(nameof(locationRecord));
            }

            // Only track the entity if it's not already being tracked
            if (!_context.LocationRecords.Local.Any(l => l.Id == locationRecord.Id))
            {
                _context.Entry(locationRecord).State = EntityState.Modified;
            }
            
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LocationRecordExists(locationRecord.Id))
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// Updates the synchronization status of location records.
        /// Called after successful synchronization with the backend API.
        /// </summary>
        /// <param name="ids">The IDs of the location records to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ids is null.</exception>
        public async Task<bool> UpdateSyncStatusAsync(IEnumerable<int> ids, bool isSynced)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            // Convert to array to avoid multiple enumeration
            var idArray = ids.ToArray();
            
            if (idArray.Length == 0)
            {
                return true; // Nothing to update
            }

            // For larger datasets, consider using a more efficient approach
            if (idArray.Length > 100)
            {
                // Process in batches of 100 to avoid excessive memory usage
                bool success = true;
                for (int i = 0; i < idArray.Length; i += 100)
                {
                    var batchIds = idArray.Skip(i).Take(100).ToArray();
                    var batchSuccess = await UpdateSyncStatusBatchAsync(batchIds, isSynced);
                    success = success && batchSuccess;
                }
                return success;
            }
            else
            {
                return await UpdateSyncStatusBatchAsync(idArray, isSynced);
            }
        }

        /// <summary>
        /// Updates the synchronization status for a batch of location records.
        /// </summary>
        /// <param name="ids">Array of record IDs to update.</param>
        /// <param name="isSynced">The new synchronization status.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        private async Task<bool> UpdateSyncStatusBatchAsync(int[] ids, bool isSynced)
        {
            var records = await _context.LocationRecords
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            if (!records.Any())
            {
                return false; // No matching records found
            }

            // Update the sync status
            foreach (var record in records)
            {
                record.IsSynced = isSynced;
            }

            // Save changes to the database
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Deletes a location record from the system.
        /// </summary>
        /// <param name="id">The ID of the location record to delete.</param>
        /// <returns>True if the delete was successful, false otherwise.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var locationRecord = await _context.LocationRecords.FindAsync(id);
            if (locationRecord == null)
            {
                return false;
            }

            _context.LocationRecords.Remove(locationRecord);
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Deletes location records older than a specified date.
        /// Supports the data retention policy of keeping location history for 30 days
        /// and then automatically purging via scheduled job.
        /// </summary>
        /// <param name="date">The cutoff date for deletion.</param>
        /// <param name="onlySynced">If true, only deletes records that have been synchronized to ensure data is not lost.</param>
        /// <returns>The number of records deleted.</returns>
        public async Task<int> DeleteOlderThanAsync(DateTime date, bool onlySynced)
        {
            var query = _context.LocationRecords.Where(r => r.Timestamp < date);
            
            if (onlySynced)
            {
                query = query.Where(r => r.IsSynced);
            }

            // For efficiency, especially with large datasets, use a direct deletion approach
            // if the database provider supports it (EF Core 7.0+)
            try
            {
                // Try to use a more efficient delete operation for supported providers
                return await query.ExecuteDeleteAsync();
            }
            catch (InvalidOperationException)
            {
                // Fall back to the traditional approach if ExecuteDeleteAsync is not supported
                var records = await query.ToListAsync();
                
                if (!records.Any())
                {
                    return 0;
                }

                _context.LocationRecords.RemoveRange(records);
                await _context.SaveChangesAsync();
                
                return records.Count;
            }
        }

        /// <summary>
        /// Retrieves location records that have not been synchronized.
        /// Used by the synchronization service to find records that need to be sent to the backend.
        /// </summary>
        /// <param name="limit">The maximum number of records to retrieve.</param>
        /// <returns>A collection of unsynced location records.</returns>
        public async Task<IEnumerable<LocationRecord>> GetUnsyncedRecordsAsync(int limit)
        {
            var query = _context.LocationRecords
                .Where(r => !r.IsSynced)
                .OrderBy(r => r.Timestamp); // Process oldest records first

            if (limit > 0)
            {
                query = query.Take(limit);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves the most recent location record for a specific user.
        /// Used to display current user location on maps and for proximity calculations.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>The most recent location record for the specified user, or null if none exists.</returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty.</exception>
        public async Task<LocationRecord> GetLatestLocationAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return await _context.LocationRecords
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Checks if a location record with the specified ID exists in the database.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        /// <returns>True if the record exists, false otherwise.</returns>
        private async Task<bool> LocationRecordExists(int id)
        {
            return await _context.LocationRecords.AnyAsync(e => e.Id == id);
        }
    }
}