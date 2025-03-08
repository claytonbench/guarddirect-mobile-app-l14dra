using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// The main dashboard page of the Security Patrol application that serves as a central hub
    /// for navigation and status monitoring.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel associated with this page.
        /// </summary>
        public MainViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class with the MainViewModel.
        /// </summary>
        /// <param name="viewModel">The view model for this page.</param>
        public MainPage(MainViewModel viewModel)
        {
            // Validate that viewModel is not null
            ArgumentNullException.ThrowIfNull(viewModel);

            // Set ViewModel property
            ViewModel = viewModel;
            
            // Set BindingContext to ViewModel for data binding
            BindingContext = ViewModel;
            
            // Initialize components from XAML
            InitializeComponent();
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Call the ViewModel's OnAppearing method to refresh status indicators and start monitoring
            ViewModel.OnAppearing();
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Call the ViewModel's OnDisappearing method to stop monitoring when not visible
            ViewModel.OnDisappearing();
        }
    }
}