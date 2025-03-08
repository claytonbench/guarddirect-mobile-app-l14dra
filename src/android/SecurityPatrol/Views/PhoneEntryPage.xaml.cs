using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Code-behind class for the phone number entry page in the authentication flow
    /// This is the first screen where users enter their phone number to receive a verification code
    /// </summary>
    public partial class PhoneEntryPage : ContentPage
    {
        /// <summary>
        /// The ViewModel associated with this page
        /// </summary>
        public PhoneEntryViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PhoneEntryPage class
        /// </summary>
        public PhoneEntryPage()
        {
            InitializeComponent();
            
            // Resolve the ViewModel from dependency injection
            ViewModel = App.Current.Handler.MauiContext.Services.GetService<PhoneEntryViewModel>();
            
            // Set the ViewModel as the BindingContext for data binding
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Initialize the ViewModel when the page appears
            await ViewModel.InitializeAsync();
        }

        /// <summary>
        /// Called when the page disappears from screen
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // No cleanup needed currently, but method is overridden for consistency
        }
    }
}