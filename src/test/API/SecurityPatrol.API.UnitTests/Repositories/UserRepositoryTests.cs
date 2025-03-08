using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.Core.Interfaces;
using SecurityPatrol.Core.Models;
using SecurityPatrol.Infrastructure.Persistence;
using SecurityPatrol.Infrastructure.Persistence.Repositories;
using SecurityPatrol.Infrastructure.Persistence.Interceptors;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.API.UnitTests.Setup;

namespace SecurityPatrol.API.UnitTests.Repositories
{
    /// <summary>
    /// Contains unit tests for the UserRepository class to verify its data access operations for User entities.
    /// </summary>
    public class UserRepositoryTests : IDisposable
    {
        private readonly SecurityPatrolDbContext _dbContext;
        private readonly UserRepository _repository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IDateTime> _mockDateTime;

        /// <summary>
        /// Initializes a new instance of the UserRepositoryTests class with an in-memory database context and repository instance.
        /// </summary>
        public UserRepositoryTests()
        {
            // Create a unique database name for each test run to ensure test isolation
            var databaseName = $"SecurityPatrolTest_{Guid.NewGuid()}";
            
            // Configure the in-memory database options
            var options = new DbContextOptionsBuilder<SecurityPatrolDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            
            // Set up mocks for AuditableEntityInterceptor
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(m => m.GetUserId()).Returns(TestConstants.TestUserId);
            
            _mockDateTime = new Mock<IDateTime>();
            _mockDateTime.Setup(m => m.Now).Returns(DateTime.UtcNow);
            
            // Create the interceptor with mocked dependencies
            var auditableEntityInterceptor = new AuditableEntityInterceptor(
                _mockCurrentUserService.Object,
                _mockDateTime.Object);
            
            // Initialize the database context with the in-memory options and interceptor
            _dbContext = new SecurityPatrolDbContext(options, auditableEntityInterceptor);
            
            // Initialize the repository with the database context
            _repository = new UserRepository(_dbContext);
        }

        /// <summary>
        /// Cleans up resources after tests are complete.
        /// </summary>
        public void Dispose()
        {
            _dbContext.Dispose();
        }

        /// <summary>
        /// Tests that GetByIdAsync returns the correct user when given a valid ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsUser()
        {
            // Arrange
            var user = CreateTestUser();
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(user.Id);
        }

        /// <summary>
        /// Tests that GetByIdAsync returns null when given an invalid ID.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var nonExistentId = "non-existent-id";

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetByPhoneNumberAsync returns the correct user when given a valid phone number.
        /// </summary>
        [Fact]
        public async Task GetByPhoneNumberAsync_WithValidPhoneNumber_ReturnsUser()
        {
            // Arrange
            var user = CreateTestUser(phoneNumber: TestConstants.TestPhoneNumber);
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetByPhoneNumberAsync(TestConstants.TestPhoneNumber);

            // Assert
            result.Should().NotBeNull();
            result.PhoneNumber.Should().Be(TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests that GetByPhoneNumberAsync returns null when given an invalid phone number.
        /// </summary>
        [Fact]
        public async Task GetByPhoneNumberAsync_WithInvalidPhoneNumber_ReturnsNull()
        {
            // Arrange
            var nonExistentPhoneNumber = "+15555555556"; // Different from TestConstants.TestPhoneNumber

            // Act
            var result = await _repository.GetByPhoneNumberAsync(nonExistentPhoneNumber);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetAllAsync returns all users in the database.
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange
            var user1 = CreateTestUser(id: "user-1", phoneNumber: "+15555555551");
            var user2 = CreateTestUser(id: "user-2", phoneNumber: "+15555555552");
            var user3 = CreateTestUser(id: "user-3", phoneNumber: "+15555555553");
            
            await _dbContext.Users.AddRangeAsync(user1, user2, user3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain(u => u.Id == "user-1");
            result.Should().Contain(u => u.Id == "user-2");
            result.Should().Contain(u => u.Id == "user-3");
        }

        /// <summary>
        /// Tests that GetPaginatedAsync returns a paginated list of users.
        /// </summary>
        [Fact]
        public async Task GetPaginatedAsync_ReturnsPaginatedUsers()
        {
            // Arrange
            var users = new List<User>();
            for (int i = 1; i <= 20; i++)
            {
                users.Add(CreateTestUser(
                    id: $"user-{i}", 
                    phoneNumber: $"+1555555{i.ToString("D4")}"
                ));
            }
            
            await _dbContext.Users.AddRangeAsync(users);
            await _dbContext.SaveChangesAsync();

            // Act - get page 2 with page size 5
            var result = await _repository.GetPaginatedAsync(2, 5);

            // Assert
            result.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(20);
            result.TotalPages.Should().Be(4);
            
            // Check that we have the correct page of users
            // The repository orders by PhoneNumber, so validate accordingly
            var expectedPhoneNumbers = users
                .OrderBy(u => u.PhoneNumber)
                .Skip(5)
                .Take(5)
                .Select(u => u.PhoneNumber)
                .ToList();
            
            result.Items.Select(u => u.PhoneNumber).Should().BeEquivalentTo(expectedPhoneNumbers);
        }

        /// <summary>
        /// Tests that GetActiveUsersAsync returns only users with IsActive set to true.
        /// </summary>
        [Fact]
        public async Task GetActiveUsersAsync_ReturnsOnlyActiveUsers()
        {
            // Arrange
            var activeUser1 = CreateTestUser(id: "active-1", isActive: true);
            var activeUser2 = CreateTestUser(id: "active-2", isActive: true);
            var inactiveUser = CreateTestUser(id: "inactive-1", isActive: false);
            
            await _dbContext.Users.AddRangeAsync(activeUser1, activeUser2, inactiveUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveUsersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(u => u.IsActive);
            result.Should().Contain(u => u.Id == "active-1");
            result.Should().Contain(u => u.Id == "active-2");
        }

        /// <summary>
        /// Tests that GetRecentlyAuthenticatedAsync returns only users authenticated since the specified time.
        /// </summary>
        [Fact]
        public async Task GetRecentlyAuthenticatedAsync_ReturnsUsersAuthenticatedSinceSpecifiedTime()
        {
            // Arrange
            var referenceTime = DateTime.UtcNow.AddHours(-1);
            
            var recentUser1 = CreateTestUser(
                id: "recent-1", 
                lastAuthenticated: DateTime.UtcNow.AddMinutes(-30)
            );
            
            var recentUser2 = CreateTestUser(
                id: "recent-2", 
                lastAuthenticated: DateTime.UtcNow.AddMinutes(-45)
            );
            
            var oldUser = CreateTestUser(
                id: "old-1", 
                lastAuthenticated: DateTime.UtcNow.AddHours(-2)
            );
            
            await _dbContext.Users.AddRangeAsync(recentUser1, recentUser2, oldUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.GetRecentlyAuthenticatedAsync(referenceTime);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().OnlyContain(u => u.LastAuthenticated >= referenceTime);
            result.Should().Contain(u => u.Id == "recent-1");
            result.Should().Contain(u => u.Id == "recent-2");
        }

        /// <summary>
        /// Tests that AddAsync correctly adds a user to the database.
        /// </summary>
        [Fact]
        public async Task AddAsync_AddsUserToDatabase()
        {
            // Arrange
            var user = CreateTestUser();

            // Act
            var result = await _repository.AddAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            
            var userInDb = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            userInDb.Should().NotBeNull();
        }

        /// <summary>
        /// Tests that UpdateAsync correctly updates an existing user in the database.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_UpdatesExistingUser()
        {
            // Arrange
            var user = CreateTestUser();
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            // Modify user properties
            user.PhoneNumber = "+15555555599";
            user.LastAuthenticated = DateTime.UtcNow.AddHours(-1);
            user.IsActive = false;

            // Act
            await _repository.UpdateAsync(user);
            
            // Get the user from the database to verify changes
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

            // Assert
            updatedUser.Should().NotBeNull();
            updatedUser.PhoneNumber.Should().Be("+15555555599");
            updatedUser.IsActive.Should().BeFalse();
            updatedUser.LastAuthenticated.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Tests that DeleteAsync correctly removes a user from the database.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_RemovesUserFromDatabase()
        {
            // Arrange
            var user = CreateTestUser();
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(user.Id);
            
            // Try to get the deleted user
            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

            // Assert
            deletedUser.Should().BeNull();
        }

        /// <summary>
        /// Tests that DeactivateAsync correctly sets a user's IsActive property to false.
        /// </summary>
        [Fact]
        public async Task DeactivateAsync_SetsUserIsActiveToFalse()
        {
            // Arrange
            var user = CreateTestUser(isActive: true);
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            await _repository.DeactivateAsync(user.Id);
            
            // Get the updated user
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

            // Assert
            updatedUser.Should().NotBeNull();
            updatedUser.IsActive.Should().BeFalse();
        }

        /// <summary>
        /// Tests that UpdateLastAuthenticatedAsync correctly updates a user's LastAuthenticated timestamp.
        /// </summary>
        [Fact]
        public async Task UpdateLastAuthenticatedAsync_UpdatesUserLastAuthenticatedTimestamp()
        {
            // Arrange
            var initialAuthenticated = DateTime.UtcNow.AddDays(-1);
            var user = CreateTestUser(lastAuthenticated: initialAuthenticated);
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            var newAuthenticated = DateTime.UtcNow;

            // Act
            await _repository.UpdateLastAuthenticatedAsync(user.Id, newAuthenticated);
            
            // Get the updated user
            var updatedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

            // Assert
            updatedUser.Should().NotBeNull();
            updatedUser.LastAuthenticated.Should().BeCloseTo(newAuthenticated, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Tests that ExistsAsync returns true when checking for an existing user ID.
        /// </summary>
        [Fact]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var user = CreateTestUser();
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(user.Id);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExistsAsync returns false when checking for a non-existing user ID.
        /// </summary>
        [Fact]
        public async Task ExistsAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistentId = "non-existent-id";

            // Act
            var result = await _repository.ExistsAsync(nonExistentId);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that ExistsByPhoneNumberAsync returns true when checking for an existing phone number.
        /// </summary>
        [Fact]
        public async Task ExistsByPhoneNumberAsync_WithExistingPhoneNumber_ReturnsTrue()
        {
            // Arrange
            var user = CreateTestUser(phoneNumber: TestConstants.TestPhoneNumber);
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsByPhoneNumberAsync(TestConstants.TestPhoneNumber);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that ExistsByPhoneNumberAsync returns false when checking for a non-existing phone number.
        /// </summary>
        [Fact]
        public async Task ExistsByPhoneNumberAsync_WithNonExistingPhoneNumber_ReturnsFalse()
        {
            // Arrange
            var nonExistentPhoneNumber = "+15555555556"; // Different from TestConstants.TestPhoneNumber

            // Act
            var result = await _repository.ExistsByPhoneNumberAsync(nonExistentPhoneNumber);

            // Assert
            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that CountAsync returns the correct number of users in the database.
        /// </summary>
        [Fact]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var user1 = CreateTestUser(id: "user-1");
            var user2 = CreateTestUser(id: "user-2");
            var user3 = CreateTestUser(id: "user-3");
            
            await _dbContext.Users.AddRangeAsync(user1, user2, user3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync();

            // Assert
            result.Should().Be(3);
        }

        /// <summary>
        /// Tests that CountActiveAsync returns the correct number of active users in the database.
        /// </summary>
        [Fact]
        public async Task CountActiveAsync_ReturnsCorrectCountOfActiveUsers()
        {
            // Arrange
            var activeUser1 = CreateTestUser(id: "active-1", isActive: true);
            var activeUser2 = CreateTestUser(id: "active-2", isActive: true);
            var inactiveUser = CreateTestUser(id: "inactive-1", isActive: false);
            
            await _dbContext.Users.AddRangeAsync(activeUser1, activeUser2, inactiveUser);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _repository.CountActiveAsync();

            // Assert
            result.Should().Be(2);
        }

        /// <summary>
        /// Helper method to create a test user with specified or default values.
        /// </summary>
        private User CreateTestUser(
            string id = null, 
            string phoneNumber = null, 
            bool isActive = true, 
            DateTime? lastAuthenticated = null)
        {
            return new User
            {
                Id = id ?? Guid.NewGuid().ToString(),
                PhoneNumber = phoneNumber ?? TestConstants.TestPhoneNumber,
                IsActive = isActive,
                LastAuthenticated = lastAuthenticated ?? DateTime.UtcNow
            };
        }
    }
}