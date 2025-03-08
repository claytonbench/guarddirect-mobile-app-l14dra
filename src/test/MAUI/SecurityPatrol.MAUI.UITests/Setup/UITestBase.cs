using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Xamarin.UITest;
using Xamarin.UITest.Queries;
using NUnit.Framework;
using SecurityPatrol.TestCommon.Constants;

namespace SecurityPatrol.MAUI.UITests.Setup
{
    /// <summary>
    /// Base class for UI tests in the Security Patrol application. Provides common setup, teardown, and utility methods for UI automation testing.
    /// </summary>
    public class UITestBase
    {
        /// <summary>
        /// The Xamarin.UITest application instance for UI automation
        /// </summary>
        protected IApp App { get; private set; }
        
        /// <summary>
        /// The platform being tested (Android by default)
        /// </summary>
        protected Platform Platform { get; private set; }
        
        /// <summary>
        /// Default timeout for UI operations
        /// </summary>
        protected TimeSpan DefaultTimeout { get; private set; }

        /// <summary>
        /// Initializes a new instance of the UITestBase class
        /// </summary>
        public UITestBase()
        {
            // Set default platform to Android as per technical specifications
            Platform = Platform.Android;
            
            // Set default timeout from test constants
            DefaultTimeout = TimeSpan.FromMilliseconds(TestConstants.TestTimeoutMilliseconds);
        }

        /// <summary>
        /// Sets up the test environment before each test
        /// </summary>
        [SetUp]
        public virtual void SetUp()
        {
            // Initialize the app using the AppInitializer
            App = AppInitializer.StartApp(Platform);
            
            // Allow app to fully initialize
            WaitForLoading();
        }

        /// <summary>
        /// Cleans up the test environment after each test
        /// </summary>
        [TearDown]
        public virtual void TearDown()
        {
            // Take a screenshot of the final state for debugging purposes
            TakeScreenshot("FinalState");
        }

        /// <summary>
        /// Waits for a UI element to appear on screen
        /// </summary>
        /// <param name="marked">The identifier of the element to wait for</param>
        /// <param name="timeout">Optional timeout, defaults to DefaultTimeout if not specified</param>
        /// <returns>The UI element that was found</returns>
        protected virtual AppResult WaitForElement(string marked, TimeSpan? timeout = null)
        {
            TimeSpan waitTimeout = timeout ?? DefaultTimeout;
            return App.WaitForElement(marked, $"Timed out waiting for element '{marked}'", waitTimeout)[0];
        }

        /// <summary>
        /// Waits for a UI element to disappear from screen
        /// </summary>
        /// <param name="marked">The identifier of the element to wait for disappearance</param>
        /// <param name="timeout">Optional timeout, defaults to DefaultTimeout if not specified</param>
        protected virtual void WaitForNoElement(string marked, TimeSpan? timeout = null)
        {
            TimeSpan waitTimeout = timeout ?? DefaultTimeout;
            App.WaitForNoElement(marked, $"Element '{marked}' still present", waitTimeout);
        }

        /// <summary>
        /// Enters text into a UI element
        /// </summary>
        /// <param name="marked">The identifier of the element to enter text into</param>
        /// <param name="text">The text to enter</param>
        protected virtual void EnterText(string marked, string text)
        {
            WaitForElement(marked);
            App.EnterText(marked, text);
            App.DismissKeyboard();
        }

        /// <summary>
        /// Clears text from a UI element
        /// </summary>
        /// <param name="marked">The identifier of the element to clear text from</param>
        protected virtual void ClearText(string marked)
        {
            WaitForElement(marked);
            App.ClearText(marked);
        }

        /// <summary>
        /// Taps on a UI element
        /// </summary>
        /// <param name="marked">The identifier of the element to tap</param>
        protected virtual void TapElement(string marked)
        {
            WaitForElement(marked);
            App.Tap(marked);
        }

        /// <summary>
        /// Scrolls to a UI element
        /// </summary>
        /// <param name="marked">The identifier of the element to scroll to</param>
        protected virtual void ScrollTo(string marked)
        {
            App.ScrollTo(marked);
        }

        /// <summary>
        /// Asserts that a UI element exists on screen
        /// </summary>
        /// <param name="marked">The identifier of the element to check for</param>
        /// <param name="timeout">Optional timeout, defaults to DefaultTimeout if not specified</param>
        protected virtual void AssertElementExists(string marked, TimeSpan? timeout = null)
        {
            try
            {
                WaitForElement(marked, timeout);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Element '{marked}' not found: {ex.Message}");
            }
        }

        /// <summary>
        /// Asserts that a UI element does not exist on screen
        /// </summary>
        /// <param name="marked">The identifier of the element to check for absence</param>
        /// <param name="timeout">Optional timeout, defaults to a short timeout if not specified</param>
        protected virtual void AssertElementDoesNotExist(string marked, TimeSpan? timeout = null)
        {
            TimeSpan waitTimeout = timeout ?? TimeSpan.FromSeconds(2); // Short timeout for checking absence
            
            try
            {
                App.WaitForNoElement(marked, timeout: waitTimeout);
            }
            catch (Exception)
            {
                Assert.Fail($"Element '{marked}' exists but should not");
            }
        }

        /// <summary>
        /// Asserts that a UI element contains the expected text
        /// </summary>
        /// <param name="marked">The identifier of the element to check</param>
        /// <param name="expectedText">The text expected to be in the element</param>
        protected virtual void AssertElementText(string marked, string expectedText)
        {
            WaitForElement(marked);
            var element = App.Query(marked).FirstOrDefault();
            
            Assert.IsNotNull(element, $"Element '{marked}' not found");
            string actualText = element.Text;
            
            Assert.AreEqual(expectedText, actualText, $"Text in element '{marked}' does not match. Expected: '{expectedText}', Actual: '{actualText}'");
        }

        /// <summary>
        /// Takes a screenshot of the current screen
        /// </summary>
        /// <param name="screenshotName">Name for the screenshot file</param>
        /// <returns>Information about the saved screenshot file</returns>
        protected virtual FileInfo TakeScreenshot(string screenshotName)
        {
            return App.Screenshot(screenshotName);
        }

        /// <summary>
        /// Navigates to a specific page in the application
        /// </summary>
        /// <param name="pageName">The name of the page to navigate to</param>
        protected virtual void NavigateToPage(string pageName)
        {
            // Common pattern for tab-based navigation
            TapElement(pageName + "Tab");
            
            // Wait for the page to load
            WaitForPageToLoad(pageName);
        }

        /// <summary>
        /// Performs the login process for tests that require authentication
        /// </summary>
        protected virtual void Login()
        {
            // Wait for phone entry page
            WaitForElement("PhoneNumberEntry");
            
            // Enter test phone number
            EnterText("PhoneNumberEntry", TestConstants.TestPhoneNumber);
            
            // Tap request code button
            TapElement("RequestCodeButton");
            
            // Wait for verification code page
            WaitForElement("VerificationCodeEntry");
            
            // Enter test verification code
            EnterText("VerificationCodeEntry", TestConstants.TestVerificationCode);
            
            // Tap verify button
            TapElement("VerifyButton");
            
            // Wait for main page to load after successful login
            WaitForElement("MainTabControl");
        }

        /// <summary>
        /// Performs the logout process
        /// </summary>
        protected virtual void Logout()
        {
            // Navigate to settings page
            NavigateToPage("Settings");
            
            // Tap logout button
            TapElement("LogoutButton");
            
            // If there's a confirmation dialog, confirm logout
            try
            {
                WaitForElement("ConfirmLogoutButton", TimeSpan.FromSeconds(2));
                TapElement("ConfirmLogoutButton");
            }
            catch
            {
                // No confirmation dialog, continue
            }
            
            // Wait for login page to appear, indicating successful logout
            WaitForElement("PhoneNumberEntry");
        }

        /// <summary>
        /// Waits for a specific page to load by checking for its identifying elements
        /// </summary>
        /// <param name="pageIdentifier">The identifier of the page</param>
        /// <param name="timeout">Optional timeout, defaults to DefaultTimeout if not specified</param>
        protected virtual void WaitForPageToLoad(string pageIdentifier, TimeSpan? timeout = null)
        {
            WaitForElement(pageIdentifier + "Page", timeout);
        }

        /// <summary>
        /// Performs a swipe left gesture on the screen
        /// </summary>
        protected virtual void SwipeLeft()
        {
            App.SwipeRightToLeft();
        }

        /// <summary>
        /// Performs a swipe right gesture on the screen
        /// </summary>
        protected virtual void SwipeRight()
        {
            App.SwipeLeftToRight();
        }

        /// <summary>
        /// Performs a swipe up gesture on the screen
        /// </summary>
        protected virtual void SwipeUp()
        {
            App.SwipeBottomToTop();
        }

        /// <summary>
        /// Performs a swipe down gesture on the screen
        /// </summary>
        protected virtual void SwipeDown()
        {
            App.SwipeTopToBottom();
        }

        /// <summary>
        /// Presses the back button
        /// </summary>
        protected virtual void PressBack()
        {
            App.Back();
        }

        /// <summary>
        /// Waits for loading indicators to disappear
        /// </summary>
        /// <param name="timeout">Optional timeout, defaults to DefaultTimeout if not specified</param>
        protected virtual void WaitForLoading(TimeSpan? timeout = null)
        {
            TimeSpan waitTimeout = timeout ?? DefaultTimeout;
            
            try
            {
                // Wait for loading indicator to appear, it might already be gone
                App.WaitForElement(c => c.Marked("LoadingIndicator"), timeout: TimeSpan.FromSeconds(1));
                
                // Wait for it to disappear
                App.WaitForNoElement(c => c.Marked("LoadingIndicator"), timeout: waitTimeout);
            }
            catch
            {
                // Loading indicator not found, which is fine - the screen is probably already loaded
            }
        }

        /// <summary>
        /// Resets the application to its initial state
        /// </summary>
        protected virtual void ResetAppState()
        {
            AppInitializer.ResetApp(App);
            
            // Allow app to fully initialize after reset
            WaitForLoading();
        }
    }
}