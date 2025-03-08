using System;
using System.Threading.Tasks;
using Xamarin.UITest;
using NUnit.Framework;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Flows
{
    /// <summary>
    /// Contains end-to-end UI tests for the complete authentication flow of the Security Patrol application.
    /// These tests validate the entire authentication process from phone number entry through verification code submission to successful login.
    /// </summary>
    [TestFixture]
    public class AuthenticationFlowTests : UITestBase
    {
        // Constants for UI element identifiers
        private const string PhoneEntryField = "PhoneNumberEntry";
        private const string RequestCodeButton = "RequestCodeButton";
        private const string VerificationCodeField = "VerificationCodeEntry";
        private const string VerifyButton = "VerifyButton";
        private const string MainPageIdentifier = "MainPage";
        private const string PhoneEntryPageIdentifier = "PhoneEntryPage";
        private const string VerificationPageIdentifier = "VerificationPage";

        /// <summary>
        /// Initializes a new instance of the AuthenticationFlowTests class.
        /// </summary>
        public AuthenticationFlowTests() : base()
        {
            // Constructor
        }

        /// <summary>
        /// Tests the complete authentication flow from phone number entry to successful login.
        /// </summary>
        [Test]
        [Order(1)]
        public void TestCompleteAuthenticationFlow()
        {
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter the test phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Wait for the verification code page to load
            WaitForPageToLoad(VerificationPageIdentifier);
            
            // Enter the test verification code
            EnterText(VerificationCodeField, TestConstants.TestVerificationCode);
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Wait for the main page to load
            WaitForPageToLoad(MainPageIdentifier);
            
            // Assert that the main page elements exist, confirming successful authentication
            AssertElementExists("MainTabControl");
        }

        /// <summary>
        /// Tests that invalid phone numbers are properly validated and error messages displayed.
        /// </summary>
        [Test]
        [Order(2)]
        public void TestInvalidPhoneNumberHandling()
        {
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter an invalid phone number
            EnterText(PhoneEntryField, "123");
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that an error message is displayed
            AssertElementExists("PhoneNumberErrorMessage");
            
            // Assert that the application remains on the phone entry page
            AssertElementExists(PhoneEntryPageIdentifier);
            
            // Clear the phone number field
            ClearText(PhoneEntryField);
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that the verification code page loads, indicating validation passed
            WaitForPageToLoad(VerificationPageIdentifier);
        }

        /// <summary>
        /// Tests that invalid verification codes are properly validated and error messages displayed.
        /// </summary>
        [Test]
        [Order(3)]
        public void TestInvalidVerificationCodeHandling()
        {
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Wait for the verification code page to load
            WaitForPageToLoad(VerificationPageIdentifier);
            
            // Enter an invalid verification code
            EnterText(VerificationCodeField, "123");
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that an error message is displayed
            AssertElementExists("VerificationCodeErrorMessage");
            
            // Assert that the application remains on the verification code page
            AssertElementExists(VerificationPageIdentifier);
            
            // Clear the verification code field
            ClearText(VerificationCodeField);
            
            // Enter a valid verification code
            EnterText(VerificationCodeField, TestConstants.TestVerificationCode);
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that the main page loads, indicating validation passed
            WaitForPageToLoad(MainPageIdentifier);
        }

        /// <summary>
        /// Tests navigation between authentication screens using the back button.
        /// </summary>
        [Test]
        [Order(4)]
        public void TestBackNavigationBetweenAuthenticationScreens()
        {
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Wait for the verification code page to load
            WaitForPageToLoad(VerificationPageIdentifier);
            
            // Tap the back button
            PressBack();
            
            // Assert that the phone entry page is displayed again
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Assert that the previously entered phone number is still displayed
            AssertElementText(PhoneEntryField, TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests the functionality to resend a verification code.
        /// </summary>
        [Test]
        [Order(5)]
        public void TestResendVerificationCode()
        {
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Wait for the verification code page to load
            WaitForPageToLoad(VerificationPageIdentifier);
            
            // Tap the resend code button
            TapElement("ResendCodeButton");
            
            // Assert that a confirmation message is displayed
            AssertElementExists("ResendConfirmationMessage");
            
            // Enter the test verification code
            EnterText(VerificationCodeField, TestConstants.TestVerificationCode);
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that the main page loads, confirming the resent code works
            WaitForPageToLoad(MainPageIdentifier);
        }

        /// <summary>
        /// Tests the Login helper method from UITestBase for reuse in other tests.
        /// </summary>
        [Test]
        [Order(6)]
        public void TestLoginHelperMethod()
        {
            // Call the Login helper method from UITestBase
            Login();
            
            // Assert that the main page loads, confirming the helper method works correctly
            WaitForPageToLoad(MainPageIdentifier);
            
            // Verify that key elements of the main page are visible
            AssertElementExists("MainTabControl");
        }

        /// <summary>
        /// Tests handling of network errors during authentication.
        /// </summary>
        [Test]
        [Order(7)]
        public void TestNetworkErrorHandling()
        {
            // Configure the app to simulate network errors (using test mode)
            App.Invoke("enableOfflineMode", true);
            
            // Wait for the phone entry page to load
            WaitForPageToLoad(PhoneEntryPageIdentifier);
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that a network error message is displayed
            AssertElementExists("NetworkErrorMessage");
            
            // Assert that a retry option is available
            AssertElementExists("RetryButton");
            
            // Disable network error simulation
            App.Invoke("enableOfflineMode", false);
            
            // Tap the retry button
            TapElement("RetryButton");
            
            // Assert that the verification code page loads after retry
            WaitForPageToLoad(VerificationPageIdentifier);
        }

        /// <summary>
        /// Tests that authentication session persists when app is restarted.
        /// </summary>
        [Test]
        [Order(8)]
        public void TestSessionPersistence()
        {
            // Complete the authentication flow to log in
            Login();
            
            // Restart the application
            ResetAppState();
            
            // Assert that the main page loads directly without requiring re-authentication
            WaitForPageToLoad(MainPageIdentifier);
            
            // Verify that the user is still authenticated by checking for authenticated-only elements
            AssertElementExists("MainTabControl");
        }
    }
}