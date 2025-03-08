using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that allows security personnel to create and submit activity reports
    /// with their current location. Supports offline operation with data queuing.
    /// </summary>
    public partial class ActivityReportPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityReportPage"/> class.
        /// </summary>
        public ActivityReportPage()
        {
            InitializeComponent();
            
            // Get the ViewModel from the dependency injection container
            BindingContext = Application.Current.Handler.MauiContext.Services.GetService<ActivityReportViewModel>();
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialize the ViewModel when the page appears
            if (BindingContext is ActivityReportViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Additional cleanup can be added here if needed in the future
        }
    }
}