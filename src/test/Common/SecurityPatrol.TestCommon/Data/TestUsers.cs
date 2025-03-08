using System;
using System.Collections.Generic;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SecurityPatrol.Core.Entities;

namespace SecurityPatrol.TestCommon.Data
{
    /// <summary>
    /// Static class providing predefined test user data for use in unit, integration, and UI tests.
    /// </summary>
    public static class TestUsers
    {
        // Mobile user entities
        public static UserEntity DefaultMobileUser { get; private set; }
        public static UserEntity AdminMobileUser { get; private set; }
        public static UserEntity ExpiredTokenMobileUser { get; private set; }
        public static UserEntity NewMobileUser { get; private set; }
        
        // Backend user entities
        public static User DefaultBackendUser { get; private set; }
        public static User AdminBackendUser { get; private set; }
        public static User InactiveBackendUser { get; private set; }
        public static User NewBackendUser { get; private set; }
        
        // Collection of all users for easy access
        public static List<UserEntity> AllMobileUsers { get; private set; }
        public static List<User> AllBackendUsers { get; private set; }
        
        // Pre-defined authentication states
        public static AuthState AuthenticatedState { get; private set; }
        public static AuthState UnauthenticatedState { get; private set; }
        public static AuthState ExpiredState { get; private set; }
        
        /// <summary>
        /// Static constructor that initializes all test user data
        /// </summary>
        static TestUsers()
        {
            // Initialize mobile user entities
            DefaultMobileUser = new UserEntity
            {
                Id = 1,
                UserId = TestConstants.TestUserId,
                PhoneNumber = TestConstants.TestPhoneNumber,
                LastAuthenticated = DateTime.UtcNow,
                AuthToken = TestConstants.TestAuthToken,
                TokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            
            AdminMobileUser = new UserEntity
            {
                Id = 2,
                UserId = "admin_user",
                PhoneNumber = "+15555555556",
                LastAuthenticated = DateTime.UtcNow,
                AuthToken = "admin-auth-token-123",
                TokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            
            ExpiredTokenMobileUser = new UserEntity
            {
                Id = 3,
                UserId = "expired_user",
                PhoneNumber = "+15555555557",
                LastAuthenticated = DateTime.UtcNow.AddDays(-2),
                AuthToken = "expired-auth-token-123",
                TokenExpiry = DateTime.UtcNow.AddDays(-1)
            };
            
            NewMobileUser = new UserEntity
            {
                Id = 4,
                UserId = "new_user",
                PhoneNumber = "+15555555558",
                LastAuthenticated = DateTime.MinValue,
                AuthToken = string.Empty,
                TokenExpiry = DateTime.MinValue
            };
            
            // Initialize backend user entities
            DefaultBackendUser = new User
            {
                Id = TestConstants.TestUserId,
                PhoneNumber = TestConstants.TestPhoneNumber,
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true,
                Created = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "system"
            };
            
            AdminBackendUser = new User
            {
                Id = "admin_user",
                PhoneNumber = "+15555555556",
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true,
                Created = DateTime.UtcNow.AddDays(-60),
                CreatedBy = "system"
            };
            
            InactiveBackendUser = new User
            {
                Id = "inactive_user",
                PhoneNumber = "+15555555559",
                LastAuthenticated = DateTime.UtcNow.AddDays(-14),
                IsActive = false,
                Created = DateTime.UtcNow.AddDays(-45),
                CreatedBy = "system"
            };
            
            NewBackendUser = new User
            {
                Id = "new_user",
                PhoneNumber = "+15555555558",
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true,
                Created = DateTime.UtcNow,
                CreatedBy = "system"
            };
            
            // Initialize collections
            AllMobileUsers = new List<UserEntity>
            {
                DefaultMobileUser,
                AdminMobileUser,
                ExpiredTokenMobileUser,
                NewMobileUser
            };
            
            AllBackendUsers = new List<User>
            {
                DefaultBackendUser,
                AdminBackendUser,
                InactiveBackendUser,
                NewBackendUser
            };
            
            // Initialize authentication states
            AuthenticatedState = AuthState.CreateAuthenticated(TestConstants.TestPhoneNumber);
            UnauthenticatedState = AuthState.CreateUnauthenticated();
            ExpiredState = new AuthState(false, TestConstants.TestPhoneNumber, DateTime.UtcNow.AddDays(-2));
        }
        
        /// <summary>
        /// Gets a mobile user entity by its ID
        /// </summary>
        /// <param name="id">The ID to search for</param>
        /// <returns>The user entity with the specified ID, or null if not found</returns>
        public static UserEntity GetMobileUserById(int id)
        {
            return AllMobileUsers.Find(u => u.Id == id);
        }
        
        /// <summary>
        /// Gets a mobile user entity by its UserId string
        /// </summary>
        /// <param name="userId">The UserId to search for</param>
        /// <returns>The user entity with the specified UserId, or null if not found</returns>
        public static UserEntity GetMobileUserByUserId(string userId)
        {
            return AllMobileUsers.Find(u => u.UserId == userId);
        }
        
        /// <summary>
        /// Gets a backend user entity by its Id string
        /// </summary>
        /// <param name="id">The Id to search for</param>
        /// <returns>The user entity with the specified Id, or null if not found</returns>
        public static User GetBackendUserById(string id)
        {
            return AllBackendUsers.Find(u => u.Id == id);
        }
        
        /// <summary>
        /// Gets a backend user entity by its phone number
        /// </summary>
        /// <param name="phoneNumber">The phone number to search for</param>
        /// <returns>The user entity with the specified phone number, or null if not found</returns>
        public static User GetBackendUserByPhoneNumber(string phoneNumber)
        {
            return AllBackendUsers.Find(u => u.PhoneNumber == phoneNumber);
        }
        
        /// <summary>
        /// Creates a new mobile user entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the user entity</param>
        /// <param name="userId">The UserId for the user entity</param>
        /// <param name="phoneNumber">The phone number for the user entity</param>
        /// <param name="lastAuthenticated">The last authentication time</param>
        /// <param name="authToken">The authentication token</param>
        /// <param name="tokenExpiry">The token expiry time</param>
        /// <returns>A new UserEntity instance with the specified parameters</returns>
        public static UserEntity CreateMobileUserEntity(
            int id, 
            string userId = null, 
            string phoneNumber = null, 
            DateTime? lastAuthenticated = null, 
            string authToken = null, 
            DateTime? tokenExpiry = null)
        {
            return new UserEntity
            {
                Id = id,
                UserId = userId ?? Guid.NewGuid().ToString(),
                PhoneNumber = phoneNumber ?? $"+1{new Random().Next(1000000000, 2000000000)}",
                LastAuthenticated = lastAuthenticated ?? DateTime.UtcNow,
                AuthToken = authToken ?? $"test-token-{Guid.NewGuid()}",
                TokenExpiry = tokenExpiry ?? DateTime.UtcNow.AddDays(1)
            };
        }
        
        /// <summary>
        /// Creates a new backend user entity with the specified parameters
        /// </summary>
        /// <param name="id">The ID for the user</param>
        /// <param name="phoneNumber">The phone number for the user</param>
        /// <param name="lastAuthenticated">The last authentication time</param>
        /// <param name="isActive">Whether the user is active</param>
        /// <returns>A new User instance with the specified parameters</returns>
        public static User CreateBackendUser(
            string id = null, 
            string phoneNumber = null, 
            DateTime? lastAuthenticated = null, 
            bool isActive = true)
        {
            var user = new User
            {
                Id = id ?? Guid.NewGuid().ToString(),
                PhoneNumber = phoneNumber ?? $"+1{new Random().Next(1000000000, 2000000000)}",
                LastAuthenticated = lastAuthenticated ?? DateTime.UtcNow,
                IsActive = isActive,
                Created = DateTime.UtcNow,
                CreatedBy = "test"
            };
            
            // Initialize collections to prevent null reference exceptions
            user.TimeRecords = new List<TimeRecord>();
            user.LocationRecords = new List<LocationRecord>();
            user.Photos = new List<Photo>();
            user.Reports = new List<Report>();
            user.CheckpointVerifications = new List<CheckpointVerification>();
            
            return user;
        }
        
        /// <summary>
        /// Creates a new AuthState instance with the specified parameters
        /// </summary>
        /// <param name="isAuthenticated">Whether the state is authenticated</param>
        /// <param name="phoneNumber">The phone number for the state</param>
        /// <param name="lastAuthenticated">The last authentication time</param>
        /// <returns>A new AuthState instance with the specified parameters</returns>
        public static AuthState CreateAuthState(
            bool isAuthenticated, 
            string phoneNumber = null, 
            DateTime? lastAuthenticated = null)
        {
            if (isAuthenticated && !string.IsNullOrEmpty(phoneNumber))
            {
                return AuthState.CreateAuthenticated(phoneNumber);
            }
            
            if (!isAuthenticated && lastAuthenticated == null)
            {
                return AuthState.CreateUnauthenticated();
            }
            
            return new AuthState(isAuthenticated, phoneNumber, lastAuthenticated);
        }
    }
}