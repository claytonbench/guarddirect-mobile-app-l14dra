using Microsoft.EntityFrameworkCore; // Version 8.0.0
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecurityPatrol.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Implements the IUserRepository interface to provide data access operations for User entities
    /// using Entity Framework Core.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly SecurityPatrolDbContext _context;

        /// <summary>
        /// Initializes a new instance of the UserRepository class with the specified database context.
        /// </summary>
        /// <param name="context">The database context to use for data access operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public UserRepository(SecurityPatrolDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<User> GetByIdAsync(string id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <inheritdoc/>
        public async Task<User> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .OrderBy(u => u.PhoneNumber)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<PaginatedList<User>> GetPaginatedAsync(int pageNumber, int pageSize)
        {
            return await PaginatedList<User>.CreateAsync(
                _context.Users.OrderBy(u => u.PhoneNumber),
                pageNumber,
                pageSize);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.PhoneNumber)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<User>> GetRecentlyAuthenticatedAsync(DateTime since)
        {
            return await _context.Users
                .Where(u => u.LastAuthenticated >= since)
                .OrderByDescending(u => u.LastAuthenticated)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<User> AddAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <inheritdoc/>
        public async Task UpdateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task DeactivateAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateLastAuthenticatedAsync(string id, DateTime timestamp)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                user.LastAuthenticated = timestamp;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }

        /// <inheritdoc/>
        public async Task<int> CountAsync()
        {
            return await _context.Users.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> CountActiveAsync()
        {
            return await _context.Users.CountAsync(u => u.IsActive);
        }
    }
}