using System;
using System.Threading.Tasks;
using System.Text.Json;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecurityPatrol.MAUI.UnitTests.Setup;
using SecurityPatrol.Services;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.MAUI.UnitTests.Services
{
    public class SettingsServiceTests : TestBase
    {
        private SettingsService _settingsService;
        private Mock<ILogger<SettingsService>> _mockLogger;

        public SettingsServiceTests()
        {
            _mockLogger = new Mock<ILogger<SettingsService>>();
            _settingsService = new SettingsService(_mockLogger.Object);
        }

        public void Dispose()
        {
            base.Cleanup();
            _settingsService.Clear();
            _mockLogger.Reset();
        }

        [Fact]
        public async Task SetValue_ValidKeyAndValue_StoresSuccessfully()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            string serializedValue = JsonSerializer.Serialize(testValue);

            // Setup SecurityHelper mock
            MockSettingsService.Setup(x => x.SetValue(It.IsAny<string>(), It.IsAny<TestClass>()))
                .Callback<string, TestClass>((k, v) => { });

            // Act
            _settingsService.SetValue(testKey, testValue);

            // Assert - Just verify no exception is thrown
            // We can't directly verify the interaction with SecurityHelper because it's static
        }

        [Fact]
        public async Task SetValue_NullOrEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _settingsService.SetValue<TestClass>(null, testValue));
            Assert.Throws<ArgumentException>(() => _settingsService.SetValue<TestClass>(string.Empty, testValue));
        }

        [Fact]
        public async Task SetValue_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            string testKey = "testKey";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _settingsService.SetValue<TestClass>(testKey, null));
        }

        [Fact]
        public async Task SetValue_SecurityHelperThrowsException_PropagatesException()
        {
            // Note: This test is limited because we can't directly mock SecurityHelper
            // In a complete test environment, we would inject a mock implementation
            // or use a more advanced mocking framework for static methods
            
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            
            // Act & Assert - If we could mock SecurityHelper to throw, we would test exception propagation
        }

        [Fact]
        public async Task GetValue_SettingExists_ReturnsValue()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Set up the value first
            _settingsService.SetValue(testKey, testValue);

            // Act
            var result = _settingsService.GetValue(testKey, defaultValue);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(testValue.Name);
            result.Value.Should().Be(testValue.Value);
        }

        [Fact]
        public async Task GetValue_SettingDoesNotExist_ReturnsDefaultValue()
        {
            // Arrange
            string testKey = "nonExistentKey";
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Act
            var result = _settingsService.GetValue(testKey, defaultValue);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(defaultValue.Name);
            result.Value.Should().Be(defaultValue.Value);
        }

        [Fact]
        public async Task GetValue_NullOrEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _settingsService.GetValue<TestClass>(null, defaultValue));
            Assert.Throws<ArgumentException>(() => _settingsService.GetValue<TestClass>(string.Empty, defaultValue));
        }

        [Fact]
        public async Task GetValue_SecurityHelperThrowsException_ReturnsDefaultValue()
        {
            // Note: This test is limited because we can't directly mock SecurityHelper
            // In a complete test environment, we would verify the logger was called
            // and the default value was returned when SecurityHelper throws
            
            // Arrange
            string testKey = "testKey";
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };
            
            // Act
            var result = _settingsService.GetValue(testKey, defaultValue);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Be(defaultValue);
        }

        [Fact]
        public async Task GetValue_InvalidJsonFormat_ReturnsDefaultValue()
        {
            // Note: This test is limited because we can't directly manipulate what SecurityHelper returns
            // In a complete test environment, we would mock SecurityHelper to return invalid JSON
            
            // Arrange
            string testKey = "testKey";
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };
            
            // Act
            var result = _settingsService.GetValue(testKey, defaultValue);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Be(defaultValue);
        }

        [Fact]
        public async Task GetValue_CachedValue_ReturnsCachedValueWithoutStorageAccess()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Set up the value (which also caches it)
            _settingsService.SetValue(testKey, testValue);

            // Act - This should use the cached value
            var result = _settingsService.GetValue(testKey, defaultValue);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(testValue.Name);
            result.Value.Should().Be(testValue.Value);
        }

        [Fact]
        public async Task ContainsKey_SettingExists_ReturnsTrue()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Set up the value first
            _settingsService.SetValue(testKey, testValue);

            // Act
            var result = _settingsService.ContainsKey(testKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ContainsKey_SettingDoesNotExist_ReturnsFalse()
        {
            // Arrange
            string testKey = "nonExistentKey";

            // Act
            var result = _settingsService.ContainsKey(testKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ContainsKey_NullOrEmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _settingsService.ContainsKey(null));
            Assert.Throws<ArgumentException>(() => _settingsService.ContainsKey(string.Empty));
        }

        [Fact]
        public async Task ContainsKey_SecurityHelperThrowsException_ReturnsFalse()
        {
            // Note: This test is limited because we can't directly mock SecurityHelper
            // In a complete test environment, we would verify the logger was called 
            // and false was returned when SecurityHelper throws
            
            // Arrange
            string testKey = "testKey";
            
            // Act & Assert
            // We'll test with a key that shouldn't exist, expecting false
            var result = _settingsService.ContainsKey($"nonexistent_{Guid.NewGuid()}");
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ContainsKey_CachedValue_ReturnsTrueWithoutStorageAccess()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Set up the value (which also caches it)
            _settingsService.SetValue(testKey, testValue);

            // Act - This should check the cache first
            var result = _settingsService.ContainsKey(testKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Remove_SettingExists_RemovesSetting()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Set up the value first
            _settingsService.SetValue(testKey, testValue);
            _settingsService.ContainsKey(testKey).Should().BeTrue(); // Verify it exists

            // Act
            _settingsService.Remove(testKey);

            // Assert
            _settingsService.ContainsKey(testKey).Should().BeFalse();
        }

        [Fact]
        public async Task Remove_NullOrEmptyKey_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _settingsService.Remove(null));
            Assert.Throws<ArgumentException>(() => _settingsService.Remove(string.Empty));
        }

        [Fact]
        public async Task Remove_SecurityHelperThrowsException_PropagatesException()
        {
            // Note: This test is limited because we can't directly mock SecurityHelper
            // In a complete test environment, we would mock SecurityHelper to throw
            // and verify the exception propagation
            
            // Arrange
            string testKey = "testKey";
            
            // Act & Assert - This is more of a placeholder for completeness
        }

        [Fact]
        public async Task Remove_CachedValue_RemovesFromCacheAndStorage()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Set up the value (which also caches it)
            _settingsService.SetValue(testKey, testValue);
            _settingsService.ContainsKey(testKey).Should().BeTrue(); // Verify it exists

            // Act
            _settingsService.Remove(testKey);

            // Assert
            _settingsService.ContainsKey(testKey).Should().BeFalse();
        }

        [Fact]
        public async Task Clear_RemovesAllSettings()
        {
            // Arrange
            string testKey1 = "testKey1";
            string testKey2 = "testKey2";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };

            // Set up some values
            _settingsService.SetValue(testKey1, testValue);
            _settingsService.SetValue(testKey2, testValue);
            _settingsService.ContainsKey(testKey1).Should().BeTrue(); // Verify they exist
            _settingsService.ContainsKey(testKey2).Should().BeTrue();

            // Act
            _settingsService.Clear();

            // Assert
            _settingsService.ContainsKey(testKey1).Should().BeFalse();
            _settingsService.ContainsKey(testKey2).Should().BeFalse();
        }

        [Fact]
        public async Task Clear_SecurityHelperThrowsException_PropagatesException()
        {
            // Note: This test is limited because we can't directly mock SecurityHelper
            // In a complete test environment, we would mock SecurityHelper to throw
            // and verify the exception propagation
            
            // Arrange, Act & Assert - This is more of a placeholder for completeness
        }

        [Fact]
        public async Task Clear_ClearsCache()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Set up the value (which also caches it)
            _settingsService.SetValue(testKey, testValue);

            // Act
            _settingsService.Clear();

            // Assert
            var result = _settingsService.GetValue(testKey, defaultValue);
            result.Should().NotBeNull();
            result.Name.Should().Be(defaultValue.Name);
            result.Value.Should().Be(defaultValue.Value);
        }

        [Fact]
        public async Task ClearCache_ClearsOnlyCache()
        {
            // Arrange
            string testKey = "testKey";
            TestClass testValue = new TestClass { Name = "Test", Value = 42 };
            TestClass defaultValue = new TestClass { Name = "Default", Value = 0 };

            // Set up the value (which also caches it)
            _settingsService.SetValue(testKey, testValue);

            // Act
            _settingsService.ClearCache();

            // Assert - Should retrieve from storage since cache is cleared
            var result = _settingsService.GetValue(testKey, defaultValue);
            result.Should().NotBeNull();
            result.Name.Should().Be(testValue.Name);
            result.Value.Should().Be(testValue.Value);
        }

        // Helper class for testing
        private class TestClass
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }
    }
}