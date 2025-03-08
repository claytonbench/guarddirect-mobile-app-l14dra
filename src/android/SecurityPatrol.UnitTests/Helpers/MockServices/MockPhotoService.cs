using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of IPhotoService for unit testing that provides configurable
    /// responses for photo capture and management operations.
    /// </summary>
    public class MockPhotoService : IPhotoService
    {
        // Storage for photos
        public List<PhotoModel> StoredPhotos { get; private set; }
        
        // Configurable results
        public PhotoModel CapturePhotoResult { get; private set; }
        public bool DeletePhotoResult { get; private set; }
        public int CleanupOldPhotosResult { get; private set; }
        public long StorageUsageResult { get; private set; }
        
        // Exception handling
        public bool ShouldThrowException { get; private set; }
        public Exception ExceptionToThrow { get; private set; }
        
        // Call tracking
        public int CapturePhotoCallCount { get; private set; }
        public int GetStoredPhotosCallCount { get; private set; }
        public int GetPhotoCallCount { get; private set; }
        public int GetPhotoFileCallCount { get; private set; }
        public int DeletePhotoCallCount { get; private set; }
        public int CleanupOldPhotosCallCount { get; private set; }
        public int GetStorageUsageCallCount { get; private set; }
        
        // Photo file streams
        public Dictionary<string, Stream> PhotoStreams { get; private set; }
        
        /// <summary>
        /// Initializes a new instance of the MockPhotoService class with default settings.
        /// </summary>
        public MockPhotoService()
        {
            StoredPhotos = new List<PhotoModel>();
            PhotoStreams = new Dictionary<string, Stream>();
            DeletePhotoResult = true;
            CleanupOldPhotosResult = 0;
            StorageUsageResult = 0;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            CapturePhotoCallCount = 0;
            GetStoredPhotosCallCount = 0;
            GetPhotoCallCount = 0;
            GetPhotoFileCallCount = 0;
            DeletePhotoCallCount = 0;
            CleanupOldPhotosCallCount = 0;
            GetStorageUsageCallCount = 0;
        }
        
        /// <summary>
        /// Mocks capturing a photo using the device camera.
        /// </summary>
        /// <returns>A task that returns the configured photo model result.</returns>
        public async Task<PhotoModel> CapturePhotoAsync()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            CapturePhotoCallCount++;
            
            if (CapturePhotoResult != null)
            {
                StoredPhotos.Add(CapturePhotoResult);
            }
            
            return await Task.FromResult(CapturePhotoResult);
        }
        
        /// <summary>
        /// Mocks retrieving all stored photos.
        /// </summary>
        /// <returns>A task that returns the list of stored photos.</returns>
        public async Task<List<PhotoModel>> GetStoredPhotosAsync()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            GetStoredPhotosCallCount++;
            
            return await Task.FromResult(new List<PhotoModel>(StoredPhotos));
        }
        
        /// <summary>
        /// Mocks retrieving a specific photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve.</param>
        /// <returns>A task that returns the photo with the specified ID, or null if not found.</returns>
        public async Task<PhotoModel> GetPhotoAsync(string id)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            GetPhotoCallCount++;
            
            return await Task.FromResult(StoredPhotos.Find(p => p.Id == id));
        }
        
        /// <summary>
        /// Mocks retrieving the image file stream for a photo by its ID.
        /// </summary>
        /// <param name="id">The ID of the photo to retrieve the file for.</param>
        /// <returns>A task that returns the configured stream for the photo, or null if not found.</returns>
        public async Task<Stream> GetPhotoFileAsync(string id)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            GetPhotoFileCallCount++;
            
            PhotoStreams.TryGetValue(id, out Stream stream);
            return await Task.FromResult(stream);
        }
        
        /// <summary>
        /// Mocks deleting a photo and its associated image file.
        /// </summary>
        /// <param name="id">The ID of the photo to delete.</param>
        /// <returns>A task that returns the configured delete result.</returns>
        public async Task<bool> DeletePhotoAsync(string id)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            DeletePhotoCallCount++;
            
            if (DeletePhotoResult)
            {
                StoredPhotos.RemoveAll(p => p.Id == id);
                PhotoStreams.Remove(id);
            }
            
            return await Task.FromResult(DeletePhotoResult);
        }
        
        /// <summary>
        /// Mocks deleting photos older than the specified retention period.
        /// </summary>
        /// <param name="retentionDays">The number of days to retain photos. Photos older than this will be deleted if synced.</param>
        /// <returns>A task that returns the configured cleanup result.</returns>
        public async Task<int> CleanupOldPhotosAsync(int retentionDays)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            CleanupOldPhotosCallCount++;
            
            return await Task.FromResult(CleanupOldPhotosResult);
        }
        
        /// <summary>
        /// Mocks retrieving the current storage usage for photos in bytes.
        /// </summary>
        /// <returns>A task that returns the configured storage usage result.</returns>
        public async Task<long> GetStorageUsageAsync()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            GetStorageUsageCallCount++;
            
            return await Task.FromResult(StorageUsageResult);
        }
        
        /// <summary>
        /// Configures the result for the CapturePhotoAsync method.
        /// </summary>
        /// <param name="result">The PhotoModel to be returned by CapturePhotoAsync.</param>
        public void SetupCapturePhotoResult(PhotoModel result)
        {
            CapturePhotoResult = result;
        }
        
        /// <summary>
        /// Configures the result for the DeletePhotoAsync method.
        /// </summary>
        /// <param name="result">The boolean result to be returned by DeletePhotoAsync.</param>
        public void SetupDeletePhotoResult(bool result)
        {
            DeletePhotoResult = result;
        }
        
        /// <summary>
        /// Configures the result for the CleanupOldPhotosAsync method.
        /// </summary>
        /// <param name="result">The number of photos to be reported as deleted.</param>
        public void SetupCleanupOldPhotosResult(int result)
        {
            CleanupOldPhotosResult = result;
        }
        
        /// <summary>
        /// Configures the result for the GetStorageUsageAsync method.
        /// </summary>
        /// <param name="result">The storage usage in bytes to be returned.</param>
        public void SetupStorageUsageResult(long result)
        {
            StorageUsageResult = result;
        }
        
        /// <summary>
        /// Configures a stream to be returned for a specific photo ID.
        /// </summary>
        /// <param name="id">The ID of the photo.</param>
        /// <param name="stream">The stream to be returned for that photo ID.</param>
        public void SetupPhotoStream(string id, Stream stream)
        {
            PhotoStreams[id] = stream;
        }
        
        /// <summary>
        /// Adds a photo to the stored photos collection.
        /// </summary>
        /// <param name="photo">The photo to add.</param>
        public void AddStoredPhoto(PhotoModel photo)
        {
            StoredPhotos.Add(photo);
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
        /// Verifies that CapturePhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyCapturePhotoCalled()
        {
            return CapturePhotoCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetStoredPhotosAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetStoredPhotosCalled()
        {
            return GetStoredPhotosCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetPhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetPhotoCalled()
        {
            return GetPhotoCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetPhotoFileAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetPhotoFileCalled()
        {
            return GetPhotoFileCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that DeletePhotoAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyDeletePhotoCalled()
        {
            return DeletePhotoCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that CleanupOldPhotosAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyCleanupOldPhotosCalled()
        {
            return CleanupOldPhotosCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that GetStorageUsageAsync was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyGetStorageUsageCalled()
        {
            return GetStorageUsageCallCount > 0;
        }
        
        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            StoredPhotos.Clear();
            PhotoStreams.Clear();
            CapturePhotoResult = null;
            DeletePhotoResult = true;
            CleanupOldPhotosResult = 0;
            StorageUsageResult = 0;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            CapturePhotoCallCount = 0;
            GetStoredPhotosCallCount = 0;
            GetPhotoCallCount = 0;
            GetPhotoFileCallCount = 0;
            DeletePhotoCallCount = 0;
            CleanupOldPhotosCallCount = 0;
            GetStorageUsageCallCount = 0;
        }
    }
}