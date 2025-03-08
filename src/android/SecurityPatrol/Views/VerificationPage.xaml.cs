using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that implements the checkpoint verification functionality, allowing security personnel 
    /// to verify their presence at specific checkpoints during patrols.
    /// </summary>
    public partial class VerificationPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel from the BindingContext.
        /// </summary>
        private VerificationViewModel ViewModel => BindingContext as VerificationViewModel;

        /// <summary>
        /// Initializes a new instance of the VerificationPage class.
        /// </summary>
        public VerificationPage()
        {
            InitializeComponent();
            // BindingContext is set via dependency injection in MauiProgram.cs
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (ViewModel != null)
            {
                await ViewModel.InitializeAsync();
            }
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            if (ViewModel != null)
            {
                await ViewModel.OnNavigatedFrom();
            }
        }

        /// <summary>
        /// Called when the page is navigated to, passing navigation parameters to the ViewModel.
        /// </summary>
        /// <param name="parameters">Navigation parameters containing checkpoint and location IDs.</param>
        public async void OnNavigatedTo(Dictionary<string, object> parameters)
        {
            if (ViewModel != null)
            {
                await ViewModel.OnNavigatedTo(parameters);
            }
        }
    }
}