using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using System; // version 8.0.0
using SecurityPatrol.Constants; // Internal import
using SecurityPatrol.Views; // Internal import

namespace SecurityPatrol
{
    /// <summary>
    /// The shell navigation container for the Security Patrol application that defines the application's navigation structure and handles route registration
    /// </summary>
    public partial class AppShell : Shell
    {
        /// <summary>
        /// Initializes a new instance of the AppShell class
        /// </summary>
        public AppShell()
        {
            InitializeComponent(); // Call InitializeComponent() to load XAML resources
            RegisterRoutes(); // Register routes for pages that are not directly defined in XAML
            Navigating += OnNavigating; // Set up any event handlers for navigation events
            Navigated += OnNavigated; // Set up any event handlers for navigation events
        }

        /// <summary>
        /// Registers routes for pages that are not directly defined in XAML
        /// </summary>
        private void RegisterRoutes()
        {
            Routing.RegisterRoute(NavigationConstants.PhoneEntryPage, typeof(PhoneEntryPage)); // Register PhoneEntryPage route
            Routing.RegisterRoute(NavigationConstants.VerificationPage, typeof(VerificationPage)); // Register VerificationPage route
            Routing.RegisterRoute(NavigationConstants.TimeHistoryPage, typeof(TimeHistoryPage)); // Register TimeHistoryPage route
            Routing.RegisterRoute(NavigationConstants.LocationSelectionPage, typeof(LocationSelectionPage)); // Register LocationSelectionPage route
            Routing.RegisterRoute(NavigationConstants.CheckpointListPage, typeof(CheckpointListPage)); // Register CheckpointListPage route
            Routing.RegisterRoute(NavigationConstants.PhotoGalleryPage, typeof(PhotoGalleryPage)); // Register PhotoGalleryPage route
            Routing.RegisterRoute(NavigationConstants.PhotoDetailPage, typeof(PhotoDetailPage)); // Register PhotoDetailPage route
            Routing.RegisterRoute(NavigationConstants.ReportListPage, typeof(ReportListPage)); // Register ReportListPage route
            Routing.RegisterRoute(NavigationConstants.ReportDetailsPage, typeof(ReportDetailsPage)); // Register ReportDetailsPage route
        }

        /// <summary>
        /// Event handler for Shell navigating event
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A ShellNavigatingEventArgs that contains the event data.</param>
        private void OnNavigating(object sender, ShellNavigatingEventArgs e)
        {
            // Handle any pre-navigation logic
            // Optionally cancel navigation if needed
        }

        /// <summary>
        /// Event handler for Shell navigated event
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A ShellNavigatedEventArgs that contains the event data.</param>
        private void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            // Handle any post-navigation logic
            // Update UI state based on current route if needed
        }
    }
}