using Microsoft.Maui.Controls; // version 8.0.0
using SecurityPatrol.ViewModels;

namespace SecurityPatrol.Views
{
    /// <summary>
    /// Page that displays detailed information about a specific activity report and allows users to view, edit, and delete reports
    /// </summary>
    public partial class ReportDetailsPage : ContentPage
    {
        /// <summary>
        /// Gets the ViewModel for this page
        /// </summary>
        public ReportDetailsViewModel ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ReportDetailsPage class with the ReportDetailsViewModel
        /// </summary>
        /// <param name="viewModel">The ViewModel to use for data binding and command handling</param>
        public ReportDetailsPage(ReportDetailsViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = ViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// Called when the page appears on screen
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.OnAppearing();
        }

        /// <summary>
        /// Called when the page disappears from screen
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel.OnDisappearing();
        }

        /// <summary>
        /// Event handler for text changes in the report editor
        /// </summary>
        /// <param name="sender">The control that triggered the event</param>
        /// <param name="e">Event arguments containing the old and new text values</param>
        private void EditorTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.ReportText = e.NewTextValue;
            }
        }
    }
}