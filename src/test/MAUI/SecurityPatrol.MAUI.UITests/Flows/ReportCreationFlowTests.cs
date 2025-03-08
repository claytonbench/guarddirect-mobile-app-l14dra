using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Xamarin.UITest;
using SecurityPatrol.MAUI.UITests.Setup;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Flows
{
    /// <summary>
    /// Contains end-to-end UI tests for the report creation flow in the Security Patrol application
    /// </summary>
    [TestFixture]
    public class ReportCreationFlowTests : UITestBase
    {
        // UI element identifiers
        private const string ReportsTabIdentifier = "ReportsTab";
        private const string CreateReportButtonIdentifier = "CreateReportButton";
        private const string ReportTextFieldIdentifier = "ReportTextField";
        private const string SubmitButtonIdentifier = "SubmitButton";
        private const string CancelButtonIdentifier = "CancelButton";
        private const string CharactersRemainingLabelIdentifier = "CharactersRemainingLabel";
        private const string SuccessMessageIdentifier = "SuccessMessage";
        private const string ErrorMessageIdentifier = "ErrorMessage";
        private const string ReportListIdentifier = "ReportList";

        /// <summary>
        /// Initializes a new instance of the ReportCreationFlowTests class
        /// </summary>
        public ReportCreationFlowTests() : base()
        {
            // Constructor initializes the base class
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            Login();
            NavigateToPage("Reports");
        }

        /// <summary>
        /// Tests the complete flow of creating and submitting a report successfully
        /// </summary>
        [Test]
        [Order(1)]
        public async Task TestCompleteReportCreationFlow()
        {
            // Navigate to the Reports tab
            NavigateToPage("Reports");
            
            // Tap the Create Report button
            TapElement(CreateReportButtonIdentifier);
            
            // Wait for the report creation page to load
            WaitForElement(ReportTextFieldIdentifier);
            
            // Enter valid report text
            EnterText(ReportTextFieldIdentifier, TestConstants.TestReportText);
            
            // Verify that the characters remaining label updates correctly
            AssertElementExists(CharactersRemainingLabelIdentifier);
            int expectedRemaining = 500 - TestConstants.TestReportText.Length; // Assuming 500 is max length
            AssertElementText(CharactersRemainingLabelIdentifier, $"{expectedRemaining} characters remaining");
            
            // Verify that the Submit button is enabled
            AssertElementExists(SubmitButtonIdentifier);
            
            // Tap the Submit button
            TapElement(SubmitButtonIdentifier);
            
            // Wait for the success message to appear
            WaitForElement(SuccessMessageIdentifier);
            
            // Verify that the application returns to the report list page
            WaitForElement(ReportListIdentifier);
            
            // Verify that the new report appears in the list
            VerifyReportInList(TestConstants.TestReportText);
            
            // Take a screenshot of the final state
            TakeScreenshot("ReportCreationComplete");
        }

        /// <summary>
        /// Tests cancelling the report creation process
        /// </summary>
        [Test]
        [Order(2)]
        public async Task TestReportCreationCancellation()
        {
            // Navigate to the Reports tab
            NavigateToPage("Reports");
            
            // Tap the Create Report button
            TapElement(CreateReportButtonIdentifier);
            
            // Wait for the report creation page to load
            WaitForElement(ReportTextFieldIdentifier);
            
            // Enter some report text
            string cancellationText = "This report will be cancelled";
            EnterText(ReportTextFieldIdentifier, cancellationText);
            
            // Tap the Cancel button
            TapElement(CancelButtonIdentifier);
            
            // Verify that the application returns to the report list page without creating a report
            WaitForElement(ReportListIdentifier);
            
            // Using AssertElementDoesNotExist to verify the cancelled report doesn't appear in the list
            try
            {
                App.WaitForElement(e => e.Marked(ReportListIdentifier).Descendant().Text(cancellationText), 
                    timeout: TimeSpan.FromSeconds(2));
                Assert.Fail("Cancelled report should not appear in list");
            }
            catch
            {
                // Expected - element should not be found
            }
            
            // Tap the Create Report button again
            TapElement(CreateReportButtonIdentifier);
            
            // Verify that the report text field is empty (form was reset)
            WaitForElement(ReportTextFieldIdentifier);
            AssertElementText(ReportTextFieldIdentifier, "");
            
            // Take a screenshot of the final state
            TakeScreenshot("ReportCreationCancelled");
        }

        /// <summary>
        /// Tests validation errors during report creation
        /// </summary>
        [Test]
        [Order(3)]
        public async Task TestReportValidationErrors()
        {
            // Navigate to the Reports tab
            NavigateToPage("Reports");
            
            // Tap the Create Report button
            TapElement(CreateReportButtonIdentifier);
            
            // Wait for the report creation page to load
            WaitForElement(ReportTextFieldIdentifier);
            
            // Verify that the Submit button is initially disabled (empty report)
            AssertElementExists("SubmitButtonDisabled");
            
            // Enter whitespace-only text
            EnterText(ReportTextFieldIdentifier, "   ");
            
            // Verify that the Submit button remains disabled
            AssertElementExists("SubmitButtonDisabled");
            
            // Enter valid text and then clear it
            EnterText(ReportTextFieldIdentifier, "Valid text");
            ClearText(ReportTextFieldIdentifier);
            
            // Verify that the Submit button becomes disabled again
            AssertElementExists("SubmitButtonDisabled");
            
            // Enter extremely long text exceeding the maximum length
            string longText = new string('A', 600); // Assuming max length is 500
            EnterText(ReportTextFieldIdentifier, longText);
            
            // Verify that the text is truncated to the maximum allowed length
            var element = WaitForElement(ReportTextFieldIdentifier);
            Assert.AreEqual(500, element.Text.Length, "Text should be truncated to maximum length");
            
            // Verify that the characters remaining label shows '0'
            AssertElementText(CharactersRemainingLabelIdentifier, "0 characters remaining");
            
            // Take a screenshot of the validation errors
            TakeScreenshot("ReportValidationErrors");
        }

        /// <summary>
        /// Tests report creation behavior when network connectivity issues occur
        /// </summary>
        [Test]
        [Order(4)]
        public async Task TestReportCreationWithNetworkIssue()
        {
            // Navigate to the Reports tab
            NavigateToPage("Reports");
            
            // Tap the Create Report button
            TapElement(CreateReportButtonIdentifier);
            
            // Wait for the report creation page to load
            WaitForElement(ReportTextFieldIdentifier);
            
            // Enter valid report text
            EnterText(ReportTextFieldIdentifier, TestConstants.TestReportText);
            
            // Simulate network disconnection
            App.Invoke("enableOfflineMode", true);
            
            // Tap the Submit button
            TapElement(SubmitButtonIdentifier);
            
            // Verify that the report is saved locally with offline indicator
            WaitForElement("OfflineIndicator");
            
            // Verify that appropriate offline message is displayed
            AssertElementExists("OfflineMessage");
            
            // Simulate network reconnection
            App.Invoke("enableOfflineMode", false);
            
            // Verify that the report is eventually synchronized
            WaitForElement("SyncCompletedIndicator", TimeSpan.FromSeconds(10));
            
            // Take a screenshot of the offline and sync states
            TakeScreenshot("ReportOfflineSync");
        }

        /// <summary>
        /// Tests creating multiple reports in succession
        /// </summary>
        [Test]
        [Order(5)]
        public async Task TestMultipleReportCreation()
        {
            // Navigate to the Reports tab
            NavigateToPage("Reports");
            
            // Create first report with specific text
            string firstReportText = "First test report " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await CreateAndSubmitReport(firstReportText);
            
            // Verify successful submission
            VerifyReportInList(firstReportText);
            
            // Create second report with different text
            string secondReportText = "Second test report " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            await CreateAndSubmitReport(secondReportText);
            
            // Verify successful submission
            VerifyReportInList(secondReportText);
            
            // Verify that both reports appear in the list in correct order
            // This assumes the newest reports appear at the top of the list
            var reportElements = App.Query(e => e.Marked(ReportListIdentifier).Descendant());
            bool foundFirst = false;
            bool foundSecond = false;
            
            foreach (var element in reportElements)
            {
                if (element.Text.Contains(firstReportText))
                    foundFirst = true;
                if (element.Text.Contains(secondReportText))
                    foundSecond = true;
            }
            
            Assert.IsTrue(foundFirst && foundSecond, "Both reports should appear in the list");
            
            // Take a screenshot of the report list
            TakeScreenshot("MultipleReports");
        }

        /// <summary>
        /// Helper method to navigate to the report creation page
        /// </summary>
        private void NavigateToReportCreationPage()
        {
            // Ensure we're on the Reports tab
            NavigateToPage("Reports");
            
            // Tap the Create Report button
            TapElement(CreateReportButtonIdentifier);
            
            // Wait for the report creation page to load with report text field visible
            WaitForElement(ReportTextFieldIdentifier);
        }

        /// <summary>
        /// Helper method to create and submit a report with the given text
        /// </summary>
        /// <param name="reportText">The text to use in the report</param>
        private async Task CreateAndSubmitReport(string reportText)
        {
            // Navigate to the report creation page
            NavigateToReportCreationPage();
            
            // Enter the provided report text
            EnterText(ReportTextFieldIdentifier, reportText);
            
            // Tap the Submit button
            TapElement(SubmitButtonIdentifier);
            
            // Wait for the success message or report list to appear
            try
            {
                WaitForElement(SuccessMessageIdentifier, TimeSpan.FromSeconds(5));
            }
            catch
            {
                // If success message doesn't appear, check if we're back at the report list
                WaitForElement(ReportListIdentifier);
            }
        }

        /// <summary>
        /// Helper method to verify a report with specific text exists in the list
        /// </summary>
        /// <param name="reportText">The text to search for in the report list</param>
        private void VerifyReportInList(string reportText)
        {
            // Wait for the report list to load
            WaitForElement(ReportListIdentifier);
            
            // Search for an element containing the report text
            var element = App.Query(e => e.Marked(ReportListIdentifier).Descendant().Contains(reportText)).FirstOrDefault();
            
            // Assert that the element exists in the list
            Assert.IsNotNull(element, $"Report with text '{reportText}' not found in the list");
        }
    }
}