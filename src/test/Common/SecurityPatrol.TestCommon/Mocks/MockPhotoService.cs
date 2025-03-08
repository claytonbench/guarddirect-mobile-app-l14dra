using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Helpers;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of IPhotoService for testing purposes that simulates photo capture, 
    /// storage, and management functionality without accessing actual device camera or storage.
    /// </summary>
    public class MockPhotoService : IPhotoService, IDisposable
    {
        /// <summary>
        /// Gets the collection of mock photos for testing.
        /// </summary>
        public List<PhotoModel> Photos { get; private set; }
        
        /// <summary>
        /// Gets the dictionary of photo streams indexed by photo ID.
        /// </summary>
        public Dictionary<string, MemoryStream> PhotoStreams { get; private set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether operations should succeed.
        /// </summary>
        public bool ShouldSucceed { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether operations should throw exceptions.
        /// </summary>
        public bool ShouldThrowException { get; set; }
        
        /// <summary>
        /// Gets or sets the exception to throw when ShouldThrowException is true.
        /// </summary>
        public Exception ExceptionToThrow { get; set; }
        
        /// <summary>
        /// Gets the number of times CapturePhotoAsync was called.
        /// </summary>
        public int CapturePhotoCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetStoredPhotosAsync was called.
        /// </summary>
        public int GetStoredPhotosCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetPhotoAsync was called.
        /// </summary>
        public int GetPhotoCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetPhotoFileAsync was called.
        /// </summary>
        public int GetPhotoFileCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times DeletePhotoAsync was called.
        /// </summary>
        public int DeletePhotoCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times CleanupOldPhotosAsync was called.
        /// </summary>
        public int CleanupOldPhotosCallCount { get; private set; }
        
        /// <summary>
        /// Gets the number of times GetStorageUsageAsync was called.
        /// </summary>
        public int GetStorageUsageCallCount { get; private set; }
        
        /// <summary>
        /// Gets the simulated storage usage in bytes.
        /// </summary>
        public long StorageUsage { get; private set; }
        
        /// <summary>
        /// Gets or sets the size of each mock photo in bytes.
        /// </summary>
        public int PhotoSizeInBytes { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the MockPhotoService class with default settings.
        /// </summary>
        public MockPhotoService()
        {
            Photos = new List<PhotoModel>();
            PhotoStreams = new Dictionary<string, MemoryStream>();
            ShouldSucceed = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            CapturePhotoCallCount = 0;
            GetStoredPhotosCallCount = 0;
            GetPhotoCallCount = 0;
            GetPhotoFileCallCount = 0;
            DeletePhotoCallCount = 0;
            CleanupOldPhotosCallCount = 0;
            GetStorageUsageCallCount = 0;
            StorageUsage = 0;
            PhotoSizeInBytes = 100000; // 100KB default size
        }
        
        /// <summary>
        /// Mocks capturing a photo using the device camera.
        /// </summary>
        /// <returns>A task that returns a mock photo model.</returns>
        public async Task<PhotoModel> CapturePhotoAsync()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            CapturePhotoCallCount++;
            
            if (!ShouldSucceed)
                return null;
                
            // Generate a unique ID
            string id = Guid.NewGuid().ToString();
            
            // Create a photo model
            var photo = MockDataGenerator.CreatePhotoModel(id, TestConstants.TestUserId, TestConstants.TestImagePath);
            
            // Generate test image
            var imageStream = await TestImageGenerator.GenerateTestImageWithTimestampAsync();
            
            // Store the stream
            PhotoStreams[id] = imageStream;
            
            // Add to collection
            Photos.Add(photo);
            
            // Update storage usage
            StorageUsage += PhotoSizeInBytes;
            
            return photo;
        }
        
        /// <summary>
        /// Mocks retrieving all stored photos.
        /// </summary>
        /// <returns>A task that returns a list of all stored photo models.</returns>
        public async Task<List<PhotoModel>> GetStoredPhotosAsync()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            GetStoredPhotosCallCount++;
            
            if (!ShouldSucceed)
                return new List<PhotoModel>();
                
            // Return a copy of the photos collection
            return Photos.ToList();
        }
        
        /// <summary>
        /// Mocks retrieving a specific photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve.</param>
        /// <returns>A task that returns the photo model with the specified ID, or null if not found.</returns>
        public async Task<PhotoModel> GetPhotoAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
                
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            GetPhotoCallCount++;
            
            if (!ShouldSucceed)
                return null;
                
            // Find and return the photo
            return Photos.FirstOrDefault(p => p.Id == id);
        }
        
        /// <summary>
        /// Mocks retrieving the image file stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve the file for.</param>
        /// <returns>A task that returns the image stream for the photo, or null if not found.</returns>
        public async Task<Stream> GetPhotoFileAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
                
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            GetPhotoFileCallCount++;
            
            if (!ShouldSucceed)
                return null;
                
            // Check if we have the stream
            if (PhotoStreams.TryGetValue(id, out var stream))
            {
                // Create a copy to avoid disposing the original
                var copy = new MemoryStream();
                stream.Position = 0;
                await stream.CopyToAsync(copy);
                copy.Position = 0;
                return copy;
            }
            
            return null;
        }
        
        /// <summary>
        /// Mocks deleting a photo and its associated image file.
        /// </summary>
        /// <param name="id">The ID of the photo to delete.</param>
        /// <returns>A task that returns true if the photo was deleted, false otherwise.</returns>
        public async Task<bool> DeletePhotoAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));
                
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            DeletePhotoCallCount++;
            
            if (!ShouldSucceed)
                return false;
                
            // Find the photo
            var photo = Photos.FirstOrDefault(p => p.Id == id);
            if (photo == null)
                return false;
                
            // Remove from collection
            Photos.Remove(photo);
            
            // Remove and dispose the stream if it exists
            if (PhotoStreams.TryGetValue(id, out var stream))
            {
                PhotoStreams.Remove(id);
                stream.Dispose();
                
                // Update storage usage
                StorageUsage -= PhotoSizeInBytes;
            }
            
            return true;
        }
        
        /// <summary>
        /// Mocks deleting photos older than the specified retention period that have been synchronized.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos. Photos older than this will be deleted if synced.</param>
        /// <returns>A task that returns the number of photos deleted.</returns>
        public async Task<int> CleanupOldPhotosAsync(int retentionDays)
        {
            if (retentionDays <= 0)
                throw new ArgumentException("Retention days must be greater than zero", nameof(retentionDays));
                
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            CleanupOldPhotosCallCount++;
            
            if (!ShouldSucceed)
                return 0;
                
            // Calculate cutoff date
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            // Find photos to delete
            var photosToDelete = Photos
                .Where(p => p.Timestamp < cutoffDate && p.IsSynced)
                .ToList();
                
            // Delete each photo
            foreach (var photo in photosToDelete)
            {
                Photos.Remove(photo);
                
                // Remove and dispose the stream if it exists
                if (PhotoStreams.TryGetValue(photo.Id, out var stream))
                {
                    PhotoStreams.Remove(photo.Id);
                    stream.Dispose();
                }
            }
            
            // Update storage usage
            StorageUsage -= photosToDelete.Count * PhotoSizeInBytes;
            
            return photosToDelete.Count;
        }
        
        /// <summary>
        /// Mocks retrieving the current storage usage for photos in bytes.
        /// </summary>
        /// <returns>A task that returns the total size of stored photos in bytes.</returns>
        public async Task<long> GetStorageUsageAsync()
        {
            if (ShouldThrowException && ExceptionToThrow != null)
                throw ExceptionToThrow;
                
            GetStorageUsageCallCount++;
            
            return StorageUsage;
        }
        
        /// <summary>
        /// Sets up a collection of mock photos for testing.
        /// </summary>
        /// <param name="photos">The photos to set up.</param>
        public void SetupPhotos(List<PhotoModel> photos)
        {
            // Clear existing data
            Photos.Clear();
            
            foreach (var stream in PhotoStreams.Values)
            {
                stream.Dispose();
            }
            PhotoStreams.Clear();
            
            StorageUsage = 0;
            
            if (photos == null || photos.Count == 0)
                return;
                
            // Add new photos
            Photos.AddRange(photos);
            
            // Generate streams for each photo
            foreach (var photo in photos)
            {
                var stream = TestImageGenerator.GenerateTestImageAsync().Result;
                PhotoStreams[photo.Id] = stream;
            }
            
            // Update storage usage
            StorageUsage = photos.Count * PhotoSizeInBytes;
        }
        
        /// <summary>
        /// Sets up custom image streams for specific photo IDs.
        /// </summary>
        /// <param name="streams">Dictionary of photo ID to stream mappings.</param>
        public void SetupPhotoStreams(Dictionary<string, Stream> streams)
        {
            if (streams == null)
                throw new ArgumentNullException(nameof(streams));
                
            foreach (var kvp in streams)
            {
                if (kvp.Value == null)
                    continue;
                    
                // Create a new stream and copy the content
                var newStream = new MemoryStream();
                kvp.Value.Position = 0;
                kvp.Value.CopyTo(newStream);
                newStream.Position = 0;
                
                // Add or replace existing stream
                if (PhotoStreams.TryGetValue(kvp.Key, out var existingStream))
                {
                    existingStream.Dispose();
                }
                
                PhotoStreams[kvp.Key] = newStream;
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown by any method.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
        }
        
        /// <summary>
        /// Clears any configured exception.
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }
        
        /// <summary>
        /// Sets the simulated size of photos in bytes.
        /// </summary>
        /// <param name="sizeInBytes">The size in bytes for each photo.</param>
        public void SetPhotoSize(int sizeInBytes)
        {
            if (sizeInBytes <= 0)
                throw new ArgumentException("Size must be greater than zero", nameof(sizeInBytes));
                
            PhotoSizeInBytes = sizeInBytes;
            
            // Recalculate storage usage
            StorageUsage = Photos.Count * PhotoSizeInBytes;
        }
        
        /// <summary>
        /// Verifies that CapturePhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyCapturePhotoCalled() => CapturePhotoCallCount > 0;
        
        /// <summary>
        /// Verifies that GetStoredPhotosAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetStoredPhotosCalled() => GetStoredPhotosCallCount > 0;
        
        /// <summary>
        /// Verifies that GetPhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetPhotoCalled() => GetPhotoCallCount > 0;
        
        /// <summary>
        /// Verifies that GetPhotoFileAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetPhotoFileCalled() => GetPhotoFileCallCount > 0;
        
        /// <summary>
        /// Verifies that DeletePhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyDeletePhotoCalled() => DeletePhotoCallCount > 0;
        
        /// <summary>
        /// Verifies that CleanupOldPhotosAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyCleanupOldPhotosCalled() => CleanupOldPhotosCallCount > 0;
        
        /// <summary>
        /// Verifies that GetStorageUsageAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetStorageUsageCalled() => GetStorageUsageCallCount > 0;
        
        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            Photos.Clear();
            
            foreach (var stream in PhotoStreams.Values)
            {
                stream.Dispose();
            }
            PhotoStreams.Clear();
            
            ShouldSucceed = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            CapturePhotoCallCount = 0;
            GetStoredPhotosCallCount = 0;
            GetPhotoCallCount = 0;
            GetPhotoFileCallCount = 0;
            DeletePhotoCallCount = 0;
            CleanupOldPhotosCallCount = 0;
            GetStorageUsageCallCount = 0;
            StorageUsage = 0;
            PhotoSizeInBytes = 100000;
        }
        
        /// <summary>
        /// Disposes the MockPhotoService and releases resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var stream in PhotoStreams.Values)
            {
                stream.Dispose();
            }
            PhotoStreams.Clear();
            Photos.Clear();
        }
    }
}