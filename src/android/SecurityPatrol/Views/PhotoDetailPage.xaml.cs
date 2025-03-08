using Microsoft.Maui.Controls; // version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays detailed information about a captured photo, including the image itself, 
    /// metadata, and synchronization status.
    /// </summary>
    public partial class PhotoDetailPage : ContentPage
    {
        /// <summary>
        /// Gets the view model associated with this page.
        /// </summary>
        public PhotoDetailViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhotoDetailPage"/> class.
        /// </summary>
        /// <param name="viewModel">The view model for the photo detail page.</param>
        public PhotoDetailPage(PhotoDetailViewModel viewModel)
        {
            InitializeComponent();
            
            ViewModel = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }
    }
}