using Microsoft.Maui.Controls;
using SecurityPatrol.Models;
using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// A custom ContentView control that displays an activity report item with text, timestamp, and sync status indicator.
    /// </summary>
    public partial class ReportItem : ContentView
    {
        /// <summary>
        /// Bindable property for the Report model.
        /// </summary>
        public static readonly BindableProperty ReportProperty = BindableProperty.Create(
            propertyName: nameof(Report),
            returnType: typeof(ReportModel),
            declaringType: typeof(ReportItem),
            defaultValue: null,
            propertyChanged: OnReportChanged);

        /// <summary>
        /// Gets or sets the Report model to display in this control.
        /// </summary>
        public ReportModel Report
        {
            get => (ReportModel)GetValue(ReportProperty);
            set => SetValue(ReportProperty, value);
        }

        /// <summary>
        /// Gets or sets the command to execute when the report item is tapped.
        /// </summary>
        public ICommand TapCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the ReportItem class.
        /// </summary>
        public ReportItem()
        {
            InitializeComponent();
            BindingContext = this;
            
            // Add tap gesture recognizer to handle item selection
            var tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) => 
            {
                if (TapCommand?.CanExecute(Report) == true)
                {
                    TapCommand.Execute(Report);
                }
            };
            GestureRecognizers.Add(tapGestureRecognizer);
        }

        /// <summary>
        /// Called when the Report property changes.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnReportChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is ReportItem reportItem)
            {
                // The property has been updated - no additional action needed
                // as the UI will automatically update due to data binding
            }
        }
    }
}