using Microsoft.Maui.Controls; // version 8.0.0
using System; // version 8.0.0
using SecurityPatrol.Models;
using SecurityPatrol.Constants;
using SecurityPatrol.ViewModels; // version 1.0.0

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays a list of activity reports with options to create new reports, view details, and sync with the backend
    /// </summary>
    public partial class ReportListPage : ContentPage
    {
        /// <summary>
        /// The ViewModel associated with this page
        /// </summary>
        public ReportListViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ReportListPage class
        /// </summary>
        public ReportListPage()
        {
            InitializeComponent();
            
            // Get the ViewModel from the dependency injection container
            ViewModel = Handler?.MauiContext?.Services.GetService<ReportListViewModel>();
            
            // Fallback to creating a new instance if DI is not configured
            ViewModel ??= new ReportListViewModel();
            
            // Set the BindingContext to the ViewModel
            BindingContext = ViewModel;
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Call OnAppearing on ViewModel if it exists
            ViewModel?.OnAppearing();
            
            // Refresh the report list
            if (ViewModel?.RefreshCommand?.CanExecute(null) == true)
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        }

        /// <summary>
        /// Called when the page disappears from screen
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Call OnDisappearing on ViewModel if it exists
            ViewModel?.OnDisappearing();
        }

        /// <summary>
        /// Handles the create report button click event
        /// </summary>
        private void OnCreateReportClicked(object sender, EventArgs e)
        {
            if (ViewModel?.CreateReportCommand?.CanExecute(null) == true)
            {
                ViewModel.CreateReportCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles the sync all button click event
        /// </summary>
        private void OnSyncAllClicked(object sender, EventArgs e)
        {
            if (ViewModel?.SyncAllCommand?.CanExecute(null) == true)
            {
                ViewModel.SyncAllCommand.Execute(null);
            }
        }

        /// <summary>
        /// Handles the selection of a report item in the collection view
        /// </summary>
        private void OnReportSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                var selectedReport = e.CurrentSelection[0] as ReportModel;
                if (selectedReport != null && ViewModel?.SelectReportCommand?.CanExecute(selectedReport) == true)
                {
                    ViewModel.SelectReportCommand.Execute(selectedReport);
                }

                // Reset selection
                if (sender is CollectionView collectionView)
                {
                    collectionView.SelectedItem = null;
                }
            }
        }
    }
}