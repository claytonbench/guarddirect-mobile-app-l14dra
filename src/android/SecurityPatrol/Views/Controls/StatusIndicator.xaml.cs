using Microsoft.Maui.Controls; // Version 8.0.0

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// Defines the different types of statuses that can be displayed by the StatusIndicator.
    /// </summary>
    public enum StatusType
    {
        /// <summary>
        /// Informational status.
        /// </summary>
        Info,
        
        /// <summary>
        /// Success status.
        /// </summary>
        Success,
        
        /// <summary>
        /// Warning status.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error status.
        /// </summary>
        Error,
        
        /// <summary>
        /// Checkpoint verification status.
        /// </summary>
        Verification,
        
        /// <summary>
        /// Clock in status.
        /// </summary>
        ClockIn,
        
        /// <summary>
        /// Clock out status.
        /// </summary>
        ClockOut,
        
        /// <summary>
        /// Synchronization status.
        /// </summary>
        Sync,
        
        /// <summary>
        /// Location tracking status.
        /// </summary>
        Location
    }

    /// <summary>
    /// A reusable control that provides visual status indicators for various states in the application,
    /// such as verification status, clock status, location tracking status, and synchronization status.
    /// </summary>
    public partial class StatusIndicator : ContentView
    {
        #region Bindable Properties

        /// <summary>
        /// Bindable property for the type of status to display.
        /// </summary>
        public static readonly BindableProperty StatusTypeProperty = BindableProperty.Create(
            propertyName: nameof(StatusType),
            returnType: typeof(StatusType),
            declaringType: typeof(StatusIndicator),
            defaultValue: StatusType.Info,
            propertyChanged: OnStatusTypeChanged);

        /// <summary>
        /// Bindable property for the status text to display.
        /// </summary>
        public static readonly BindableProperty StatusTextProperty = BindableProperty.Create(
            propertyName: nameof(StatusText),
            returnType: typeof(string),
            declaringType: typeof(StatusIndicator),
            defaultValue: string.Empty,
            propertyChanged: OnStatusTextChanged);

        /// <summary>
        /// Bindable property indicating whether the status is active.
        /// </summary>
        public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
            propertyName: nameof(IsActive),
            returnType: typeof(bool),
            declaringType: typeof(StatusIndicator),
            defaultValue: false,
            propertyChanged: OnIsActiveChanged);

        /// <summary>
        /// Bindable property indicating whether to show the status text.
        /// </summary>
        public static readonly BindableProperty ShowTextProperty = BindableProperty.Create(
            propertyName: nameof(ShowText),
            returnType: typeof(bool),
            declaringType: typeof(StatusIndicator),
            defaultValue: true,
            propertyChanged: OnShowTextChanged);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the type of status to display.
        /// </summary>
        public StatusType StatusType
        {
            get => (StatusType)GetValue(StatusTypeProperty);
            set => SetValue(StatusTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the status text to display.
        /// </summary>
        public string StatusText
        {
            get => (string)GetValue(StatusTextProperty);
            set => SetValue(StatusTextProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the status is active.
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the status text.
        /// </summary>
        public bool ShowText
        {
            get => (bool)GetValue(ShowTextProperty);
            set => SetValue(ShowTextProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusIndicator"/> class.
        /// </summary>
        public StatusIndicator()
        {
            InitializeComponent();
            UpdateVisualState();
        }

        #region Property Changed Handlers

        private static void OnStatusTypeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is StatusIndicator indicator)
            {
                indicator.UpdateStatusAppearance();
            }
        }

        private static void OnStatusTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is StatusIndicator indicator)
            {
                indicator.UpdateLabelText();
            }
        }

        private static void OnIsActiveChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is StatusIndicator indicator)
            {
                indicator.UpdateVisualState();
            }
        }

        private static void OnShowTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is StatusIndicator indicator)
            {
                indicator.UpdateTextVisibility();
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Updates the visual state of the control based on the current property values.
        /// </summary>
        private void UpdateVisualState()
        {
            UpdateStatusAppearance();
            UpdateLabelText();
            UpdateTextVisibility();
        }

        /// <summary>
        /// Updates the appearance of the status indicator based on the StatusType and IsActive properties.
        /// </summary>
        private void UpdateStatusAppearance()
        {
            // The corresponding XAML file should have a visual element (like an indicator circle or icon)
            // that this method will update based on StatusType and IsActive
            
            string activeState = IsActive ? "Active" : "Inactive";
            VisualStateManager.GoToState(this, activeState);

            string typeState = StatusType.ToString();
            VisualStateManager.GoToState(this, typeState);
        }

        /// <summary>
        /// Updates the label text with the current StatusText value.
        /// </summary>
        private void UpdateLabelText()
        {
            // The corresponding XAML file should have a Label element
            // that this method will update with the StatusText
        }

        /// <summary>
        /// Updates the visibility of the status text based on the ShowText property.
        /// </summary>
        private void UpdateTextVisibility()
        {
            string textVisibilityState = ShowText ? "TextVisible" : "TextHidden";
            VisualStateManager.GoToState(this, textVisibilityState);
        }

        #endregion
    }
}