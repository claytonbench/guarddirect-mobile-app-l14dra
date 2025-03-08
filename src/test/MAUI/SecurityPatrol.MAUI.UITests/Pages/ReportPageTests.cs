using System;
using Xamarin.UITest;
using NUnit.Framework;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;
using SecurityPatrol.Constants;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Contains UI tests for the Activity Report page components of the Security Patrol application
    /// </summary>
    public class ReportPageTests : UITestBase
    {
        // UI element identifiers
        private const string ReportTextField = "ReportTextField";
        private const string CharactersRemainingLabel = "CharactersRemainingLabel";
        private const string SubmitButton = "SubmitButton";
        private const string CancelButton = "CancelButton";
        private const string ErrorMessage = "ValidationErrorMessage";
        private const string ReportListView = "ReportListView";
        private const string CreateReportButton = "CreateReportButton";

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Login to the application
            Login();
            
            // Navigate to the Reports page
            NavigateToPage("Reports");
        }

        /// <summary>
        /// Tests that the report list page loads correctly with all required UI elements
        /// </summary>
        [Test]
        [Order(1)]
        public void TestReportListPageLoads()
        {
            // Assert that the report list view exists
            AssertElementExists(ReportListView);
            
            // Assert that the create report button exists
            AssertElementExists(CreateReportButton);
            
            // Assert that the page title is displayed correctly
            AssertElementExists("ReportsPageTitle");
            AssertElementText("ReportsPageTitle", "Activity Reports");
        }

        /// <summary>
        /// Tests that the create report page loads correctly when the create button is tapped
        /// </summary>
        [Test]
        [Order(2)]
        public void TestCreateReportPageLoads()
        {
            // Tap the create report button
            TapElement(CreateReportButton);
            
            // Wait for the report creation page to load
            WaitForElement("CreateReportPageTitle");
            
            // Assert that the report text field exists
            AssertElementExists(ReportTextField);
            
            // Assert that the characters remaining label exists
            AssertElementExists(CharactersRemainingLabel);
            
            // Assert that the submit button exists
            AssertElementExists(SubmitButton);
            
            // Assert that the cancel button exists
            AssertElementExists(CancelButton);
            
            // Assert that the submit button is initially disabled
            var submitButtonElement = App.Query(SubmitButton)[0];
            Assert.IsFalse(submitButtonElement.Enabled, "Submit button should be disabled initially");
        }

        /// <summary>
        /// Tests validation behavior of the report text field
        /// </summary>
        [Test]
        [Order(3)]
        public void TestReportTextFieldValidation()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Assert that the submit button is initially disabled
            var submitButtonElement = App.Query(SubmitButton)[0];
            Assert.IsFalse(submitButtonElement.Enabled, "Submit button should be disabled initially");
            
            // Enter a valid report text
            EnterText(ReportTextField, TestConstants.TestReportText);
            
            // Assert that the submit button becomes enabled
            submitButtonElement = App.Query(SubmitButton)[0];
            Assert.IsTrue(submitButtonElement.Enabled, "Submit button should be enabled after entering valid text");
            
            // Clear the report text field
            ClearText(ReportTextField);
            
            // Assert that the submit button becomes disabled again
            submitButtonElement = App.Query(SubmitButton)[0];
            Assert.IsFalse(submitButtonElement.Enabled, "Submit button should be disabled after clearing text");
            
            // Enter a very long text exceeding the maximum length
            string longText = new string('A', AppConstants.ReportMaxLength + 100);
            EnterText(ReportTextField, longText);
            
            // Assert that the text is truncated to the maximum allowed length
            var textFieldElement = App.Query(ReportTextField)[0];
            Assert.AreEqual(AppConstants.ReportMaxLength, textFieldElement.Text.Length, 
                "Text should be truncated to the maximum allowed length");
            
            // Assert that the characters remaining label shows '0'
            AssertElementText(CharactersRemainingLabel, "0");
        }

        /// <summary>
        /// Tests that the characters remaining counter updates correctly as text is entered
        /// </summary>
        [Test]
        [Order(4)]
        public void TestCharactersRemainingCounter()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Assert that the characters remaining label initially shows the maximum character count
            AssertElementText(CharactersRemainingLabel, AppConstants.ReportMaxLength.ToString());
            
            // Enter a short text
            string shortText = "Short text";
            EnterText(ReportTextField, shortText);
            
            // Assert that the characters remaining label updates correctly
            int expectedRemaining = AppConstants.ReportMaxLength - shortText.Length;
            AssertElementText(CharactersRemainingLabel, expectedRemaining.ToString());
            
            // Enter more text
            string additionalText = " with additional content to test character counting";
            EnterText(ReportTextField, shortText + additionalText);
            
            // Assert that the characters remaining label updates again
            expectedRemaining = AppConstants.ReportMaxLength - (shortText.Length + additionalText.Length);
            AssertElementText(CharactersRemainingLabel, expectedRemaining.ToString());
            
            // Clear the text field
            ClearText(ReportTextField);
            
            // Assert that the characters remaining label resets to the maximum character count
            AssertElementText(CharactersRemainingLabel, AppConstants.ReportMaxLength.ToString());
        }

        /// <summary>
        /// Tests that the cancel button returns to the report list page
        /// </summary>
        [Test]
        [Order(5)]
        public void TestCancelButtonNavigation()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Enter some text in the report field
            EnterText(ReportTextField, "Some text that should not be saved");
            
            // Tap the cancel button
            TapElement(CancelButton);
            
            // Assert that the report list page is displayed
            AssertElementExists(ReportListView);
            AssertElementExists(CreateReportButton);
            
            // Navigate to the report creation page again
            TapElement(CreateReportButton);
            WaitForElement(ReportTextField);
            
            // Assert that the previously entered text is not present (form was reset)
            var textFieldElement = App.Query(ReportTextField)[0];
            Assert.AreEqual(string.Empty, textFieldElement.Text, 
                "Report text field should be empty after cancellation");
        }

        /// <summary>
        /// Tests successful submission of a report
        /// </summary>
        [Test]
        [Order(6)]
        public void TestSubmitReportSuccess()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Enter valid report text
            EnterText(ReportTextField, TestConstants.TestReportText);
            
            // Tap the submit button
            TapElement(SubmitButton);
            
            // Wait for the success message
            WaitForElement("SuccessMessage");
            
            // Assert that the application returns to the report list page
            WaitForElement(ReportListView);
            
            // Assert that the new report appears in the list
            // This assumes reports contain the text or timestamp that can be identified
            AssertElementExists("ReportItem");
        }

        /// <summary>
        /// Tests that error messages are displayed correctly when validation fails
        /// </summary>
        [Test]
        [Order(7)]
        public void TestErrorMessageDisplay()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Enter invalid report text (e.g., only whitespace)
            EnterText(ReportTextField, "   ");
            
            // Attempt to submit the report by tapping submit button
            TapElement(SubmitButton);
            
            // Assert that an error message is displayed
            AssertElementExists(ErrorMessage);
            
            // Assert that the error message contains the expected validation text
            AssertElementText(ErrorMessage, "Report text cannot be empty");
            
            // Assert that the form remains on the report creation page
            AssertElementExists(ReportTextField);
        }

        /// <summary>
        /// Tests keyboard behavior when entering report text
        /// </summary>
        [Test]
        [Order(8)]
        public void TestKeyboardBehavior()
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Tap the report text field
            TapElement(ReportTextField);
            
            // Assert that the keyboard appears
            // Note: Direct keyboard verification is limited in UI tests
            
            // Enter some text
            App.EnterText("Test keyboard input");
            
            // Assert that the keyboard can be dismissed
            App.DismissKeyboard();
            
            // Assert that the entered text remains in the field
            AssertElementText(ReportTextField, "Test keyboard input");
        }

        /// <summary>
        /// Helper method to navigate to the report creation page
        /// </summary>
        private void NavigateToReportCreationPage()
        {
            // Ensure we're on the report list page
            WaitForElement(ReportListView);
            
            // Tap the create report button
            TapElement(CreateReportButton);
            
            // Wait for the report creation page to load
            WaitForElement(ReportTextField);
        }
    }
}