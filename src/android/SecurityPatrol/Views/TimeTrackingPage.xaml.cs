using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that allows security personnel to clock in and out of their shifts,
    /// view their current status, and access their time history.
    /// </summary>
    public partial class TimeTrackingPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel for this page.
        /// </summary>
        private TimeTrackingViewModel ViewModel => BindingContext as TimeTrackingViewModel;

        /// <summary>
        /// Initializes a new instance of the TimeTrackingPage class.
        /// </summary>
        public TimeTrackingPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Initialize the ViewModel to fetch current clock status
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}