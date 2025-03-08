using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SecurityPatrol.Helpers
{
    /// <summary>
    /// Static utility class providing methods for displaying various types of dialogs and alerts to the user.
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Displays an alert dialog with a title, message, and OK button.
        /// </summary>
        /// <param name="title">The title of the alert dialog.</param>
        /// <param name="message">The message to display in the alert dialog.</param>
        /// <returns>A task that completes when the user dismisses the dialog.</returns>
        public static async Task DisplayAlertAsync(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        /// <summary>
        /// Displays a confirmation dialog with a title, message, and accept/cancel buttons.
        /// </summary>
        /// <param name="title">The title of the confirmation dialog.</param>
        /// <param name="message">The message to display in the confirmation dialog.</param>
        /// <param name="accept">The text for the accept button.</param>
        /// <param name="cancel">The text for the cancel button.</param>
        /// <returns>True if the user accepted, false if canceled.</returns>
        public static async Task<bool> DisplayConfirmationAsync(string title, string message, string accept, string cancel)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }

        /// <summary>
        /// Displays an action sheet with multiple options for the user to choose from.
        /// </summary>
        /// <param name="title">The title of the action sheet.</param>
        /// <param name="cancel">The text for the cancel button.</param>
        /// <param name="destruction">The text for the destruction button, or null if not needed.</param>
        /// <param name="buttons">An array of button texts to display as options.</param>
        /// <returns>The text of the button that was clicked, or null if the user dismissed the dialog.</returns>
        public static async Task<string> DisplayActionSheetAsync(string title, string cancel, string destruction, string[] buttons)
        {
            return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);
        }

        /// <summary>
        /// Displays an error alert with a standardized title and the provided error message.
        /// </summary>
        /// <param name="errorMessage">The error message to display.</param>
        /// <returns>A task that completes when the user dismisses the dialog.</returns>
        public static async Task DisplayErrorAsync(string errorMessage)
        {
            await Application.Current.MainPage.DisplayAlert("Error", errorMessage, "OK");
        }

        /// <summary>
        /// Displays a success alert with a standardized title and the provided success message.
        /// </summary>
        /// <param name="successMessage">The success message to display.</param>
        /// <returns>A task that completes when the user dismisses the dialog.</returns>
        public static async Task DisplaySuccessAsync(string successMessage)
        {
            await Application.Current.MainPage.DisplayAlert("Success", successMessage, "OK");
        }

        /// <summary>
        /// Displays a prompt dialog that allows the user to enter text input.
        /// </summary>
        /// <param name="title">The title of the prompt dialog.</param>
        /// <param name="message">The message to display in the prompt dialog.</param>
        /// <param name="accept">The text for the accept button.</param>
        /// <param name="cancel">The text for the cancel button.</param>
        /// <param name="placeholder">The placeholder text for the input field.</param>
        /// <param name="maxLength">The maximum length of input text, or 0 for unlimited.</param>
        /// <param name="keyboard">The keyboard type to use for input, or null for default.</param>
        /// <returns>The text entered by the user, or null if canceled.</returns>
        public static async Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = 0, Keyboard keyboard = null)
        {
            return await Application.Current.MainPage.DisplayPromptAsync(
                title, 
                message, 
                accept, 
                cancel, 
                placeholder, 
                maxLength, 
                keyboard ?? Keyboard.Default);
        }

        /// <summary>
        /// Creates and returns a loading dialog that can be shown and hidden programmatically.
        /// </summary>
        /// <param name="message">The message to display in the loading dialog.</param>
        /// <returns>A LoadingDialog instance that can be shown and hidden.</returns>
        public static LoadingDialog DisplayLoadingDialog(string message)
        {
            return new LoadingDialog(message);
        }
    }

    /// <summary>
    /// A class representing a loading dialog that can be shown and hidden programmatically.
    /// </summary>
    public class LoadingDialog
    {
        private readonly ContentPage _dialogPage;
        
        /// <summary>
        /// Gets a value indicating whether the loading dialog is currently visible.
        /// </summary>
        public bool IsShowing { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LoadingDialog class with the specified message.
        /// </summary>
        /// <param name="message">The message to display in the loading dialog.</param>
        public LoadingDialog(string message)
        {
            // Create activity indicator
            var activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HeightRequest = 50,
                WidthRequest = 50
            };

            // Create message label
            var messageLabel = new Label
            {
                Text = message,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 15, 0, 0)
            };

            // Create content layout
            var contentLayout = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10,
                Padding = new Thickness(30),
                BackgroundColor = Colors.Transparent,
                Children = { activityIndicator, messageLabel }
            };

            // Create container frame
            var frame = new Frame
            {
                BackgroundColor = Colors.FromArgb("#80000000"),
                CornerRadius = 10,
                HasShadow = true,
                Content = contentLayout
            };

            // Configure dialog page
            _dialogPage = new ContentPage
            {
                BackgroundColor = Colors.Transparent,
                Content = frame
            };
            
            // Allow taps to pass through to the background (but keep the dialog modal)
            _dialogPage.SetValue(NavigationPage.HasNavigationBarProperty, false);
            _dialogPage.SetValue(NavigationPage.HasBackButtonProperty, false);
            
            IsShowing = false;
        }

        /// <summary>
        /// Shows the loading dialog.
        /// </summary>
        /// <returns>A task that completes when the dialog is displayed.</returns>
        public async Task ShowAsync()
        {
            if (IsShowing)
                return;

            await Application.Current.MainPage.Navigation.PushModalAsync(_dialogPage, false);
            IsShowing = true;
        }

        /// <summary>
        /// Hides the loading dialog.
        /// </summary>
        /// <returns>A task that completes when the dialog is hidden.</returns>
        public async Task HideAsync()
        {
            if (!IsShowing)
                return;

            await Application.Current.MainPage.Navigation.PopModalAsync(false);
            IsShowing = false;
        }

        /// <summary>
        /// Updates the message displayed in the loading dialog.
        /// </summary>
        /// <param name="message">The new message to display.</param>
        public void UpdateMessage(string message)
        {
            // Find the label in the dialog page structure and update its text
            if (_dialogPage.Content is Frame frame &&
                frame.Content is VerticalStackLayout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Label label)
                    {
                        label.Text = message;
                        break;
                    }
                }
            }
        }
    }
}