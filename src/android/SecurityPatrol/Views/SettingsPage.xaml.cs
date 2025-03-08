using System;
using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// The Settings page of the Security Patrol application that allows users to configure application settings,
    /// manage preferences, and perform account-related actions.
    /// </summary>
    public partial class SettingsPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel for this page.
        /// </summary>
        public SettingsViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class with the SettingsViewModel.
        /// </summary>
        /// <param name="viewModel">The ViewModel for this page.</param>
        /// <exception cref="ArgumentNullException">Thrown if viewModel is null.</exception>
        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
            InitializeComponent();
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