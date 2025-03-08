using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.TestCommon.Helpers;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test photo data for use in unit, integration, and UI tests
    /// across the Security Patrol application.
    /// </summary>
    public static class TestPhotos
    {
        // Predefined PhotoEntity instances
        public static PhotoEntity DefaultPhotoEntity { get; private set; }
        public static PhotoEntity SyncedPhotoEntity { get; private set; }
        public static PhotoEntity UnsyncedPhotoEntity { get; private set; }
        public static PhotoEntity InProgressPhotoEntity { get; private set; }
        
        // Predefined PhotoModel instances
        public static PhotoModel DefaultPhotoModel { get; private set; }
        public static PhotoModel SyncedPhotoModel { get; private set; }
        public static PhotoModel UnsyncedPhotoModel { get; private set; }
        public static PhotoModel InProgressPhotoModel { get; private set; }
        
        // Collections of all predefined photos
        public static List<PhotoEntity> AllPhotoEntities { get; private set; }
        public static List<PhotoModel> AllPhotoModels { get; private set; }
        
        // Test image file paths
        public static string DefaultTestImagePath { get; private set; }
        public static string LargeTestImagePath { get; private set; }
        public static string SmallTestImagePath { get; private set; }
        
        /// <summary>
        /// Static constructor that initializes all test photo data
        /// </summary>
        static TestPhotos()
        {
            // Initialize test image paths
            DefaultTestImagePath = Path.Combine(Path.GetTempPath(), "SecurityPatrolTests", "test_photo.jpg");
            LargeTestImagePath = Path.Combine(Path.GetTempPath(), "SecurityPatrolTests", "large_test_photo.jpg");
            SmallTestImagePath = Path.Combine(Path.GetTempPath(), "SecurityPatrolTests", "small_test_photo.jpg");
            
            // Initialize photo entities
            DefaultPhotoEntity = new PhotoEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = DefaultTestImagePath,
                IsSynced = false,
                SyncProgress = 0,
                RemoteId = null
            };
            
            SyncedPhotoEntity = new PhotoEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = DefaultTestImagePath,
                IsSynced = true,
                SyncProgress = 100,
                RemoteId = "photo_synced_1"
            };
            
            UnsyncedPhotoEntity = new PhotoEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = DefaultTestImagePath,
                IsSynced = false,
                SyncProgress = 0,
                RemoteId = null
            };
            
            InProgressPhotoEntity = new PhotoEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = DefaultTestImagePath,
                IsSynced = false,
                SyncProgress = 50,
                RemoteId = null
            };
            
            // Initialize photo models from entities
            DefaultPhotoModel = PhotoModel.FromEntity(DefaultPhotoEntity);
            SyncedPhotoModel = PhotoModel.FromEntity(SyncedPhotoEntity);
            UnsyncedPhotoModel = PhotoModel.FromEntity(UnsyncedPhotoEntity);
            InProgressPhotoModel = PhotoModel.FromEntity(InProgressPhotoEntity);
            
            // Populate collections
            AllPhotoEntities = new List<PhotoEntity>
            {
                DefaultPhotoEntity,
                SyncedPhotoEntity,
                UnsyncedPhotoEntity,
                InProgressPhotoEntity
            };
            
            AllPhotoModels = new List<PhotoModel>
            {
                DefaultPhotoModel,
                SyncedPhotoModel,
                UnsyncedPhotoModel,
                InProgressPhotoModel
            };
        }
        
        /// <summary>
        /// Gets a photo entity by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The photo entity with the specified ID, or null if not found</returns>
        public static PhotoEntity GetPhotoEntityById(string id)
        {
            return AllPhotoEntities.Find(p => p.Id == id);
        }
        
        /// <summary>
        /// Gets a photo model by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The photo model with the specified ID, or null if not found</returns>
        public static PhotoModel GetPhotoModelById(string id)
        {
            return AllPhotoModels.Find(p => p.Id == id);
        }
        
        /// <summary>
        /// Creates a new photo entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the photo entity</param>
        /// <param name="userId">The user ID for the photo entity</param>
        /// <param name="filePath">The file path for the photo entity</param>
        /// <param name="isSynced">Whether the photo is synced</param>
        /// <param name="syncProgress">The sync progress percentage</param>
        /// <param name="remoteId">The remote ID for the photo entity</param>
        /// <returns>A new PhotoEntity instance with the specified parameters</returns>
        public static PhotoEntity CreatePhotoEntity(
            string id = null,
            string userId = null,
            string filePath = null,
            bool isSynced = false,
            int syncProgress = 0,
            string remoteId = null)
        {
            return new PhotoEntity
            {
                Id = id ?? Guid.NewGuid().ToString(),
                UserId = userId ?? TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = filePath ?? DefaultTestImagePath,
                IsSynced = isSynced,
                SyncProgress = syncProgress,
                RemoteId = remoteId
            };
        }
        
        /// <summary>
        /// Creates a new photo model with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the photo model</param>
        /// <param name="userId">The user ID for the photo model</param>
        /// <param name="filePath">The file path for the photo model</param>
        /// <param name="isSynced">Whether the photo is synced</param>
        /// <param name="syncProgress">The sync progress percentage</param>
        /// <param name="remoteId">The remote ID for the photo model</param>
        /// <returns>A new PhotoModel instance with the specified parameters</returns>
        public static PhotoModel CreatePhotoModel(
            string id = null,
            string userId = null,
            string filePath = null,
            bool isSynced = false,
            int syncProgress = 0,
            string remoteId = null)
        {
            return new PhotoModel
            {
                Id = id ?? Guid.NewGuid().ToString(),
                UserId = userId ?? TestConstants.TestUserId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude,
                FilePath = filePath ?? DefaultTestImagePath,
                IsSynced = isSynced,
                SyncProgress = syncProgress,
                RemoteId = remoteId
            };
        }
        
        /// <summary>
        /// Creates a list of photo entities with varying sync states
        /// </summary>
        /// <param name="count">The number of photo entities to create</param>
        /// <param name="includeMixedSyncStates">Whether to include a mix of synced, unsynced, and in-progress photos</param>
        /// <returns>A list of PhotoEntity instances with varying sync states</returns>
        public static List<PhotoEntity> CreatePhotoEntities(int count, bool includeMixedSyncStates = true)
        {
            var photos = new List<PhotoEntity>();
            
            for (int i = 1; i <= count; i++)
            {
                bool isSynced = false;
                int syncProgress = 0;
                string remoteId = null;
                
                if (includeMixedSyncStates)
                {
                    // Create a mix of sync states
                    int syncState = i % 3;
                    
                    if (syncState == 0) // Synced
                    {
                        isSynced = true;
                        syncProgress = 100;
                        remoteId = $"photo_remote_{i}";
                    }
                    else if (syncState == 1) // Unsynced
                    {
                        isSynced = false;
                        syncProgress = 0;
                        remoteId = null;
                    }
                    else // In progress
                    {
                        isSynced = false;
                        syncProgress = i * 10 % 100; // 10, 20, 30, ... 90
                        remoteId = null;
                    }
                }
                
                var photo = CreatePhotoEntity(
                    Guid.NewGuid().ToString(),
                    TestConstants.TestUserId,
                    DefaultTestImagePath,
                    isSynced,
                    syncProgress,
                    remoteId);
                
                photos.Add(photo);
            }
            
            return photos;
        }
        
        /// <summary>
        /// Creates a list of photo models with varying sync states
        /// </summary>
        /// <param name="count">The number of photo models to create</param>
        /// <param name="includeMixedSyncStates">Whether to include a mix of synced, unsynced, and in-progress photos</param>
        /// <returns>A list of PhotoModel instances with varying sync states</returns>
        public static List<PhotoModel> CreatePhotoModels(int count, bool includeMixedSyncStates = true)
        {
            var photos = new List<PhotoModel>();
            
            for (int i = 1; i <= count; i++)
            {
                bool isSynced = false;
                int syncProgress = 0;
                string remoteId = null;
                
                if (includeMixedSyncStates)
                {
                    // Create a mix of sync states
                    int syncState = i % 3;
                    
                    if (syncState == 0) // Synced
                    {
                        isSynced = true;
                        syncProgress = 100;
                        remoteId = $"photo_remote_{i}";
                    }
                    else if (syncState == 1) // Unsynced
                    {
                        isSynced = false;
                        syncProgress = 0;
                        remoteId = null;
                    }
                    else // In progress
                    {
                        isSynced = false;
                        syncProgress = i * 10 % 100; // 10, 20, 30, ... 90
                        remoteId = null;
                    }
                }
                
                var photo = CreatePhotoModel(
                    Guid.NewGuid().ToString(),
                    TestConstants.TestUserId,
                    DefaultTestImagePath,
                    isSynced,
                    syncProgress,
                    remoteId);
                
                photos.Add(photo);
            }
            
            return photos;
        }
        
        /// <summary>
        /// Generates a test image file at the specified path
        /// </summary>
        /// <param name="filePath">The path where the image should be saved</param>
        /// <param name="width">The width of the image in pixels</param>
        /// <param name="height">The height of the image in pixels</param>
        /// <returns>A task that returns true if the file was created successfully, false otherwise</returns>
        public static async Task<bool> GenerateTestPhotoFileAsync(string filePath, int width = 640, int height = 480)
        {
            try
            {
                return await MockDataGenerator.CreateTestPhotoFileAsync(filePath, width, height);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Generates multiple test image files with different sizes
        /// </summary>
        /// <param name="count">The number of files to generate</param>
        /// <param name="directoryPath">The directory where files should be saved</param>
        /// <returns>A task that returns a list of file paths for the generated images</returns>
        public static async Task<List<string>> GenerateTestPhotoFilesAsync(int count, string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            var filePaths = new List<string>();
            
            for (int i = 1; i <= count; i++)
            {
                string fileName = $"test_photo_{i}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
                string filePath = Path.Combine(directoryPath, fileName);
                
                // Vary the dimensions based on index
                int width = 640 + (i % 3) * 100; // 640, 740, 840
                int height = 480 + (i % 2) * 120; // 480, 600
                
                bool success = await GenerateTestPhotoFileAsync(filePath, width, height);
                
                if (success)
                {
                    filePaths.Add(filePath);
                }
            }
            
            return filePaths;
        }
        
        /// <summary>
        /// Ensures that test photo files exist for the predefined test photos
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task EnsureTestPhotosExistAsync()
        {
            // Ensure directories exist
            string directory = Path.GetDirectoryName(DefaultTestImagePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create default test photo if it doesn't exist
            if (!File.Exists(DefaultTestImagePath))
            {
                await GenerateTestPhotoFileAsync(DefaultTestImagePath);
            }
            
            // Create large test photo if it doesn't exist
            if (!File.Exists(LargeTestImagePath))
            {
                await GenerateTestPhotoFileAsync(LargeTestImagePath, 1920, 1080);
            }
            
            // Create small test photo if it doesn't exist
            if (!File.Exists(SmallTestImagePath))
            {
                await GenerateTestPhotoFileAsync(SmallTestImagePath, 320, 240);
            }
        }
    }
}