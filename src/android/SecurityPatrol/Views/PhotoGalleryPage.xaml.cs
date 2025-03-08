using Microsoft.Maui.Controls; // v8.0.0
using Microsoft.Extensions.DependencyInjection; // v8.0.0
using SecurityPatrol.ViewModels; // v1.0.0

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays a gallery of photos captured within the Security Patrol application,
    /// allowing users to view photo details or capture new photos.
    /// </summary>
    public partial class PhotoGalleryPage : ContentPage
    {
        /// <summary>
        /// Gets the view model associated with this page.
        /// </summary>
        public PhotoGalleryViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PhotoGalleryPage class.
        /// </summary>
        public PhotoGalleryPage()
        {
            InitializeComponent();

            // Resolve PhotoGalleryViewModel from dependency injection container
            ViewModel = Application.Current.Handler.MauiContext.Services.GetService<PhotoGalleryViewModel>() 
                ?? new PhotoGalleryViewModel();
                
            // Set BindingContext to ViewModel for data binding
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Load photos and refresh synchronization status
            ViewModel.OnAppearing();
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Perform any cleanup
            ViewModel.OnDisappearing();
        }
    }
}