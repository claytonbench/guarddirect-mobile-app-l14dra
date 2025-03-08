using System;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.TestCommon.Mocks
{
    /// <summary>
    /// Mock implementation of IAuthenticationService for testing purposes
    /// that simulates authentication functionality without accessing actual backend services.
    /// </summary>
    public class MockAuthService : IAuthenticationService
    {
        private string _currentPhoneNumber;
        private string _verificationCode;
        private AuthState _currentAuthState;

        /// <summary>
        /// Gets or sets a value indicating whether authentication operations should succeed.
        /// </summary>
        public bool ShouldSucceed { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether methods should throw exceptions to simulate service failures.
        /// </summary>
        public bool ShouldThrowException { get; set; } = false;

        /// <summary>
        /// Gets the last phone number that was used in a verification request.
        /// </summary>
        public string LastRequestedPhoneNumber { get; private set; }

        /// <summary>
        /// Gets the last verification code that was submitted.
        /// </summary>
        public string LastSubmittedCode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MockAuthService class with default test values.
        /// </summary>
        public MockAuthService()
        {
            _currentPhoneNumber = null;
            _verificationCode = TestConstants.TestVerificationCode;
            _currentAuthState = AuthState.CreateUnauthenticated();
            LastRequestedPhoneNumber = null;
            LastSubmittedCode = null;
        }

        /// <summary>
        /// Simulates requesting a verification code for the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the verification code to.</param>
        /// <returns>A task that returns true if the verification code was successfully requested, otherwise false.</returns>
        public async Task<bool> RequestVerificationCode(string phoneNumber)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in RequestVerificationCode");
            }

            _currentPhoneNumber = phoneNumber;
            LastRequestedPhoneNumber = phoneNumber;

            return await Task.FromResult(ShouldSucceed);
        }

        /// <summary>
        /// Simulates verifying the code sent to the user's phone number.
        /// </summary>
        /// <param name="code">The verification code received by the user.</param>
        /// <returns>A task that returns true if the verification was successful, otherwise false.</returns>
        public async Task<bool> VerifyCode(string code)
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in VerifyCode");
            }

            LastSubmittedCode = code;

            if (!ShouldSucceed)
            {
                return await Task.FromResult(false);
            }

            if (_currentPhoneNumber == null)
            {
                return await Task.FromResult(false);
            }

            if (code == _verificationCode || ShouldSucceed)
            {
                _currentAuthState = AuthState.CreateAuthenticated(_currentPhoneNumber);
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Retrieves the current authentication state.
        /// </summary>
        /// <returns>A task that returns the current authentication state.</returns>
        public async Task<AuthState> GetAuthenticationState()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in GetAuthenticationState");
            }

            return await Task.FromResult(_currentAuthState);
        }

        /// <summary>
        /// Simulates logging out the current user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Logout()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in Logout");
            }

            _currentAuthState = AuthState.CreateUnauthenticated();
            _currentPhoneNumber = null;

            return await Task.CompletedTask;
        }

        /// <summary>
        /// Simulates refreshing the authentication token.
        /// </summary>
        /// <returns>A task that returns true if the token was successfully refreshed, otherwise false.</returns>
        public async Task<bool> RefreshToken()
        {
            if (ShouldThrowException)
            {
                throw new Exception("Simulated exception in RefreshToken");
            }

            if (!ShouldSucceed)
            {
                return await Task.FromResult(false);
            }

            if (!_currentAuthState.IsAuthenticated)
            {
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Sets the authentication state for testing scenarios.
        /// </summary>
        /// <param name="authState">The authentication state to set.</param>
        public void SetAuthenticationState(AuthState authState)
        {
            _currentAuthState = authState;
            
            if (authState.IsAuthenticated)
            {
                _currentPhoneNumber = authState.PhoneNumber;
            }
            else
            {
                _currentPhoneNumber = null;
            }
        }

        /// <summary>
        /// Sets the expected verification code for testing scenarios.
        /// </summary>
        /// <param name="code">The verification code to set.</param>
        public void SetVerificationCode(string code)
        {
            _verificationCode = code;
        }
    }
}