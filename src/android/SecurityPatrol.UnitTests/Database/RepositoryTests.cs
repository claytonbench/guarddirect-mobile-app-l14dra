using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using SQLite;
using SecurityPatrol.Database.Repositories;
using SecurityPatrol.Services;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SecurityPatrol.UnitTests.Helpers;
using SecurityPatrol.UnitTests.Helpers.MockServices;

namespace SecurityPatrol.UnitTests.Database
{
    /// <summary>
    /// Contains unit tests for the TimeRecordRepository class to verify its functionality for storing and retrieving time records.
    /// </summary>
    public class TimeRecordRepositoryTests
    {
        private readonly MockDatabaseService _mockDatabaseService;
        private readonly Mock<ILogger<TimeRecordRepository>> _mockLogger;
        private readonly TimeRecordRepository _repository;

        /// <summary>
        /// Initializes a new instance of the TimeRecordRepositoryTests class with mocked dependencies.
        /// </summary>
        public TimeRecordRepositoryTests()
        {
            _mockDatabaseService = new MockDatabaseService();
            _mockLogger = new Mock<ILogger<TimeRecordRepository>>();
            _repository = new TimeRecordRepository(_mockDatabaseService, _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the test environment before each test.
        /// </summary>
        [Fact]
        public void Setup()
        {
            _mockDatabaseService.Reset();
            _mockLogger.Reset();
        }

        [Fact]
        public async Task SaveTimeRecordAsync_ShouldInsertNewRecord_WhenIdIsZero()
        {
            // Arrange
            var testRecord = TestDataGenerator.CreateTimeRecord(id: 0);
            _mockDatabaseService.SetupNonQueryResult("INSERT INTO TimeRecord", 1); // Simulate successful insert with ID = 1
            
            // Act
            var result = await _repository.SaveTimeRecordAsync(testRecord);
            
            // Assert
            result.Should().Be(1);
            _mockDatabaseService.VerifyNonQueryExecuted("INSERT INTO TimeRecord").Should().BeTrue();
        }

        [Fact]
        public async Task SaveTimeRecordAsync_ShouldUpdateExistingRecord_WhenIdIsNotZero()
        {
            // Arrange
            var testId = 5;
            var testRecord = TestDataGenerator.CreateTimeRecord(id: testId);
            _mockDatabaseService.SetupNonQueryResult("UPDATE TimeRecord", 1); // Simulate successful update
            
            // Act
            var result = await _repository.SaveTimeRecordAsync(testRecord);
            
            // Assert
            result.Should().Be(testId);
            _mockDatabaseService.VerifyNonQueryExecuted("UPDATE TimeRecord").Should().BeTrue();
        }

        [Fact]
        public async Task GetTimeRecordsAsync_ShouldReturnRecords_WhenRecordsExist()
        {
            // Arrange
            int count = 5;
            var testEntities = new List<TimeRecordEntity>();
            for (int i = 1; i <= count; i++)
            {
                testEntities.Add(TestDataGenerator.CreateTimeRecord(id: i).ToEntity());
            }
            
            _mockDatabaseService.SetupQueryResult<TimeRecordEntity>("SELECT * FROM TimeRecord", testEntities);
            
            // Act
            var result = await _repository.GetTimeRecordsAsync(count);
            
            // Assert
            result.Should().HaveCount(count);
            result.First().Id.Should().Be(1);
            result.Last().Id.Should().Be(5);
            _mockDatabaseService.VerifyQueryExecuted("SELECT * FROM TimeRecord").Should().BeTrue();
        }

        [Fact]
        public async Task GetTimeRecordByIdAsync_ShouldReturnRecord_WhenRecordExists()
        {
            // Arrange
            var testId = 1;
            var testEntity = TestDataGenerator.CreateTimeRecord(id: testId).ToEntity();
            
            _mockDatabaseService.SetupQueryResult<TimeRecordEntity>($"SELECT * FROM TimeRecord WHERE Id = {testId}", new List<TimeRecordEntity> { testEntity });
            
            // Act
            var result = await _repository.GetTimeRecordByIdAsync(testId);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(testId);
            result.Type.Should().Be(testEntity.Type);
            _mockDatabaseService.VerifyQueryExecuted($"SELECT * FROM TimeRecord WHERE Id = {testId}").Should().BeTrue();
        }

        [Fact]
        public async Task GetTimeRecordByIdAsync_ShouldReturnNull_WhenRecordDoesNotExist()
        {
            // Arrange
            var testId = 999;
            
            _mockDatabaseService.SetupQueryResult<TimeRecordEntity>($"SELECT * FROM TimeRecord WHERE Id = {testId}", new List<TimeRecordEntity>());
            
            // Act
            var result = await _repository.GetTimeRecordByIdAsync(testId);
            
            // Assert
            result.Should().BeNull();
            _mockDatabaseService.VerifyQueryExecuted($"SELECT * FROM TimeRecord WHERE Id = {testId}").Should().BeTrue();
        }

        [Fact]
        public async Task GetPendingRecordsAsync_ShouldReturnUnsyncedRecords()
        {
            // Arrange
            var testEntities = new List<TimeRecordEntity>
            {
                TestDataGenerator.CreateTimeRecord(id: 1, isSynced: false).ToEntity(),
                TestDataGenerator.CreateTimeRecord(id: 2, isSynced: false).ToEntity(),
                TestDataGenerator.CreateTimeRecord(id: 3, isSynced: false).ToEntity()
            };
            
            _mockDatabaseService.SetupQueryResult<TimeRecordEntity>("SELECT * FROM TimeRecord WHERE IsSynced = 0", testEntities);
            
            // Act
            var result = await _repository.GetPendingRecordsAsync();
            
            // Assert
            result.Should().HaveCount(testEntities.Count);
            result.All(r => !r.IsSynced).Should().BeTrue();
            _mockDatabaseService.VerifyQueryExecuted("SELECT * FROM TimeRecord WHERE IsSynced = 0").Should().BeTrue();
        }

        [Fact]
        public async Task UpdateSyncStatusAsync_ShouldUpdateSyncStatus()
        {
            // Arrange
            var testId = 1;
            var isSynced = true;
            
            _mockDatabaseService.SetupNonQueryResult("UPDATE TimeRecord SET IsSynced = ? WHERE Id = ?", 1);
            
            // Act
            var result = await _repository.UpdateSyncStatusAsync(testId, isSynced);
            
            // Assert
            result.Should().Be(1);
            _mockDatabaseService.VerifyNonQueryExecuted("UPDATE TimeRecord SET IsSynced = ? WHERE Id = ?").Should().BeTrue();
        }

        [Fact]
        public async Task DeleteTimeRecordAsync_ShouldDeleteRecord()
        {
            // Arrange
            var testId = 1;
            
            _mockDatabaseService.SetupNonQueryResult("DELETE FROM TimeRecord WHERE Id = ?", 1);
            
            // Act
            var result = await _repository.DeleteTimeRecordAsync(testId);
            
            // Assert
            result.Should().Be(1);
            _mockDatabaseService.VerifyNonQueryExecuted("DELETE FROM TimeRecord WHERE Id = ?").Should().BeTrue();
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_ShouldDeleteOldSyncedRecords()
        {
            // Arrange
            var retentionDays = 90;
            
            _mockDatabaseService.SetupNonQueryResult("DELETE FROM TimeRecord WHERE Timestamp < ? AND IsSynced = 1", 5);
            
            // Act
            var result = await _repository.CleanupOldRecordsAsync(retentionDays);
            
            // Assert
            result.Should().Be(5);
            _mockDatabaseService.VerifyNonQueryExecuted("DELETE FROM TimeRecord WHERE Timestamp < ? AND IsSynced = 1").Should().BeTrue();
        }

        [Fact]
        public async Task Repository_ShouldHandleDatabaseExceptions()
        {
            // Arrange
            var testId = 1;
            var exception = new SQLiteException(SQLiteResult.Error, "Test database exception");
            
            _mockDatabaseService.SetupQueryException($"SELECT * FROM TimeRecord WHERE Id = {testId}", exception);
            
            // Act & Assert
            await Assert.ThrowsAsync<SQLiteException>(() => _repository.GetTimeRecordByIdAsync(testId));
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error retrieving time record")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    /// <summary>
    /// Contains unit tests for the BaseRepository class to verify its core functionality for database operations.
    /// </summary>
    public class BaseRepositoryTests
    {
        private readonly MockDatabaseService _mockDatabaseService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly TestRepository _repository;

        /// <summary>
        /// Initializes a new instance of the BaseRepositoryTests class with mocked dependencies.
        /// </summary>
        public BaseRepositoryTests()
        {
            _mockDatabaseService = new MockDatabaseService();
            _mockLogger = new Mock<ILogger>();
            _repository = new TestRepository(_mockDatabaseService, _mockLogger.Object);
        }

        /// <summary>
        /// Sets up the test environment before each test.
        /// </summary>
        [Fact]
        public void Setup()
        {
            _mockDatabaseService.Reset();
            _mockLogger.Reset();
        }

        [Fact]
        public async Task GetConnectionAsync_ShouldReturnConnection()
        {
            // Arrange
            var connection = new SQLiteAsyncConnection(":memory:");
            _mockDatabaseService.SetupQueryResult<TimeRecordEntity>("SELECT * FROM TimeRecord", new List<TimeRecordEntity>());
            
            // Act
            var result = await _repository.GetConnectionAsyncPublic();
            
            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_ShouldExecuteActionInTransaction()
        {
            // Arrange
            bool actionExecuted = false;
            
            // Act
            await _repository.ExecuteInTransactionAsyncPublic(async () => 
            {
                actionExecuted = true;
                await Task.CompletedTask;
            });
            
            // Assert
            actionExecuted.Should().BeTrue();
            _mockDatabaseService.TransactionBeginCount.Should().Be(1);
            _mockDatabaseService.TransactionCommitCount.Should().Be(1);
            _mockDatabaseService.TransactionRollbackCount.Should().Be(0);
        }
    }

    /// <summary>
    /// A concrete implementation of BaseRepository for testing purposes.
    /// </summary>
    public class TestRepository : BaseRepository<TimeRecordEntity, TimeRecordModel>
    {
        /// <summary>
        /// Initializes a new instance of the TestRepository class with the specified dependencies.
        /// </summary>
        /// <param name="databaseService">The database service for data access operations</param>
        /// <param name="logger">The logger for recording repository activities</param>
        public TestRepository(IDatabaseService databaseService, ILogger logger)
            : base(databaseService, logger)
        {
        }

        /// <summary>
        /// Converts a TimeRecordEntity to a TimeRecordModel.
        /// </summary>
        /// <param name="entity">The entity to convert</param>
        /// <returns>The entity converted to a model</returns>
        protected override TimeRecordModel ConvertToModel(TimeRecordEntity entity)
        {
            return TimeRecordModel.FromEntity(entity);
        }

        /// <summary>
        /// Converts a TimeRecordModel to a TimeRecordEntity.
        /// </summary>
        /// <param name="model">The model to convert</param>
        /// <returns>The model converted to an entity</returns>
        protected override TimeRecordEntity ConvertToEntity(TimeRecordModel model)
        {
            return model.ToEntity();
        }

        /// <summary>
        /// Public wrapper for the protected GetConnectionAsync method for testing.
        /// </summary>
        /// <returns>A task that returns the database connection</returns>
        public Task<SQLiteAsyncConnection> GetConnectionAsyncPublic()
        {
            return GetConnectionAsync();
        }

        /// <summary>
        /// Public wrapper for the protected ExecuteInTransactionAsync method for testing.
        /// </summary>
        /// <param name="action">The action to execute within the transaction</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task ExecuteInTransactionAsyncPublic(Func<Task> action)
        {
            return ExecuteInTransactionAsync(action);
        }
    }
}