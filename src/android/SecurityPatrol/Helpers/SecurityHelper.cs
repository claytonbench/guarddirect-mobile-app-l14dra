using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using SecurityPatrol.Constants;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Static helper class that provides security-related functionality for the Security Patrol application.
    /// </summary>
    public static class SecurityHelper
    {
        private static readonly ILogger<SecurityHelper> _logger;

        /// <summary>
        /// Static constructor that initializes the logger
        /// </summary>
        static SecurityHelper()
        {
            // Initialize logger using the application's logger factory
            _logger = LoggerFactory.Create(builder => 
                builder.AddConsole()
                       .SetMinimumLevel(LogLevel.Debug))
                       .CreateLogger<SecurityHelper>();
        }

        /// <summary>
        /// Securely saves a value to the device's secure storage
        /// </summary>
        /// <param name="key">The key to associate with the value</param>
        /// <param name="value">The value to save</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task SaveToSecureStorage(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Value cannot be null");
            }

            try
            {
                _logger.LogDebug("Saving value to secure storage with key: {Key}", key);
                await SecureStorage.SetAsync(key, value);
                _logger.LogDebug("Successfully saved value to secure storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to secure storage with key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a value from the device's secure storage
        /// </summary>
        /// <param name="key">The key associated with the value</param>
        /// <returns>The retrieved value, or null if not found</returns>
        public static async Task<string> GetFromSecureStorage(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                _logger.LogDebug("Retrieving value from secure storage with key: {Key}", key);
                string value = await SecureStorage.GetAsync(key);
                _logger.LogDebug("Successfully retrieved value from secure storage");
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from secure storage with key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Removes a value from the device's secure storage
        /// </summary>
        /// <param name="key">The key associated with the value to remove</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task RemoveFromSecureStorage(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                _logger.LogDebug("Removing value from secure storage with key: {Key}", key);
                SecureStorage.Remove(key);
                _logger.LogDebug("Successfully removed value from secure storage");
                await Task.CompletedTask; // To make this method async for consistency
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from secure storage with key: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// Clears all values from the device's secure storage
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task ClearSecureStorage()
        {
            try
            {
                _logger.LogDebug("Clearing all values from secure storage");
                SecureStorage.RemoveAll();
                _logger.LogDebug("Successfully cleared all values from secure storage");
                await Task.CompletedTask; // To make this method async for consistency
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing secure storage");
                throw;
            }
        }

        /// <summary>
        /// Encrypts a string using AES encryption
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <param name="key">The encryption key</param>
        /// <returns>The encrypted string in Base64 format</returns>
        public static string EncryptString(string plainText, string key)
        {
            if (plainText == null)
            {
                throw new ArgumentNullException(nameof(plainText), "Plain text cannot be null");
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                _logger.LogDebug("Encrypting string with length: {Length}", plainText.Length);

                // Convert key to bytes
                byte[] keyBytes = Convert.FromBase64String(key);

                using (Aes aes = Aes.Create())
                {
                    // Generate a new IV (Initialization Vector) for each encryption
                    aes.GenerateIV();
                    aes.Key = keyBytes;

                    // Create an encryptor
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        // First write the IV to the stream
                        memoryStream.Write(aes.IV, 0, aes.IV.Length);

                        // Create a CryptoStream and write the encrypted data
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                        }

                        // Convert to Base64 and return
                        string result = Convert.ToBase64String(memoryStream.ToArray());
                        _logger.LogDebug("Successfully encrypted string");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting string");
                throw;
            }
        }

        /// <summary>
        /// Decrypts a string that was encrypted using AES encryption
        /// </summary>
        /// <param name="cipherText">The encrypted text in Base64 format</param>
        /// <param name="key">The encryption key</param>
        /// <returns>The decrypted string</returns>
        public static string DecryptString(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                throw new ArgumentException("Cipher text cannot be null or empty", nameof(cipherText));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                _logger.LogDebug("Decrypting string with length: {Length}", cipherText.Length);

                // Convert key and cipherText to bytes
                byte[] keyBytes = Convert.FromBase64String(key);
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    // Get the IV from the cipher bytes (first block)
                    byte[] iv = new byte[aes.BlockSize / 8];
                    Array.Copy(cipherBytes, 0, iv, 0, iv.Length);

                    // Set the key and IV
                    aes.Key = keyBytes;
                    aes.IV = iv;

                    // Create a decryptor
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                string result = streamReader.ReadToEnd();
                                _logger.LogDebug("Successfully decrypted string");
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting string");
                throw;
            }
        }

        /// <summary>
        /// Generates a secure random key for encryption
        /// </summary>
        /// <param name="keySizeInBits">The key size in bits (128, 192, or 256)</param>
        /// <returns>A Base64-encoded secure random key</returns>
        public static string GenerateSecureKey(int keySizeInBits)
        {
            // Validate key size
            if (keySizeInBits != 128 && keySizeInBits != 192 && keySizeInBits != 256)
            {
                throw new ArgumentException("Key size must be 128, 192, or 256 bits", nameof(keySizeInBits));
            }

            try
            {
                _logger.LogDebug("Generating secure key with size: {KeySize} bits", keySizeInBits);

                // Create a byte array for the key
                byte[] keyBytes = new byte[keySizeInBits / 8];

                // Fill the array with cryptographically strong random bytes
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(keyBytes);
                }

                // Convert to Base64 and return
                string result = Convert.ToBase64String(keyBytes);
                _logger.LogDebug("Successfully generated secure key");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure key");
                throw;
            }
        }

        /// <summary>
        /// Computes a SHA-256 hash of the input string
        /// </summary>
        /// <param name="input">The input string to hash</param>
        /// <returns>The hash as a hexadecimal string</returns>
        public static string ComputeHash(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input), "Input cannot be null");
            }

            try
            {
                _logger.LogDebug("Computing hash for input with length: {Length}", input.Length);

                // Convert the input string to a byte array and compute the hash
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes;

                using (SHA256 sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(inputBytes);
                }

                // Convert the byte array to a hexadecimal string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }

                string result = builder.ToString();
                _logger.LogDebug("Successfully computed hash");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing hash");
                throw;
            }
        }

        /// <summary>
        /// Checks if secure storage is available on the device
        /// </summary>
        /// <returns>True if secure storage is available, otherwise false</returns>
        public static async Task<bool> IsSecureStorageAvailable()
        {
            try
            {
                _logger.LogDebug("Checking if secure storage is available");

                // Try to store and retrieve a test value
                string testKey = $"{AppConstants.AppName}_SecureStorageTest";
                string testValue = Guid.NewGuid().ToString();

                await SecureStorage.SetAsync(testKey, testValue);
                string retrievedValue = await SecureStorage.GetAsync(testKey);
                SecureStorage.Remove(testKey);

                bool isAvailable = retrievedValue == testValue;
                _logger.LogDebug("Secure storage available: {IsAvailable}", isAvailable);
                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Secure storage is not available");
                return false;
            }
        }
    }
}