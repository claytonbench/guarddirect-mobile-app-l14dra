using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SQLite;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Database.Entities;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Data;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UnitTests.Database
{
    public class TimeRecordRepositoryTests : TestBase
    {
        public TimeRecordRepositoryTests() : base()
        {
        }

        public void Setup()
        {
            SetupMockDatabaseService();

            // Configure MockDatabaseService to return a mock SQLiteAsyncConnection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure MockDatabaseService.RunInTransactionAsync to execute the provided action
            MockDatabaseService.Setup(m => m.RunInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> action) => action());
        }

        [Fact]
        public async Task SaveTimeRecord_ShouldInsertNewRecord_WhenIdIsZero()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return a new ID when Insert is called
            mockConnection.Setup(m => m.InsertAsync(It.IsAny<TimeRecordEntity>()))
                .ReturnsAsync(1);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Create a model with Id = 0
            var model = MockDataGenerator.CreateTimeRecordModel(0, "ClockIn", false);
            
            // Act
            var result = await repository.SaveTimeRecordAsync(model);
            
            // Assert
            result.Should().BeGreaterThan(0);
            mockConnection.Verify(m => m.InsertAsync(It.IsAny<TimeRecordEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task SaveTimeRecord_ShouldUpdateExistingRecord_WhenIdIsNotZero()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return 1 when Update is called
            mockConnection.Setup(m => m.UpdateAsync(It.IsAny<TimeRecordEntity>()))
                .ReturnsAsync(1);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Create a model with Id = 1
            var model = MockDataGenerator.CreateTimeRecordModel(1, "ClockIn", false);
            
            // Act
            var result = await repository.SaveTimeRecordAsync(model);
            
            // Assert
            result.Should().Be(1);
            mockConnection.Verify(m => m.UpdateAsync(It.IsAny<TimeRecordEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetTimeRecords_ShouldReturnRecords_WhenRecordsExist()
        {
            // Arrange
            Setup();
            
            // Create a list of test TimeRecordEntity objects
            var testEntities = new List<TimeRecordEntity>
            {
                MockDataGenerator.CreateTimeRecordEntity(1, TestConstants.TestUserId, "ClockIn", false),
                MockDataGenerator.CreateTimeRecordEntity(2, TestConstants.TestUserId, "ClockOut", true)
            };
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entities when queried
            var mockTable = new Mock<TableQuery<TimeRecordEntity>>();
            mockConnection.Setup(m => m.Table<TimeRecordEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.OrderByDescending(It.IsAny<Func<TimeRecordEntity, object>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Take(It.IsAny<int>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(testEntities);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.GetTimeRecordsAsync(10);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
        }
        
        [Fact]
        public async Task GetTimeRecordById_ShouldReturnRecord_WhenRecordExists()
        {
            // Arrange
            Setup();
            
            // Create a test TimeRecordEntity with Id = 1
            var testEntity = MockDataGenerator.CreateTimeRecordEntity(1, TestConstants.TestUserId, "ClockIn", false);
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entity when FindAsync is called with Id = 1
            mockConnection.Setup(m => m.FindAsync<TimeRecordEntity>(1))
                .ReturnsAsync(testEntity);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.GetTimeRecordByIdAsync(1);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.UserId.Should().Be(TestConstants.TestUserId);
            result.Type.Should().Be("ClockIn");
        }
        
        [Fact]
        public async Task GetTimeRecordById_ShouldReturnNull_WhenRecordDoesNotExist()
        {
            // Arrange
            Setup();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return null when FindAsync is called
            mockConnection.Setup(m => m.FindAsync<TimeRecordEntity>(999))
                .ReturnsAsync((TimeRecordEntity)null);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.GetTimeRecordByIdAsync(999);
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact]
        public async Task GetPendingRecords_ShouldReturnUnsyncedRecords()
        {
            // Arrange
            Setup();
            
            // Create a list of test TimeRecordEntity objects with some having IsSynced = false
            var allEntities = new List<TimeRecordEntity>
            {
                MockDataGenerator.CreateTimeRecordEntity(1, TestConstants.TestUserId, "ClockIn", false),
                MockDataGenerator.CreateTimeRecordEntity(2, TestConstants.TestUserId, "ClockOut", true),
                MockDataGenerator.CreateTimeRecordEntity(3, TestConstants.TestUserId, "ClockIn", false)
            };
            
            var unsyncedEntities = allEntities.Where(e => !e.IsSynced).ToList();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the unsynced entities when queried
            var mockTable = new Mock<TableQuery<TimeRecordEntity>>();
            mockConnection.Setup(m => m.Table<TimeRecordEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Where(It.IsAny<Func<TimeRecordEntity, bool>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(unsyncedEntities);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.GetPendingRecordsAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => !r.IsSynced).Should().BeTrue();
        }
        
        [Fact]
        public async Task UpdateSyncStatus_ShouldUpdateSyncFlag()
        {
            // Arrange
            Setup();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return 1 when ExecuteAsync is called
            mockConnection.Setup(m => m.ExecuteAsync(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync(1);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.UpdateSyncStatusAsync(1, true);
            
            // Assert
            result.Should().Be(1);
            mockConnection.Verify(m => m.ExecuteAsync(It.IsAny<string>(), true, 1), Times.Once);
        }
        
        [Fact]
        public async Task DeleteTimeRecord_ShouldDeleteRecord()
        {
            // Arrange
            Setup();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return 1 when DeleteAsync is called
            mockConnection.Setup(m => m.DeleteAsync<TimeRecordEntity>(1))
                .ReturnsAsync(1);
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act
            var result = await repository.DeleteTimeRecordAsync(1);
            
            // Assert
            result.Should().Be(1);
            mockConnection.Verify(m => m.DeleteAsync<TimeRecordEntity>(1), Times.Once);
        }
        
        [Fact]
        public async Task Repository_ShouldHandleExceptions_WhenDatabaseOperationsFail()
        {
            // Arrange
            Setup();
            
            // Configure MockDatabaseService.GetConnectionAsync to throw an SQLiteException
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ThrowsAsync(new SQLiteException(SQLite3.Result.Error, "Test database error"));
            
            // Create a TimeRecordRepository with the mock database service
            var repository = new TimeRecordRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<TimeRecordRepository>>());
            
            // Act/Assert
            await Assert.ThrowsAsync<SQLiteException>(() => repository.GetTimeRecordsAsync(10));
            await Assert.ThrowsAsync<SQLiteException>(() => repository.GetTimeRecordByIdAsync(1));
            await Assert.ThrowsAsync<SQLiteException>(() => repository.SaveTimeRecordAsync(new TimeRecordModel()));
            await Assert.ThrowsAsync<SQLiteException>(() => repository.DeleteTimeRecordAsync(1));
        }
    }
    
    public class LocationRepositoryTests : TestBase
    {
        public LocationRepositoryTests() : base()
        {
        }
        
        public void Setup()
        {
            SetupMockDatabaseService();
            
            // Configure MockDatabaseService to return a mock SQLiteAsyncConnection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure MockDatabaseService.RunInTransactionAsync to execute the provided action
            MockDatabaseService.Setup(m => m.RunInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> action) => action());
        }
        
        [Fact]
        public async Task SaveLocationRecord_ShouldInsertNewRecord_WhenIdIsZero()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return a new ID when Insert is called
            mockConnection.Setup(m => m.InsertAsync(It.IsAny<LocationRecordEntity>()))
                .ReturnsAsync(1);
            
            // Create a LocationRepository with the mock database service
            var repository = new LocationRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<LocationRepository>>());
            
            // Create a model with Id = 0
            var model = MockDataGenerator.CreateLocationModel(0);
            
            // Act
            var result = await repository.SaveLocationRecordAsync(model);
            
            // Assert
            result.Should().BeGreaterThan(0);
            mockConnection.Verify(m => m.InsertAsync(It.IsAny<LocationRecordEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetLocationRecords_ShouldReturnRecords_WhenRecordsExist()
        {
            // Arrange
            Setup();
            
            // Create a list of test LocationRecordEntity objects
            var testEntities = new List<LocationRecordEntity>
            {
                MockDataGenerator.CreateLocationRecordEntity(1, TestConstants.TestUserId),
                MockDataGenerator.CreateLocationRecordEntity(2, TestConstants.TestUserId)
            };
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entities when queried
            var mockTable = new Mock<TableQuery<LocationRecordEntity>>();
            mockConnection.Setup(m => m.Table<LocationRecordEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.OrderByDescending(It.IsAny<Func<LocationRecordEntity, object>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Take(It.IsAny<int>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(testEntities);
            
            // Create a LocationRepository with the mock database service
            var repository = new LocationRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<LocationRepository>>());
            
            // Act
            var result = await repository.GetLocationRecordsAsync(10);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
        }
        
        [Fact]
        public async Task GetPendingLocationRecords_ShouldReturnUnsyncedRecords()
        {
            // Arrange
            Setup();
            
            // Create a list of test LocationRecordEntity objects with some having IsSynced = false
            var allEntities = new List<LocationRecordEntity>
            {
                MockDataGenerator.CreateLocationRecordEntity(1, TestConstants.TestUserId, false),
                MockDataGenerator.CreateLocationRecordEntity(2, TestConstants.TestUserId, true),
                MockDataGenerator.CreateLocationRecordEntity(3, TestConstants.TestUserId, false)
            };
            
            var unsyncedEntities = allEntities.Where(e => !e.IsSynced).ToList();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the unsynced entities when queried
            var mockTable = new Mock<TableQuery<LocationRecordEntity>>();
            mockConnection.Setup(m => m.Table<LocationRecordEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Where(It.IsAny<Func<LocationRecordEntity, bool>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(unsyncedEntities);
            
            // Create a LocationRepository with the mock database service
            var repository = new LocationRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<LocationRepository>>());
            
            // Act
            var result = await repository.GetPendingLocationRecordsAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => !r.IsSynced).Should().BeTrue();
        }
    }
    
    public class CheckpointRepositoryTests : TestBase
    {
        public CheckpointRepositoryTests() : base()
        {
        }
        
        public void Setup()
        {
            SetupMockDatabaseService();
            
            // Configure MockDatabaseService to return a mock SQLiteAsyncConnection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure MockDatabaseService.RunInTransactionAsync to execute the provided action
            MockDatabaseService.Setup(m => m.RunInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> action) => action());
        }
        
        [Fact]
        public async Task GetCheckpoints_ShouldReturnCheckpoints_WhenCheckpointsExist()
        {
            // Arrange
            Setup();
            
            // Create a list of test CheckpointEntity objects
            var testEntities = new List<CheckpointEntity>
            {
                MockDataGenerator.CreateCheckpointEntity(1, 1, "Checkpoint 1"),
                MockDataGenerator.CreateCheckpointEntity(2, 1, "Checkpoint 2")
            };
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entities when queried
            var mockTable = new Mock<TableQuery<CheckpointEntity>>();
            mockConnection.Setup(m => m.Table<CheckpointEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Where(It.IsAny<Func<CheckpointEntity, bool>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(testEntities);
            
            // Create a CheckpointRepository with the mock database service
            var repository = new CheckpointRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<CheckpointRepository>>());
            
            // Act
            var result = await repository.GetCheckpointsAsync(1);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
        }
        
        [Fact]
        public async Task SaveCheckpointVerification_ShouldInsertNewVerification()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return a new ID when Insert is called
            mockConnection.Setup(m => m.InsertAsync(It.IsAny<CheckpointVerificationEntity>()))
                .ReturnsAsync(1);
            
            // Create a CheckpointRepository with the mock database service
            var repository = new CheckpointRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<CheckpointRepository>>());
            
            // Create a verification model
            var model = new CheckpointVerificationModel
            {
                UserId = TestConstants.TestUserId,
                CheckpointId = TestConstants.TestCheckpointId,
                Timestamp = DateTime.UtcNow,
                Latitude = TestConstants.TestLatitude,
                Longitude = TestConstants.TestLongitude
            };
            
            // Act
            var result = await repository.SaveCheckpointVerificationAsync(model);
            
            // Assert
            result.Should().BeGreaterThan(0);
            mockConnection.Verify(m => m.InsertAsync(It.IsAny<CheckpointVerificationEntity>()), Times.Once);
        }
    }
    
    public class PhotoRepositoryTests : TestBase
    {
        public PhotoRepositoryTests() : base()
        {
        }
        
        public void Setup()
        {
            SetupMockDatabaseService();
            
            // Configure MockDatabaseService to return a mock SQLiteAsyncConnection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure MockDatabaseService.RunInTransactionAsync to execute the provided action
            MockDatabaseService.Setup(m => m.RunInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> action) => action());
        }
        
        [Fact]
        public async Task SavePhoto_ShouldInsertNewPhoto_WhenIdIsEmpty()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return a new ID when Insert is called
            mockConnection.Setup(m => m.InsertAsync(It.IsAny<PhotoEntity>()))
                .ReturnsAsync(1);
            
            // Create a PhotoRepository with the mock database service
            var repository = new PhotoRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<PhotoRepository>>());
            
            // Create a model with empty Id
            var model = MockDataGenerator.CreatePhotoModel(string.Empty);
            
            // Act
            var result = await repository.SavePhotoAsync(model);
            
            // Assert
            result.Should().NotBeNullOrEmpty();
            mockConnection.Verify(m => m.InsertAsync(It.IsAny<PhotoEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetPhotos_ShouldReturnPhotos_WhenPhotosExist()
        {
            // Arrange
            Setup();
            
            // Create a list of test PhotoEntity objects
            var testEntities = new List<PhotoEntity>
            {
                MockDataGenerator.CreatePhotoEntity("1", TestConstants.TestUserId),
                MockDataGenerator.CreatePhotoEntity("2", TestConstants.TestUserId)
            };
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entities when queried
            var mockTable = new Mock<TableQuery<PhotoEntity>>();
            mockConnection.Setup(m => m.Table<PhotoEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.OrderByDescending(It.IsAny<Func<PhotoEntity, object>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Take(It.IsAny<int>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(testEntities);
            
            // Create a PhotoRepository with the mock database service
            var repository = new PhotoRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<PhotoRepository>>());
            
            // Act
            var result = await repository.GetPhotosAsync(10);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be("1");
            result[1].Id.Should().Be("2");
        }
        
        [Fact]
        public async Task GetPendingPhotos_ShouldReturnUnsyncedPhotos()
        {
            // Arrange
            Setup();
            
            // Create a list of test PhotoEntity objects with some having IsSynced = false
            var allEntities = new List<PhotoEntity>
            {
                MockDataGenerator.CreatePhotoEntity("1", TestConstants.TestUserId, null, false),
                MockDataGenerator.CreatePhotoEntity("2", TestConstants.TestUserId, null, true),
                MockDataGenerator.CreatePhotoEntity("3", TestConstants.TestUserId, null, false)
            };
            
            var unsyncedEntities = allEntities.Where(e => !e.IsSynced).ToList();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the unsynced entities when queried
            var mockTable = new Mock<TableQuery<PhotoEntity>>();
            mockConnection.Setup(m => m.Table<PhotoEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Where(It.IsAny<Func<PhotoEntity, bool>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(unsyncedEntities);
            
            // Create a PhotoRepository with the mock database service
            var repository = new PhotoRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<PhotoRepository>>());
            
            // Act
            var result = await repository.GetPendingPhotosAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => !r.IsSynced).Should().BeTrue();
        }
    }
    
    public class ReportRepositoryTests : TestBase
    {
        public ReportRepositoryTests() : base()
        {
        }
        
        public void Setup()
        {
            SetupMockDatabaseService();
            
            // Configure MockDatabaseService to return a mock SQLiteAsyncConnection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure MockDatabaseService.RunInTransactionAsync to execute the provided action
            MockDatabaseService.Setup(m => m.RunInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns((Func<Task> action) => action());
        }
        
        [Fact]
        public async Task SaveReport_ShouldInsertNewReport_WhenIdIsZero()
        {
            // Arrange
            Setup();
            
            // Create a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return a new ID when Insert is called
            mockConnection.Setup(m => m.InsertAsync(It.IsAny<ReportEntity>()))
                .ReturnsAsync(1);
            
            // Create a ReportRepository with the mock database service
            var repository = new ReportRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<ReportRepository>>());
            
            // Create a model with Id = 0
            var model = MockDataGenerator.CreateReportModel(0);
            
            // Act
            var result = await repository.SaveReportAsync(model);
            
            // Assert
            result.Should().BeGreaterThan(0);
            mockConnection.Verify(m => m.InsertAsync(It.IsAny<ReportEntity>()), Times.Once);
        }
        
        [Fact]
        public async Task GetReports_ShouldReturnReports_WhenReportsExist()
        {
            // Arrange
            Setup();
            
            // Create a list of test ReportEntity objects
            var testEntities = new List<ReportEntity>
            {
                MockDataGenerator.CreateReportEntity(1, TestConstants.TestUserId, "Test Report 1"),
                MockDataGenerator.CreateReportEntity(2, TestConstants.TestUserId, "Test Report 2")
            };
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the test entities when queried
            var mockTable = new Mock<TableQuery<ReportEntity>>();
            mockConnection.Setup(m => m.Table<ReportEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.OrderByDescending(It.IsAny<Func<ReportEntity, object>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Take(It.IsAny<int>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(testEntities);
            
            // Create a ReportRepository with the mock database service
            var repository = new ReportRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<ReportRepository>>());
            
            // Act
            var result = await repository.GetReportsAsync(10);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[1].Id.Should().Be(2);
        }
        
        [Fact]
        public async Task GetPendingReports_ShouldReturnUnsyncedReports()
        {
            // Arrange
            Setup();
            
            // Create a list of test ReportEntity objects with some having IsSynced = false
            var allEntities = new List<ReportEntity>
            {
                MockDataGenerator.CreateReportEntity(1, TestConstants.TestUserId, "Report 1", false),
                MockDataGenerator.CreateReportEntity(2, TestConstants.TestUserId, "Report 2", true),
                MockDataGenerator.CreateReportEntity(3, TestConstants.TestUserId, "Report 3", false)
            };
            
            var unsyncedEntities = allEntities.Where(e => !e.IsSynced).ToList();
            
            // Configure MockDatabaseService.GetConnectionAsync to return a mock connection
            var mockConnection = new Mock<SQLiteAsyncConnection>();
            MockDatabaseService.Setup(m => m.GetConnectionAsync())
                .ReturnsAsync(mockConnection.Object);
            
            // Configure the mock connection to return the unsynced entities when queried
            var mockTable = new Mock<TableQuery<ReportEntity>>();
            mockConnection.Setup(m => m.Table<ReportEntity>())
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.Where(It.IsAny<Func<ReportEntity, bool>>()))
                .Returns(mockTable.Object);
            
            mockTable.Setup(m => m.ToListAsync())
                .ReturnsAsync(unsyncedEntities);
            
            // Create a ReportRepository with the mock database service
            var repository = new ReportRepository(
                MockDatabaseService.Object,
                Mock.Of<ILogger<ReportRepository>>());
            
            // Act
            var result = await repository.GetPendingReportsAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(r => !r.IsSynced).Should().BeTrue();
        }
    }
}