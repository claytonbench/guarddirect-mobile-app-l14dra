using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using SecurityPatrol.Core.Entities;
using SecurityPatrol.UnitTests.Helpers;

namespace SecurityPatrol.UnitTests.Core.Entities
{
    /// <summary>
    /// Contains unit tests for entity classes in the Security Patrol application
    /// </summary>
    public class EntityTests : TestBase
    {
        [Fact]
        public void AuditableEntity_HasAuditProperties()
        {
            // Create a concrete implementation of AuditableEntity for testing
            var concreteEntity = new User();

            // Verify that audit properties exist and are accessible
            concreteEntity.CreatedBy = "testUser";
            concreteEntity.Created = DateTime.UtcNow;
            concreteEntity.LastModifiedBy = "modifiedBy";
            concreteEntity.LastModified = DateTime.UtcNow;

            // Assert
            concreteEntity.CreatedBy.Should().Be("testUser");
            concreteEntity.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            concreteEntity.LastModifiedBy.Should().Be("modifiedBy");
            concreteEntity.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void User_HasRequiredProperties()
        {
            // Create a new User instance
            var user = new User
            {
                Id = "user123",
                PhoneNumber = "+15551234567",
                LastAuthenticated = DateTime.UtcNow,
                IsActive = true
            };

            // Assert all properties exist and are set correctly
            user.Id.Should().Be("user123");
            user.PhoneNumber.Should().Be("+15551234567");
            user.LastAuthenticated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            user.IsActive.Should().BeTrue();
            
            // Verify collections are initialized
            user.TimeRecords.Should().NotBeNull();
            user.LocationRecords.Should().NotBeNull();
            user.Photos.Should().NotBeNull();
            user.Reports.Should().NotBeNull();
            user.CheckpointVerifications.Should().NotBeNull();
        }

        [Fact]
        public void User_InheritsFromAuditableEntity()
        {
            // Create a new User instance
            var user = new User();

            // Verify User is an instance of AuditableEntity
            user.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            user.CreatedBy = "testUser";
            user.Created = DateTime.UtcNow;
            user.LastModifiedBy = "modifiedBy";
            user.LastModified = DateTime.UtcNow;

            user.CreatedBy.Should().Be("testUser");
            user.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            user.LastModifiedBy.Should().Be("modifiedBy");
            user.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void TimeRecord_HasRequiredProperties()
        {
            // Create a new TimeRecord instance
            var timeRecord = new TimeRecord
            {
                Id = 1,
                UserId = "user123",
                Type = "ClockIn",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsSynced = false,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            timeRecord.Id.Should().Be(1);
            timeRecord.UserId.Should().Be("user123");
            timeRecord.Type.Should().Be("ClockIn");
            timeRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            timeRecord.Latitude.Should().Be(40.7128);
            timeRecord.Longitude.Should().Be(-74.0060);
            timeRecord.IsSynced.Should().BeFalse();
            timeRecord.RemoteId.Should().Be("remote123");
            
            // Verify navigation property exists
            timeRecord.User.Should().BeNull(); // Not set in this test
        }

        [Fact]
        public void TimeRecord_InheritsFromAuditableEntity()
        {
            // Create a new TimeRecord instance
            var timeRecord = new TimeRecord();

            // Verify TimeRecord is an instance of AuditableEntity
            timeRecord.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            timeRecord.CreatedBy = "testUser";
            timeRecord.Created = DateTime.UtcNow;
            timeRecord.LastModifiedBy = "modifiedBy";
            timeRecord.LastModified = DateTime.UtcNow;

            timeRecord.CreatedBy.Should().Be("testUser");
            timeRecord.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            timeRecord.LastModifiedBy.Should().Be("modifiedBy");
            timeRecord.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void LocationRecord_HasRequiredProperties()
        {
            // Create a new LocationRecord instance
            var locationRecord = new LocationRecord
            {
                Id = 1,
                UserId = "user123",
                Latitude = 40.7128,
                Longitude = -74.0060,
                Accuracy = 10.5,
                Timestamp = DateTime.UtcNow,
                IsSynced = false,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            locationRecord.Id.Should().Be(1);
            locationRecord.UserId.Should().Be("user123");
            locationRecord.Latitude.Should().Be(40.7128);
            locationRecord.Longitude.Should().Be(-74.0060);
            locationRecord.Accuracy.Should().Be(10.5);
            locationRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            locationRecord.IsSynced.Should().BeFalse();
            locationRecord.RemoteId.Should().Be("remote123");
            
            // Verify navigation property exists
            locationRecord.User.Should().BeNull(); // Not set in this test
        }

        [Fact]
        public void LocationRecord_InheritsFromAuditableEntity()
        {
            // Create a new LocationRecord instance
            var locationRecord = new LocationRecord();

            // Verify LocationRecord is an instance of AuditableEntity
            locationRecord.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            locationRecord.CreatedBy = "testUser";
            locationRecord.Created = DateTime.UtcNow;
            locationRecord.LastModifiedBy = "modifiedBy";
            locationRecord.LastModified = DateTime.UtcNow;

            locationRecord.CreatedBy.Should().Be("testUser");
            locationRecord.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            locationRecord.LastModifiedBy.Should().Be("modifiedBy");
            locationRecord.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PatrolLocation_HasRequiredProperties()
        {
            // Create a new PatrolLocation instance
            var patrolLocation = new PatrolLocation
            {
                Id = 1,
                Name = "Main Building",
                Latitude = 40.7128,
                Longitude = -74.0060,
                LastUpdated = DateTime.UtcNow,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            patrolLocation.Id.Should().Be(1);
            patrolLocation.Name.Should().Be("Main Building");
            patrolLocation.Latitude.Should().Be(40.7128);
            patrolLocation.Longitude.Should().Be(-74.0060);
            patrolLocation.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            patrolLocation.RemoteId.Should().Be("remote123");
            
            // Verify collection is initialized
            patrolLocation.Checkpoints.Should().NotBeNull();
        }

        [Fact]
        public void PatrolLocation_InheritsFromAuditableEntity()
        {
            // Create a new PatrolLocation instance
            var patrolLocation = new PatrolLocation();

            // Verify PatrolLocation is an instance of AuditableEntity
            patrolLocation.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            patrolLocation.CreatedBy = "testUser";
            patrolLocation.Created = DateTime.UtcNow;
            patrolLocation.LastModifiedBy = "modifiedBy";
            patrolLocation.LastModified = DateTime.UtcNow;

            patrolLocation.CreatedBy.Should().Be("testUser");
            patrolLocation.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            patrolLocation.LastModifiedBy.Should().Be("modifiedBy");
            patrolLocation.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Checkpoint_HasRequiredProperties()
        {
            // Create a new Checkpoint instance
            var checkpoint = new Checkpoint
            {
                Id = 1,
                LocationId = 2,
                Name = "Main Entrance",
                Latitude = 40.7128,
                Longitude = -74.0060,
                LastUpdated = DateTime.UtcNow,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            checkpoint.Id.Should().Be(1);
            checkpoint.LocationId.Should().Be(2);
            checkpoint.Name.Should().Be("Main Entrance");
            checkpoint.Latitude.Should().Be(40.7128);
            checkpoint.Longitude.Should().Be(-74.0060);
            checkpoint.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            checkpoint.RemoteId.Should().Be("remote123");
            
            // Verify navigation properties exist
            checkpoint.PatrolLocation.Should().BeNull(); // Not set in this test
            checkpoint.Verifications.Should().NotBeNull();
        }

        [Fact]
        public void Checkpoint_InheritsFromAuditableEntity()
        {
            // Create a new Checkpoint instance
            var checkpoint = new Checkpoint();

            // Verify Checkpoint is an instance of AuditableEntity
            checkpoint.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            checkpoint.CreatedBy = "testUser";
            checkpoint.Created = DateTime.UtcNow;
            checkpoint.LastModifiedBy = "modifiedBy";
            checkpoint.LastModified = DateTime.UtcNow;

            checkpoint.CreatedBy.Should().Be("testUser");
            checkpoint.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            checkpoint.LastModifiedBy.Should().Be("modifiedBy");
            checkpoint.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CheckpointVerification_HasRequiredProperties()
        {
            // Create a new CheckpointVerification instance
            var verification = new CheckpointVerification
            {
                Id = 1,
                UserId = "user123",
                CheckpointId = 2,
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsSynced = false,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            verification.Id.Should().Be(1);
            verification.UserId.Should().Be("user123");
            verification.CheckpointId.Should().Be(2);
            verification.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            verification.Latitude.Should().Be(40.7128);
            verification.Longitude.Should().Be(-74.0060);
            verification.IsSynced.Should().BeFalse();
            verification.RemoteId.Should().Be("remote123");
            
            // Verify navigation properties exist
            verification.User.Should().BeNull(); // Not set in this test
            verification.Checkpoint.Should().BeNull(); // Not set in this test
        }

        [Fact]
        public void CheckpointVerification_InheritsFromAuditableEntity()
        {
            // Create a new CheckpointVerification instance
            var verification = new CheckpointVerification();

            // Verify CheckpointVerification is an instance of AuditableEntity
            verification.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            verification.CreatedBy = "testUser";
            verification.Created = DateTime.UtcNow;
            verification.LastModifiedBy = "modifiedBy";
            verification.LastModified = DateTime.UtcNow;

            verification.CreatedBy.Should().Be("testUser");
            verification.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            verification.LastModifiedBy.Should().Be("modifiedBy");
            verification.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Photo_HasRequiredProperties()
        {
            // Create a new Photo instance
            var photo = new Photo
            {
                Id = 1,
                UserId = "user123",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                FilePath = "/storage/photos/photo1.jpg",
                IsSynced = false,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            photo.Id.Should().Be(1);
            photo.UserId.Should().Be("user123");
            photo.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            photo.Latitude.Should().Be(40.7128);
            photo.Longitude.Should().Be(-74.0060);
            photo.FilePath.Should().Be("/storage/photos/photo1.jpg");
            photo.IsSynced.Should().BeFalse();
            photo.RemoteId.Should().Be("remote123");
            
            // Verify navigation property exists
            photo.User.Should().BeNull(); // Not set in this test
        }

        [Fact]
        public void Photo_InheritsFromAuditableEntity()
        {
            // Create a new Photo instance
            var photo = new Photo();

            // Verify Photo is an instance of AuditableEntity
            photo.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            photo.CreatedBy = "testUser";
            photo.Created = DateTime.UtcNow;
            photo.LastModifiedBy = "modifiedBy";
            photo.LastModified = DateTime.UtcNow;

            photo.CreatedBy.Should().Be("testUser");
            photo.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            photo.LastModifiedBy.Should().Be("modifiedBy");
            photo.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Report_HasRequiredProperties()
        {
            // Create a new Report instance
            var report = new Report
            {
                Id = 1,
                UserId = "user123",
                Text = "Test report content",
                Timestamp = DateTime.UtcNow,
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsSynced = false,
                RemoteId = "remote123"
            };

            // Assert all properties exist and are set correctly
            report.Id.Should().Be(1);
            report.UserId.Should().Be("user123");
            report.Text.Should().Be("Test report content");
            report.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            report.Latitude.Should().Be(40.7128);
            report.Longitude.Should().Be(-74.0060);
            report.IsSynced.Should().BeFalse();
            report.RemoteId.Should().Be("remote123");
            
            // Verify navigation property exists
            report.User.Should().BeNull(); // Not set in this test
        }

        [Fact]
        public void Report_InheritsFromAuditableEntity()
        {
            // Create a new Report instance
            var report = new Report();

            // Verify Report is an instance of AuditableEntity
            report.Should().BeAssignableTo<AuditableEntity>();

            // Verify audit properties are accessible
            report.CreatedBy = "testUser";
            report.Created = DateTime.UtcNow;
            report.LastModifiedBy = "modifiedBy";
            report.LastModified = DateTime.UtcNow;

            report.CreatedBy.Should().Be("testUser");
            report.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            report.LastModifiedBy.Should().Be("modifiedBy");
            report.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void EntityRelationships_AreCorrectlyDefined()
        {
            // Create test entities
            var user = new User { Id = "user123", PhoneNumber = "+15551234567" };
            var timeRecord = new TimeRecord { Id = 1, UserId = user.Id };
            var locationRecord = new LocationRecord { Id = 1, UserId = user.Id };
            var patrolLocation = new PatrolLocation { Id = 1, Name = "Main Location" };
            var checkpoint = new Checkpoint { Id = 1, LocationId = patrolLocation.Id, Name = "Entrance" };
            var verification = new CheckpointVerification { Id = 1, UserId = user.Id, CheckpointId = checkpoint.Id };
            var photo = new Photo { Id = 1, UserId = user.Id };
            var report = new Report { Id = 1, UserId = user.Id };

            // Set up relationships
            user.TimeRecords.Add(timeRecord);
            user.LocationRecords.Add(locationRecord);
            user.Photos.Add(photo);
            user.Reports.Add(report);
            user.CheckpointVerifications.Add(verification);
            patrolLocation.Checkpoints.Add(checkpoint);
            checkpoint.Verifications.Add(verification);
            
            // Set navigation properties
            timeRecord.User = user;
            locationRecord.User = user;
            photo.User = user;
            report.User = user;
            verification.User = user;
            verification.Checkpoint = checkpoint;
            checkpoint.PatrolLocation = patrolLocation;

            // Verify User to TimeRecord relationship
            user.TimeRecords.Should().Contain(timeRecord);
            timeRecord.User.Should().Be(user);
            timeRecord.UserId.Should().Be(user.Id);

            // Verify User to LocationRecord relationship
            user.LocationRecords.Should().Contain(locationRecord);
            locationRecord.User.Should().Be(user);
            locationRecord.UserId.Should().Be(user.Id);

            // Verify User to Photo relationship
            user.Photos.Should().Contain(photo);
            photo.User.Should().Be(user);
            photo.UserId.Should().Be(user.Id);

            // Verify User to Report relationship
            user.Reports.Should().Contain(report);
            report.User.Should().Be(user);
            report.UserId.Should().Be(user.Id);

            // Verify User to CheckpointVerification relationship
            user.CheckpointVerifications.Should().Contain(verification);
            verification.User.Should().Be(user);
            verification.UserId.Should().Be(user.Id);

            // Verify PatrolLocation to Checkpoint relationship
            patrolLocation.Checkpoints.Should().Contain(checkpoint);
            checkpoint.PatrolLocation.Should().Be(patrolLocation);
            checkpoint.LocationId.Should().Be(patrolLocation.Id);

            // Verify Checkpoint to CheckpointVerification relationship
            checkpoint.Verifications.Should().Contain(verification);
            verification.Checkpoint.Should().Be(checkpoint);
            verification.CheckpointId.Should().Be(checkpoint.Id);
        }
    }
}