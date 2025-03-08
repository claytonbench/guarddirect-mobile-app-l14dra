using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecurityPatrol.Constants;
using SecurityPatrol.Helpers;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Implementation of ISettingsService that provides methods to store, retrieve, and manage 
    /// user preferences and application configuration settings securely.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly Dictionary<string, object> _cache;
        private readonly string SETTINGS_PREFIX;

        /// <summary>
        /// Initializes a new instance of the SettingsService class with required dependencies.
        /// </summary>
        /// <param name="logger">The logger instance for logging.</param>
        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new Dictionary<string, object>();
            SETTINGS_PREFIX = $"{AppConstants.AppName}_setting_";
        }

        /// <summary>
        /// Retrieves a setting value of the specified type, or returns the default value if the setting doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the setting value to retrieve.</typeparam>
        /// <param name="key">The key of the setting to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist.</param>
        /// <returns>The value of the setting if it exists, otherwise the provided default value.</returns>
        public T GetValue<T>(string key, T defaultValue) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Getting setting with key: {Key}", key);

            try
            {
                // Check cache first for better performance
                if (_cache.TryGetValue(key, out object cachedValue) && cachedValue is T typedValue)
                {
                    _logger.LogDebug("Retrieved setting from cache: {Key}", key);
                    return typedValue;
                }

                // Get from secure storage
                string prefixedKey = GetPrefixedKey(key);
                string json = SecurityHelper.GetFromSecureStorage(prefixedKey).GetAwaiter().GetResult();

                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogDebug("Setting not found in storage, returning default value: {Key}", key);
                    return defaultValue;
                }

                // Deserialize the JSON string
                T value = JsonSerializer.Deserialize<T>(json);
                
                // Cache the value for future use
                _cache[key] = value;
                
                _logger.LogDebug("Successfully retrieved setting: {Key}", key);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving setting: {Key}", key);
                return defaultValue;
            }
        }

        /// <summary>
        /// Stores a setting value with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value to store.</typeparam>
        /// <param name="key">The key to associate with the setting value.</param>
        /// <param name="value">The value to store.</param>
        public void SetValue<T>(string key, T value) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null");
            }

            _logger.LogDebug("Setting value with key: {Key}", key);

            try
            {
                // Serialize the value to JSON
                string json = JsonSerializer.Serialize(value);
                
                // Store in secure storage
                string prefixedKey = GetPrefixedKey(key);
                SecurityHelper.SaveToSecureStorage(prefixedKey, json).GetAwaiter().GetResult();
                
                // Update cache
                _cache[key] = value;
                
                _logger.LogDebug("Successfully set value for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value for key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Checks if a setting with the specified key exists.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the setting exists, otherwise false.</returns>
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Checking if setting exists: {Key}", key);

            try
            {
                // Check cache first
                if (_cache.ContainsKey(key))
                {
                    _logger.LogDebug("Setting found in cache: {Key}", key);
                    return true;
                }

                // Check secure storage
                string prefixedKey = GetPrefixedKey(key);
                string value = SecurityHelper.GetFromSecureStorage(prefixedKey).GetAwaiter().GetResult();
                
                bool exists = !string.IsNullOrEmpty(value);
                _logger.LogDebug("Setting existence check result: {Key}, {Exists}", key, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if setting exists: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Removes a setting with the specified key.
        /// </summary>
        /// <param name="key">The key of the setting to remove.</param>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _logger.LogDebug("Removing setting: {Key}", key);

            try
            {
                // Remove from secure storage
                string prefixedKey = GetPrefixedKey(key);
                SecurityHelper.RemoveFromSecureStorage(prefixedKey).GetAwaiter().GetResult();
                
                // Remove from cache
                if (_cache.ContainsKey(key))
                {
                    _cache.Remove(key);
                }
                
                _logger.LogDebug("Successfully removed setting: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing setting: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Removes all settings.
        /// </summary>
        public void Clear()
        {
            _logger.LogDebug("Clearing all settings");

            try
            {
                // Clear secure storage
                SecurityHelper.ClearSecureStorage().GetAwaiter().GetResult();
                
                // Clear cache
                _cache.Clear();
                
                _logger.LogDebug("Successfully cleared all settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all settings");
                throw;
            }
        }

        /// <summary>
        /// Clears the in-memory settings cache without affecting stored settings.
        /// </summary>
        public void ClearCache()
        {
            _logger.LogDebug("Clearing settings cache");
            _cache.Clear();
            _logger.LogDebug("Successfully cleared settings cache");
        }

        /// <summary>
        /// Creates a prefixed key for secure storage.
        /// </summary>
        /// <param name="key">The original key.</param>
        /// <returns>The key with the settings prefix applied.</returns>
        private string GetPrefixedKey(string key)
        {
            return $"{SETTINGS_PREFIX}{key}";
        }
    }
}