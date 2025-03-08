using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using SecurityPatrol.ViewModels;
using SecurityPatrol.Views.Controls;
using SecurityPatrol.Models;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that implements the patrol management functionality, allowing security personnel to view patrol locations on a map,
    /// select checkpoints, and verify their presence at checkpoints during patrols.
    /// </summary>
    public partial class PatrolPage : ContentPage
    {
        private PatrolViewModel ViewModel => BindingContext as PatrolViewModel;
        private LocationMapView _mapView;

        /// <summary>
        /// Initializes a new instance of the PatrolPage class.
        /// </summary>
        public PatrolPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Loaded event of the LocationMapView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMapViewLoaded(object sender, EventArgs e)
        {
            if (sender is LocationMapView mapView)
            {
                _mapView = mapView;
                
                // Get the ViewModel from the BindingContext
                var viewModel = BindingContext as PatrolViewModel;
                
                // Pass the mapView to the ViewModel for initialization
                if (viewModel != null)
                {
                    _mapView.IsShowingUser = true;
                }
            }
        }

        /// <summary>
        /// Handles the selection change event of the location picker.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLocationSelectionChanged(object sender, EventArgs e)
        {
            if (ViewModel == null)
                return;
                
            if (ViewModel.SelectedLocation == null)
                return;
                
            // The ViewModel will handle updating the map and loading checkpoints 
            // through data binding and commands
        }

        /// <summary>
        /// Handles the selection change event of the checkpoint collection view.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The selection change event arguments.</param>
        private void OnCheckpointSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel == null)
                return;
                
            if (e.CurrentSelection == null || e.CurrentSelection.Count == 0)
                return;
                
            // Get the selected checkpoint from the current selection
            var checkpoint = e.CurrentSelection[0] as CheckpointModel;
            if (checkpoint != null)
            {
                // Update the ViewModel's SelectedCheckpoint property
                ViewModel.SelectedCheckpoint = checkpoint;
                
                // This will trigger map updates through the ViewModel
            }
        }

        /// <summary>
        /// Called when the page appears on screen.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                if (ViewModel != null)
                {
                    // Create parameters dictionary with the map view reference
                    var parameters = new Dictionary<string, object>();
                    if (_mapView != null)
                    {
                        parameters["MapView"] = _mapView;
                    }
                    
                    // Call the ViewModel's OnNavigatedTo method with the map view reference
                    await ViewModel.OnNavigatedTo(parameters);
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in OnAppearing: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the page disappears from screen.
        /// </summary>
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            
            try
            {
                if (ViewModel != null)
                {
                    // Clean up resources by calling the ViewModel's OnNavigatedFrom method
                    await ViewModel.OnNavigatedFrom();
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in OnDisappearing: {ex.Message}");
            }
        }
    }
}