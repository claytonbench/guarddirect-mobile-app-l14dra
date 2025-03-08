using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecurityPatrol.Core.Interfaces;

namespace SecurityPatrol.Infrastructure.Services
{
    /// <summary>
    /// Configuration options for the SMS service.
    /// </summary>
    public class SmsOptions
    {
        /// <summary>
        /// Gets or sets the API key for the SMS service.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the base URL for the SMS API.
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the phone number to use as the sender.
        /// </summary>
        public string FromNumber { get; set; }

        /// <summary>
        /// Gets or sets the message template used for verification codes.
        /// The template should include a placeholder {0} for the code.
        /// </summary>
        public string VerificationMessageTemplate { get; set; }

        /// <summary>
        /// Initializes a new instance of the SmsOptions class.
        /// </summary>
        public SmsOptions()
        {
            // Default values
            ApiKey = string.Empty;
            ApiUrl = string.Empty;
            FromNumber = string.Empty;
            VerificationMessageTemplate = "Your Security Patrol verification code is: {0}";
        }
    }

    /// <summary>
    /// Implements the ISmsService interface to provide SMS messaging functionality for the Security Patrol application.
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly SmsOptions _options;
        private readonly ILogger<SmsService> _logger;

        /// <summary>
        /// Initializes a new instance of the SmsService class with required dependencies.
        /// </summary>
        /// <param name="options">SMS service configuration options.</param>
        /// <param name="logger">Logger for the SMS service.</param>
        /// <param name="httpClient">HTTP client for making API requests.</param>
        public SmsService(IOptions<SmsOptions> options, ILogger<SmsService> logger, HttpClient httpClient)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // Configure HttpClient if not already configured
            if (_httpClient.BaseAddress == null && !string.IsNullOrEmpty(_options.ApiUrl))
            {
                _httpClient.BaseAddress = new Uri(_options.ApiUrl);
            }

            // Add API key header if not already present
            if (!_httpClient.DefaultRequestHeaders.Contains("Authorization") && !string.IsNullOrEmpty(_options.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            }
        }

        /// <summary>
        /// Sends an SMS message to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="message">The message content to send.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the SMS was sent successfully.</returns>
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            try
            {
                // Mask the phone number for logging (show only last 4 digits)
                var maskedPhoneNumber = MaskPhoneNumber(phoneNumber);
                _logger.LogInformation("Sending SMS to {PhoneNumber}", maskedPhoneNumber);

                // Create the request payload
                var request = new
                {
                    to = phoneNumber,
                    from = _options.FromNumber,
                    message = message
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send the request
                var response = await _httpClient.PostAsync("sms/send", content);

                // Check if successful
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent SMS to {PhoneNumber}", maskedPhoneNumber);
                    return true;
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send SMS to {PhoneNumber}. Status code: {StatusCode}, Response: {Response}", 
                        maskedPhoneNumber, response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error when sending SMS to {PhoneNumber}: {Message}", 
                    MaskPhoneNumber(phoneNumber), ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending SMS to {PhoneNumber}: {Message}", 
                    MaskPhoneNumber(phoneNumber), ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Sends a verification code to the specified phone number using a template message.
        /// </summary>
        /// <param name="phoneNumber">The recipient's phone number.</param>
        /// <param name="code">The verification code to send.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the verification code was sent successfully.</returns>
        public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string code)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            if (string.IsNullOrEmpty(code))
                throw new ArgumentNullException(nameof(code));

            try
            {
                // Mask the phone number for logging
                var maskedPhoneNumber = MaskPhoneNumber(phoneNumber);
                _logger.LogInformation("Sending verification code to {PhoneNumber}", maskedPhoneNumber);

                // Format the message using the template
                var message = string.Format(_options.VerificationMessageTemplate, code);

                // Use the SendSmsAsync method to send the verification code
                return await SendSmsAsync(phoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification code to {PhoneNumber}: {Message}", 
                    MaskPhoneNumber(phoneNumber), ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Masks a phone number for logging purposes, showing only the last 4 digits.
        /// </summary>
        /// <param name="phoneNumber">The phone number to mask.</param>
        /// <returns>The masked phone number.</returns>
        private string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length <= 4)
                return "****";

            return $"***-***-{phoneNumber.Substring(phoneNumber.Length - 4)}";
        }
    }
}