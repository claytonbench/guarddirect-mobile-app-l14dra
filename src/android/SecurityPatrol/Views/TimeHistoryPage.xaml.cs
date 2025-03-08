using Microsoft.Maui.Controls;  // Version 8.0.0
using SecurityPatrol.ViewModels;  // Internal import

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays a chronological list of clock in/out events for security personnel
    /// </summary>
    public partial class TimeHistoryPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the TimeHistoryPage class
        /// </summary>
        public TimeHistoryPage()
        {
            InitializeComponent();
            
            // BindingContext is typically set through dependency injection
            // The XAML page binds to TimeTrackingViewModel.RecentTimeRecords collection
            // and pull-to-refresh is bound to TimeTrackingViewModel.RefreshCommand
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh the time records when the page appears
            if (BindingContext is TimeTrackingViewModel viewModel)
            {
                await viewModel.RefreshCommand.ExecuteAsync(null);
            }
        }
    }
}