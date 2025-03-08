using System;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.Core.Models;

namespace SecurityPatrol.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for file storage operations in the Security Patrol application.
    /// This interface abstracts the storage mechanism for binary data such as photos captured during security patrols.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Stores a file in the storage system.
        /// </summary>
        /// <param name="fileStream">The stream containing the file content to store.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="contentType">The MIME content type of the file.</param>
        /// <returns>A result containing the file path or identifier if successful.</returns>
        Task<Result<string>> StoreFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Retrieves a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to retrieve.</param>
        /// <returns>A result containing the file stream if successful.</returns>
        Task<Result<Stream>> GetFileAsync(string filePath);

        /// <summary>
        /// Deletes a file from the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to delete.</param>
        /// <returns>A result indicating success or failure of the delete operation.</returns>
        Task<Result> DeleteFileAsync(string filePath);

        /// <summary>
        /// Checks if a file exists in the storage system.
        /// </summary>
        /// <param name="filePath">The path or identifier of the file to check.</param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> FileExistsAsync(string filePath);
    }
}