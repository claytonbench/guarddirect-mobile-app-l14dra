using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecurityPatrol.Models;
using SecurityPatrol.Services;

namespace SecurityPatrol.UnitTests.Helpers.MockServices
{
    /// <summary>
    /// Mock implementation of IAuthenticationService for unit testing that provides
    /// configurable responses for authentication operations without making actual API requests.
    /// </summary>
    public class MockAuthenticationService : IAuthenticationService
    {
        private string _currentPhoneNumber = string.Empty;
        private AuthState _currentAuthState = AuthState.CreateUnauthenticated();
        
        // Configurable results
        public bool RequestVerificationCodeResult { get; private set; } = true;
        public bool VerifyCodeResult { get; private set; } = true;
        public bool RefreshTokenResult { get; private set; } = true;
        
        // Exception configuration
        public bool ShouldThrowException { get; private set; } = false;
        public Exception ExceptionToThrow { get; private set; } = null;
        
        // Call tracking
        public List<string> RequestVerificationCodeCalls { get; } = new List<string>();
        public List<string> VerifyCodeCalls { get; } = new List<string>();
        public int LogoutCallCount { get; private set; } = 0;
        public int RefreshTokenCallCount { get; private set; } = 0;
        
        /// <summary>
        /// Mocks requesting a verification code to be sent to the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the verification code to.</param>
        /// <returns>The configured mock result for the verification code request.</returns>
        public async Task<bool> RequestVerificationCode(string phoneNumber)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            RequestVerificationCodeCalls.Add(phoneNumber);
            _currentPhoneNumber = phoneNumber;
            return await Task.FromResult(RequestVerificationCodeResult);
        }
        
        /// <summary>
        /// Mocks verifying the code sent to the user's phone number.
        /// </summary>
        /// <param name="code">The verification code received by the user.</param>
        /// <returns>The configured mock result for the verification code validation.</returns>
        public async Task<bool> VerifyCode(string code)
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            VerifyCodeCalls.Add(code);
            
            if (VerifyCodeResult)
            {
                _currentAuthState = AuthState.CreateAuthenticated(_currentPhoneNumber);
            }
            
            return await Task.FromResult(VerifyCodeResult);
        }
        
        /// <summary>
        /// Mocks retrieving the current authentication state of the user.
        /// </summary>
        /// <returns>The current mock authentication state.</returns>
        public async Task<AuthState> GetAuthenticationState()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            return await Task.FromResult(_currentAuthState);
        }
        
        /// <summary>
        /// Mocks logging out the current user.
        /// </summary>
        /// <returns>A completed task.</returns>
        public async Task Logout()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            LogoutCallCount++;
            _currentAuthState = AuthState.CreateUnauthenticated();
            _currentPhoneNumber = string.Empty;
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Mocks refreshing the authentication token.
        /// </summary>
        /// <returns>The configured mock result for the token refresh operation.</returns>
        public async Task<bool> RefreshToken()
        {
            if (ShouldThrowException)
            {
                throw ExceptionToThrow;
            }
            
            RefreshTokenCallCount++;
            return await Task.FromResult(RefreshTokenResult);
        }
        
        /// <summary>
        /// Configures the result for the RequestVerificationCode method.
        /// </summary>
        /// <param name="result">The result to return.</param>
        public void SetupRequestVerificationCodeResult(bool result)
        {
            RequestVerificationCodeResult = result;
        }
        
        /// <summary>
        /// Configures the result for the VerifyCode method.
        /// </summary>
        /// <param name="result">The result to return.</param>
        public void SetupVerifyCodeResult(bool result)
        {
            VerifyCodeResult = result;
        }
        
        /// <summary>
        /// Configures the result for the RefreshToken method.
        /// </summary>
        /// <param name="result">The result to return.</param>
        public void SetupRefreshTokenResult(bool result)
        {
            RefreshTokenResult = result;
        }
        
        /// <summary>
        /// Configures the current authentication state.
        /// </summary>
        /// <param name="authState">The authentication state to set.</param>
        public void SetupAuthenticationState(AuthState authState)
        {
            _currentAuthState = authState;
            if (authState.IsAuthenticated)
            {
                _currentPhoneNumber = authState.PhoneNumber;
            }
        }
        
        /// <summary>
        /// Configures an exception to be thrown by any method.
        /// </summary>
        /// <param name="exception">The exception to throw.</param>
        public void SetupException(Exception exception)
        {
            ShouldThrowException = true;
            ExceptionToThrow = exception;
        }
        
        /// <summary>
        /// Clears any configured exception.
        /// </summary>
        public void ClearException()
        {
            ShouldThrowException = false;
            ExceptionToThrow = null;
        }
        
        /// <summary>
        /// Verifies that RequestVerificationCode was called with the specified phone number.
        /// </summary>
        /// <param name="phoneNumber">The expected phone number.</param>
        /// <returns>True if the method was called with the specified phone number, otherwise false.</returns>
        public bool VerifyRequestVerificationCodeCalled(string phoneNumber)
        {
            return RequestVerificationCodeCalls.Contains(phoneNumber);
        }
        
        /// <summary>
        /// Verifies that VerifyCode was called with the specified code.
        /// </summary>
        /// <param name="code">The expected code.</param>
        /// <returns>True if the method was called with the specified code, otherwise false.</returns>
        public bool VerifyVerifyCodeCalled(string code)
        {
            return VerifyCodeCalls.Contains(code);
        }
        
        /// <summary>
        /// Verifies that Logout was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyLogoutCalled()
        {
            return LogoutCallCount > 0;
        }
        
        /// <summary>
        /// Verifies that RefreshToken was called.
        /// </summary>
        /// <returns>True if the method was called, otherwise false.</returns>
        public bool VerifyRefreshTokenCalled()
        {
            return RefreshTokenCallCount > 0;
        }
        
        /// <summary>
        /// Resets all configurations and call history.
        /// </summary>
        public void Reset()
        {
            _currentPhoneNumber = string.Empty;
            _currentAuthState = AuthState.CreateUnauthenticated();
            RequestVerificationCodeResult = true;
            VerifyCodeResult = true;
            RefreshTokenResult = true;
            ShouldThrowException = false;
            ExceptionToThrow = null;
            RequestVerificationCodeCalls.Clear();
            VerifyCodeCalls.Clear();
            LogoutCallCount = 0;
            RefreshTokenCallCount = 0;
        }
    }
}