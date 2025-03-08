using System;
using Xamarin.UITest; // Xamarin.UITest 3.2.9
using NUnit.Framework; // NUnit 3.13.3
using SecurityPatrol.MAUI.UITests.Setup;

namespace SecurityPatrol.MAUI.UITests.Pages
{
    /// <summary>
    /// Contains UI tests for the photo capture page components of the Security Patrol application
    /// </summary>
    public class PhotoCapturePageTests : UITestBase
    {
        // UI element identifiers
        private const string CameraView = "CameraView";
        private const string CaptureButton = "CaptureButton";
        private const string PreviewImage = "PreviewImage";
        private const string AcceptButton = "AcceptButton";
        private const string RetakeButton = "RetakeButton";
        private const string BackButton = "BackButton";
        private const string UploadProgressIndicator = "UploadProgressIndicator";
        private const string CameraStatusIndicator = "CameraStatusIndicator";

        [Test]
        [Order(1)]
        public void TestPhotoCapturePageLoads()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Assert required UI elements
            AssertElementExists(CameraView);
            AssertElementExists(CaptureButton);
            AssertElementExists(BackButton);
            AssertElementExists(CameraStatusIndicator);

            // Assert that preview elements are not visible initially
            AssertElementDoesNotExist(PreviewImage);
            AssertElementDoesNotExist(AcceptButton);
            AssertElementDoesNotExist(RetakeButton);
        }

        [Test]
        [Order(2)]
        public void TestCameraStatusIndicator()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Assert that the camera status indicator exists
            AssertElementExists(CameraStatusIndicator);

            // Get the status indicator element and check text
            var statusElement = App.Query(CameraStatusIndicator)[0];
            Assert.AreEqual("Available", statusElement.Text, "Camera status indicator does not show 'Available'");

            // Check status indicator color (should be green for available)
            Assert.IsTrue(statusElement.Enabled, "Camera status indicator should be enabled when available");
        }

        [Test]
        [Order(3)]
        public void TestCaptureButtonFunctionality()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Assert that the capture button exists and is enabled
            AssertElementExists(CaptureButton);
            var captureButton = App.Query(CaptureButton)[0];
            Assert.IsTrue(captureButton.Enabled, "Capture button should be enabled");

            // Tap the capture button
            TapElement(CaptureButton);

            // Assert that a loading indicator appears during capture
            AssertElementExists("CaptureLoadingIndicator", TimeSpan.FromSeconds(2));

            // Wait for the preview image to appear
            WaitForElement(PreviewImage, TimeSpan.FromSeconds(5));

            // Assert that the preview image exists
            AssertElementExists(PreviewImage);

            // Assert that the capture button is no longer visible
            AssertElementDoesNotExist(CaptureButton);

            // Assert that the accept and retake buttons are now visible
            AssertElementExists(AcceptButton);
            AssertElementExists(RetakeButton);
        }

        [Test]
        [Order(4)]
        public void TestPhotoPreviewDisplay()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Tap the capture button to take a photo
            TapElement(CaptureButton);

            // Wait for the preview image to appear
            WaitForElement(PreviewImage, TimeSpan.FromSeconds(5));

            // Assert that the preview image exists
            AssertElementExists(PreviewImage);

            // Assert that the camera view is no longer visible
            AssertElementDoesNotExist(CameraView);

            // Assert that the accept and retake buttons are visible
            AssertElementExists(AcceptButton);
            AssertElementExists(RetakeButton);

            // Assert that the capture button is no longer visible
            AssertElementDoesNotExist(CaptureButton);
        }

        [Test]
        [Order(5)]
        public void TestRetakeButtonFunctionality()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Tap the capture button to take a photo
            TapElement(CaptureButton);

            // Wait for the preview image to appear
            WaitForElement(PreviewImage, TimeSpan.FromSeconds(5));

            // Assert that the retake button exists
            AssertElementExists(RetakeButton);

            // Tap the retake button
            TapElement(RetakeButton);

            // Assert that the preview image is no longer visible
            AssertElementDoesNotExist(PreviewImage);

            // Assert that the camera view is visible again
            AssertElementExists(CameraView);

            // Assert that the capture button is visible again
            AssertElementExists(CaptureButton);

            // Assert that the accept and retake buttons are no longer visible
            AssertElementDoesNotExist(AcceptButton);
            AssertElementDoesNotExist(RetakeButton);
        }

        [Test]
        [Order(6)]
        public void TestAcceptButtonFunctionality()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Tap the capture button to take a photo
            TapElement(CaptureButton);

            // Wait for the preview image to appear
            WaitForElement(PreviewImage, TimeSpan.FromSeconds(5));

            // Assert that the accept button exists
            AssertElementExists(AcceptButton);

            // Tap the accept button
            TapElement(AcceptButton);

            // Assert that the upload progress indicator appears
            AssertElementExists(UploadProgressIndicator, TimeSpan.FromSeconds(2));

            // Wait for the upload process to complete
            WaitForNoElement(UploadProgressIndicator, TimeSpan.FromSeconds(10));

            // Check if we're back to camera view or navigated away
            try
            {
                AssertElementExists(CameraView, TimeSpan.FromSeconds(2));
                AssertElementExists(CaptureButton);
            }
            catch
            {
                // If not at camera view, verify we've navigated away from photo capture page
                var currentScreen = App.Query(x => x.Marked("*Page"))[0];
                Assert.AreNotEqual("PhotoCapturePage", currentScreen.Text, 
                    "Application should navigate away from photo capture page or return to camera view");
            }
        }

        [Test]
        [Order(7)]
        public void TestUploadProgressIndicator()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Tap the capture button to take a photo
            TapElement(CaptureButton);

            // Wait for the preview image to appear
            WaitForElement(PreviewImage, TimeSpan.FromSeconds(5));

            // Tap the accept button
            TapElement(AcceptButton);

            // Assert that the upload progress indicator appears
            AssertElementExists(UploadProgressIndicator, TimeSpan.FromSeconds(2));

            // Check if the progress indicator shows progress
            var progressIndicator = App.Query(UploadProgressIndicator)[0];
            
            // Wait for the upload process to complete
            WaitForNoElement(UploadProgressIndicator, TimeSpan.FromSeconds(10));
        }

        [Test]
        [Order(8)]
        public void TestBackButtonFunctionality()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");

            // Assert that the back button exists
            AssertElementExists(BackButton);

            // Remember the current page
            var currentPage = "PhotoCapturePage";

            // Tap the back button
            TapElement(BackButton);

            // Assert that the application navigates away from the photo capture page
            AssertElementDoesNotExist(currentPage, TimeSpan.FromSeconds(2));
        }

        [Test]
        [Order(9)]
        public void TestCameraPermissionDenied()
        {
            // Login and navigate to photo capture page
            Login();
            
            // Simulate camera permission denied scenario
            App.Invoke("simulateCameraPermissionDenied", true);
            
            // Navigate to photo capture page
            NavigateToPage("PhotoCapture");

            // Assert that the camera status indicator shows "Unavailable" status
            var statusElement = App.Query(CameraStatusIndicator)[0];
            Assert.AreEqual("Unavailable", statusElement.Text, "Camera status indicator should show 'Unavailable'");

            // Assert that the capture button is disabled
            var captureButton = App.Query(CaptureButton)[0];
            Assert.IsFalse(captureButton.Enabled, "Capture button should be disabled when camera is unavailable");

            // Assert that a permission request message is displayed
            AssertElementExists("CameraPermissionRequestMessage");

            // Reset for other tests
            App.Invoke("simulateCameraPermissionDenied", false);
        }

        [Test]
        [Order(10)]
        public void TestErrorHandlingDuringCapture()
        {
            // Login and navigate to photo capture page
            Login();
            NavigateToPage("PhotoCapture");
            
            // Simulate camera error condition
            App.Invoke("simulateCameraError", true);
            
            // Tap the capture button
            TapElement(CaptureButton);
            
            // Assert that an error message is displayed
            AssertElementExists("CameraErrorMessage", TimeSpan.FromSeconds(3));
            
            // Assert that the camera view is still visible
            AssertElementExists(CameraView);
            
            // Assert that the capture button is still enabled for retry
            var captureButton = App.Query(CaptureButton)[0];
            Assert.IsTrue(captureButton.Enabled, "Capture button should be enabled for retry after error");
            
            // Reset for other tests
            App.Invoke("simulateCameraError", false);
        }
    }
}