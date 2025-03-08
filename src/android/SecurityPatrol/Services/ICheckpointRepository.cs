using System;
using System.Collections.Generic;
using System.Threading.Tasks; // Version 8.0+
using SecurityPatrol.Models;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for checkpoint repository operations in the Security Patrol application.
    /// It provides methods for retrieving, storing, and managing checkpoint data for patrol operations,
    /// supporting both online and offline functionality.
    /// </summary>
    public interface ICheckpointRepository
    {
        /// <summary>
        /// Retrieves all checkpoints from the database.
        /// </summary>
        /// <returns>A task that returns a collection of all checkpoints.</returns>
        /// <exception cref="InvalidOperationException">Thrown when checkpoints cannot be retrieved.</exception>
        Task<IEnumerable<CheckpointModel>> GetAllCheckpointsAsync();

        /// <summary>
        /// Retrieves a checkpoint by its ID.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to retrieve.</param>
        /// <returns>A task that returns the checkpoint with the specified ID, or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the checkpoint cannot be retrieved.</exception>
        Task<CheckpointModel> GetCheckpointByIdAsync(int checkpointId);

        /// <summary>
        /// Retrieves all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns a collection of checkpoints for the specified location.</returns>
        /// <exception cref="ArgumentException">Thrown when locationId is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when checkpoints cannot be retrieved.</exception>
        Task<IEnumerable<CheckpointModel>> GetCheckpointsByLocationIdAsync(int locationId);

        /// <summary>
        /// Saves a collection of checkpoints to the database, updating existing ones and inserting new ones.
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to save.</param>
        /// <returns>A task that returns the number of checkpoints saved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when checkpoints is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when checkpoints cannot be saved.</exception>
        Task<int> SaveCheckpointsAsync(IEnumerable<CheckpointModel> checkpoints);

        /// <summary>
        /// Saves a single checkpoint to the database, updating if it exists or inserting if it's new.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to save.</param>
        /// <returns>A task that returns the ID of the saved checkpoint.</returns>
        /// <exception cref="ArgumentNullException">Thrown when checkpoint is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the checkpoint cannot be saved.</exception>
        Task<int> SaveCheckpointAsync(CheckpointModel checkpoint);

        /// <summary>
        /// Deletes a checkpoint from the database by its ID.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to delete.</param>
        /// <returns>A task that returns true if the checkpoint was deleted, false if it wasn't found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the checkpoint cannot be deleted.</exception>
        Task<bool> DeleteCheckpointAsync(int checkpointId);

        /// <summary>
        /// Deletes all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns the number of checkpoints deleted.</returns>
        /// <exception cref="ArgumentException">Thrown when locationId is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when checkpoints cannot be deleted.</exception>
        Task<int> DeleteAllCheckpointsForLocationAsync(int locationId);

        /// <summary>
        /// Retrieves the verification status of all checkpoints for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns a collection of checkpoint statuses for the specified location.</returns>
        /// <exception cref="ArgumentException">Thrown when locationId is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when checkpoint statuses cannot be retrieved.</exception>
        Task<IEnumerable<CheckpointStatus>> GetCheckpointStatusesAsync(int locationId);

        /// <summary>
        /// Saves the verification status of a checkpoint.
        /// </summary>
        /// <param name="status">The checkpoint status to save.</param>
        /// <returns>A task that returns true if the status was saved successfully.</returns>
        /// <exception cref="ArgumentNullException">Thrown when status is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the status cannot be saved.</exception>
        Task<bool> SaveCheckpointStatusAsync(CheckpointStatus status);

        /// <summary>
        /// Saves the verification status of multiple checkpoints.
        /// </summary>
        /// <param name="statuses">The collection of checkpoint statuses to save.</param>
        /// <returns>A task that returns the number of statuses saved.</returns>
        /// <exception cref="ArgumentNullException">Thrown when statuses is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when statuses cannot be saved.</exception>
        Task<int> SaveCheckpointStatusesAsync(IEnumerable<CheckpointStatus> statuses);

        /// <summary>
        /// Clears all checkpoint verification statuses for a specific patrol location.
        /// </summary>
        /// <param name="locationId">The ID of the patrol location.</param>
        /// <returns>A task that returns the number of statuses cleared.</returns>
        /// <exception cref="ArgumentException">Thrown when locationId is invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown when statuses cannot be cleared.</exception>
        Task<int> ClearCheckpointStatusesAsync(int locationId);
    }
}