using Microsoft.Maui.Controls; // Version 8.0.0
using System; // Version 8.0.0
using System.Threading.Tasks; // Version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that allows users to select a patrol location from a list of available locations
    /// </summary>
    public partial class LocationSelectionPage : ContentPage
    {
        /// <summary>
        /// Gets the view model associated with this page
        /// </summary>
        private LocationSelectionViewModel ViewModel { get; set; }

        /// <summary>
        /// Initializes a new instance of the LocationSelectionPage class
        /// </summary>
        public LocationSelectionPage()
        {
            InitializeComponent();
            ViewModel = BindingContext as LocationSelectionViewModel;
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Check if ViewModel was set in constructor, otherwise get it from current binding context
            if (ViewModel == null)
            {
                ViewModel = BindingContext as LocationSelectionViewModel;
            }

            // Load locations when the page appears if ViewModel is available
            if (ViewModel != null)
            {
                await ViewModel.LoadLocationsAsync();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}