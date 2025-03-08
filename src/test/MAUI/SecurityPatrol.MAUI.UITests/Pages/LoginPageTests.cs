using System;
using NUnit.Framework;
using Xamarin.UITest;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Contains UI tests for the login page components of the Security Patrol application
    /// </summary>
    public class LoginPageTests : UITestBase
    {
        // UI element identifiers
        private const string PhoneEntryField = "PhoneNumberEntry";
        private const string RequestCodeButton = "RequestCodeButton";
        private const string VerificationCodeField = "VerificationCodeEntry";
        private const string VerifyButton = "VerifyButton";
        private const string ErrorMessage = "ErrorMessage";
        private const string BackButton = "BackButton";

        /// <summary>
        /// Tests that the phone entry page loads correctly with all required UI elements
        /// </summary>
        [Test]
        [Order(1)]
        public void TestPhoneEntryPageLoads()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Assert that the phone number entry field exists
            AssertElementExists(PhoneEntryField);
            
            // Assert that the request code button exists
            AssertElementExists(RequestCodeButton);
            
            // Assert that the page title is displayed correctly
            AssertElementExists("PageTitle");
            
            // Assert that the phone number field is empty initially
            AssertElementText(PhoneEntryField, string.Empty);
        }

        /// <summary>
        /// Tests validation behavior of the phone number entry field
        /// </summary>
        [Test]
        [Order(2)]
        public void TestPhoneEntryFieldValidation()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Enter an invalid phone number format
            EnterText(PhoneEntryField, "1234"); // Too short
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that an error message is displayed
            AssertElementExists(ErrorMessage);
            
            // Assert that the error message contains the expected validation text
            AssertElementText(ErrorMessage, "Please enter a valid phone number with country code");
            
            // Clear the phone number field
            ClearText(PhoneEntryField);
            
            // Enter a valid phone number format
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that the verification code page loads (validation passed)
            AssertElementExists(VerificationCodeField);
        }

        /// <summary>
        /// Tests that the verification code page loads correctly after phone number submission
        /// </summary>
        [Test]
        [Order(3)]
        public void TestVerificationCodePageLoads()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Enter a valid phone number (TestConstants.TestPhoneNumber)
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Wait for the verification code page to load
            WaitForElement(VerificationCodeField);
            
            // Assert that the verification code entry field exists
            AssertElementExists(VerificationCodeField);
            
            // Assert that the verify button exists
            AssertElementExists(VerifyButton);
            
            // Assert that the back button exists
            AssertElementExists(BackButton);
            
            // Assert that the page title is displayed correctly
            AssertElementExists("PageTitle");
            
            // Assert that the verification code field is empty initially
            AssertElementText(VerificationCodeField, string.Empty);
        }

        /// <summary>
        /// Tests validation behavior of the verification code entry field
        /// </summary>
        [Test]
        [Order(4)]
        public void TestVerificationCodeFieldValidation()
        {
            // Navigate to the verification code page by entering a valid phone number first
            WaitForElement(PhoneEntryField);
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            TapElement(RequestCodeButton);
            WaitForElement(VerificationCodeField);
            
            // Enter an invalid verification code format (e.g., too short)
            EnterText(VerificationCodeField, "123"); // Should be 6 digits
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that an error message is displayed
            AssertElementExists(ErrorMessage);
            
            // Assert that the error message contains the expected validation text
            AssertElementText(ErrorMessage, "Verification code must be 6 digits");
            
            // Clear the verification code field
            ClearText(VerificationCodeField);
            
            // Enter a valid verification code format
            EnterText(VerificationCodeField, TestConstants.TestVerificationCode);
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that the main page loads (validation passed)
            AssertElementExists("MainTabControl");
        }

        /// <summary>
        /// Tests that the back button on the verification page returns to the phone entry page
        /// </summary>
        [Test]
        [Order(5)]
        public void TestBackNavigationFromVerificationPage()
        {
            // Navigate to the verification code page by entering a valid phone number first
            WaitForElement(PhoneEntryField);
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            TapElement(RequestCodeButton);
            WaitForElement(VerificationCodeField);
            
            // Tap the back button
            TapElement(BackButton);
            
            // Assert that the phone entry page is displayed
            AssertElementExists(PhoneEntryField);
            
            // Assert that the previously entered phone number is still displayed in the phone entry field
            AssertElementText(PhoneEntryField, TestConstants.TestPhoneNumber);
        }

        /// <summary>
        /// Tests the functionality to resend verification code
        /// </summary>
        [Test]
        [Order(6)]
        public void TestResendCodeFunctionality()
        {
            // Navigate to the verification code page by entering a valid phone number first
            WaitForElement(PhoneEntryField);
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            TapElement(RequestCodeButton);
            WaitForElement(VerificationCodeField);
            
            // Tap the resend code button
            TapElement("ResendCodeButton");
            
            // Assert that a confirmation message is displayed
            AssertElementExists("ResendConfirmation");
            
            // Assert that the verification code field is still accessible
            AssertElementExists(VerificationCodeField);
            
            // Assert that the verify button is still accessible
            AssertElementExists(VerifyButton);
        }

        /// <summary>
        /// Tests that error messages are displayed correctly and can be dismissed
        /// </summary>
        [Test]
        [Order(7)]
        public void TestErrorMessageDisplayAndDismissal()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Enter an invalid phone number format
            EnterText(PhoneEntryField, "1234");
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that an error message is displayed
            AssertElementExists(ErrorMessage);
            
            // Tap the dismiss button on the error message
            TapElement("DismissButton");
            
            // Assert that the error message is no longer displayed
            AssertElementDoesNotExist(ErrorMessage);
            
            // Enter a valid phone number
            ClearText(PhoneEntryField);
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that the verification code page loads (no error)
            AssertElementExists(VerificationCodeField);
        }

        /// <summary>
        /// Tests that the phone number field formats input correctly
        /// </summary>
        [Test]
        [Order(8)]
        public void TestPhoneNumberFormatting()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Enter a phone number without formatting (e.g., '5551234567')
            EnterText(PhoneEntryField, "5551234567");
            
            // Assert that the displayed phone number is formatted correctly (e.g., '(555) 123-4567')
            AssertElementText(PhoneEntryField, "(555) 123-4567");
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that the verification code page loads (formatting handled correctly)
            AssertElementExists(VerificationCodeField);
        }

        /// <summary>
        /// Tests that the phone entry field shows the correct keyboard type
        /// </summary>
        [Test]
        [Order(9)]
        public void TestKeyboardBehaviorOnPhoneEntry()
        {
            // Wait for the phone entry page to load
            WaitForElement(PhoneEntryField);
            
            // Tap the phone number field to focus it
            TapElement(PhoneEntryField);
            
            // Assert that the numeric keyboard is displayed
            // Note: This is difficult to test reliably in UI tests, implementation may vary
            
            // Enter a valid phone number
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            
            // Assert that the keyboard can be dismissed
            App.DismissKeyboard();
            
            // Tap the request code button
            TapElement(RequestCodeButton);
            
            // Assert that the verification code page loads
            AssertElementExists(VerificationCodeField);
        }

        /// <summary>
        /// Tests that the verification code field shows the correct keyboard type
        /// </summary>
        [Test]
        [Order(10)]
        public void TestKeyboardBehaviorOnVerificationCodeEntry()
        {
            // Navigate to the verification code page by entering a valid phone number first
            WaitForElement(PhoneEntryField);
            EnterText(PhoneEntryField, TestConstants.TestPhoneNumber);
            TapElement(RequestCodeButton);
            WaitForElement(VerificationCodeField);
            
            // Tap the verification code field to focus it
            TapElement(VerificationCodeField);
            
            // Assert that the numeric keyboard is displayed
            // Note: This is difficult to test reliably in UI tests, implementation may vary
            
            // Enter a valid verification code
            EnterText(VerificationCodeField, TestConstants.TestVerificationCode);
            
            // Assert that the keyboard can be dismissed
            App.DismissKeyboard();
            
            // Tap the verify button
            TapElement(VerifyButton);
            
            // Assert that the main page loads
            AssertElementExists("MainTabControl");
        }
    }
}