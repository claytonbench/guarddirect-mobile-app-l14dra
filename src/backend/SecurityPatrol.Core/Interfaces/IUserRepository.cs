using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for data access operations on User entities in the Security Patrol application.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>The user with the specified ID, or null if not found.</returns>
        Task<User> GetByIdAsync(string id);

        /// <summary>
        /// Retrieves a user by their phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number of the user.</param>
        /// <returns>The user with the specified phone number, or null if not found.</returns>
        Task<User> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        /// <returns>A collection of all users.</returns>
        Task<IEnumerable<User>> GetAllAsync();

        /// <summary>
        /// Retrieves a paginated list of users.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of users per page.</param>
        /// <returns>A paginated list of users.</returns>
        Task<PaginatedList<User>> GetPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves all active users.
        /// </summary>
        /// <returns>A collection of active users.</returns>
        Task<IEnumerable<User>> GetActiveUsersAsync();

        /// <summary>
        /// Retrieves users who have authenticated within a specified time period.
        /// </summary>
        /// <param name="since">The starting date and time for the time period.</param>
        /// <returns>A collection of recently authenticated users.</returns>
        Task<IEnumerable<User>> GetRecentlyAuthenticatedAsync(DateTime since);

        /// <summary>
        /// Adds a new user to the repository.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>The added user with its assigned ID.</returns>
        Task<User> AddAsync(User user);

        /// <summary>
        /// Updates an existing user in the repository.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(User user);

        /// <summary>
        /// Deletes a user from the repository.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string id);

        /// <summary>
        /// Deactivates a user without deleting them from the repository.
        /// </summary>
        /// <param name="id">The ID of the user to deactivate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeactivateAsync(string id);

        /// <summary>
        /// Updates the LastAuthenticated timestamp for a user.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="timestamp">The new LastAuthenticated timestamp.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateLastAuthenticatedAsync(string id, DateTime timestamp);

        /// <summary>
        /// Checks if a user with the specified ID exists.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        /// <returns>True if the user exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Checks if a user with the specified phone number exists.
        /// </summary>
        /// <param name="phoneNumber">The phone number to check.</param>
        /// <returns>True if the user exists, false otherwise.</returns>
        Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// Gets the total count of users in the repository.
        /// </summary>
        /// <returns>The total number of users.</returns>
        Task<int> CountAsync();

        /// <summary>
        /// Gets the count of active users in the repository.
        /// </summary>
        /// <returns>The number of active users.</returns>
        Task<int> CountActiveAsync();
    }
}