using System;

namespace SecurityPatrol.Services
{
    /// <summary>
    /// Interface that defines the contract for the settings service, which provides methods to store, 
    /// retrieve, and manage user preferences and application configuration settings securely.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Retrieves a setting value of the specified type, or returns the default value if the setting doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of the setting value to retrieve.</typeparam>
        /// <param name="key">The key of the setting to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the setting doesn't exist.</param>
        /// <returns>The value of the setting if it exists, otherwise the provided default value.</returns>
        T GetValue<T>(string key, T defaultValue) where T : class;
        
        /// <summary>
        /// Stores a setting value with the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the setting value to store.</typeparam>
        /// <param name="key">The key to associate with the setting value.</param>
        /// <param name="value">The value to store.</param>
        void SetValue<T>(string key, T value) where T : class;
        
        /// <summary>
        /// Checks if a setting with the specified key exists.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the setting exists, otherwise false.</returns>
        bool ContainsKey(string key);
        
        /// <summary>
        /// Removes a setting with the specified key.
        /// </summary>
        /// <param name="key">The key of the setting to remove.</param>
        void Remove(string key);
        
        /// <summary>
        /// Removes all settings.
        /// </summary>
        void Clear();
    }
}