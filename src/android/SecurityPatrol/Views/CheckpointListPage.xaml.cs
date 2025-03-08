using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.ViewModels;
using System;
using System.Threading.Tasks;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays a list of checkpoints for a selected patrol location with filtering and verification capabilities
    /// </summary>
    public partial class CheckpointListPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel instance for easier access in the code-behind
        /// </summary>
        public CheckpointListViewModel ViewModel => BindingContext as CheckpointListViewModel;

        /// <summary>
        /// Initializes a new instance of the CheckpointListPage class
        /// </summary>
        public CheckpointListPage()
        {
            InitializeComponent();
            
            // The BindingContext will be set through dependency injection or in XAML
            // We can access it through the ViewModel property for easier use in code-behind
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Call the ViewModel's OnAppearing method to initialize data
            if (ViewModel != null)
            {
                await ViewModel.OnAppearing();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Call the ViewModel's OnDisappearing method to perform cleanup
            if (ViewModel != null)
            {
                await ViewModel.OnDisappearing();
            }
        }

        /// <summary>
        /// Called when the LocationId property changes, typically when navigating to this page with a specific location
        /// </summary>
        /// <param name="locationId">The ID of the location to display checkpoints for</param>
        public void OnLocationIdChanged(int locationId)
        {
            if (ViewModel != null)
            {
                // Update the LocationId in the ViewModel which will trigger checkpoint loading
                ViewModel.LocationId = locationId;
            }
        }
    }
}