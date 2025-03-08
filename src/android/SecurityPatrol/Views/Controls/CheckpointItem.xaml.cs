using Microsoft.Maui.Controls;
using SecurityPatrol.Models;
using System;
using System.Windows.Input;

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// A reusable UI control that displays checkpoint information with status indicators 
    /// and interaction capabilities for selection and verification.
    /// </summary>
    public partial class CheckpointItem : ContentView
    {
        #region Bindable Properties
        
        /// <summary>
        /// Bindable property for the Checkpoint model
        /// </summary>
        public static readonly BindableProperty CheckpointProperty = 
            BindableProperty.Create(
                nameof(Checkpoint), 
                typeof(CheckpointModel), 
                typeof(CheckpointItem), 
                null, 
                propertyChanged: OnCheckpointChanged);
                
        /// <summary>
        /// Bindable property for the Distance to checkpoint
        /// </summary>
        public static readonly BindableProperty DistanceProperty = 
            BindableProperty.Create(
                nameof(Distance), 
                typeof(double), 
                typeof(CheckpointItem), 
                0.0, 
                propertyChanged: OnDistanceChanged);
                
        /// <summary>
        /// Bindable property for the IsNearby status
        /// </summary>
        public static readonly BindableProperty IsNearbyProperty = 
            BindableProperty.Create(
                nameof(IsNearby), 
                typeof(bool), 
                typeof(CheckpointItem), 
                false, 
                propertyChanged: OnIsNearbyChanged);
                
        /// <summary>
        /// Bindable property for the IsSelected status
        /// </summary>
        public static readonly BindableProperty IsSelectedProperty = 
            BindableProperty.Create(
                nameof(IsSelected), 
                typeof(bool), 
                typeof(CheckpointItem), 
                false, 
                propertyChanged: OnIsSelectedChanged);
                
        /// <summary>
        /// Bindable property for the SelectCommand
        /// </summary>
        public static readonly BindableProperty SelectCommandProperty = 
            BindableProperty.Create(
                nameof(SelectCommand), 
                typeof(ICommand), 
                typeof(CheckpointItem), 
                null, 
                propertyChanged: OnSelectCommandChanged);
                
        /// <summary>
        /// Bindable property for the VerifyCommand
        /// </summary>
        public static readonly BindableProperty VerifyCommandProperty = 
            BindableProperty.Create(
                nameof(VerifyCommand), 
                typeof(ICommand), 
                typeof(CheckpointItem), 
                null, 
                propertyChanged: OnVerifyCommandChanged);
                
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets or sets the checkpoint model displayed by this control
        /// </summary>
        public CheckpointModel Checkpoint
        {
            get => (CheckpointModel)GetValue(CheckpointProperty);
            set => SetValue(CheckpointProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the distance to this checkpoint in meters
        /// </summary>
        public double Distance
        {
            get => (double)GetValue(DistanceProperty);
            set => SetValue(DistanceProperty, value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the user is within proximity of this checkpoint
        /// </summary>
        public bool IsNearby
        {
            get => (bool)GetValue(IsNearbyProperty);
            set => SetValue(IsNearbyProperty, value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether this checkpoint is currently selected
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the command executed when this checkpoint is selected
        /// </summary>
        public ICommand SelectCommand
        {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the command executed when this checkpoint is verified
        /// </summary>
        public ICommand VerifyCommand
        {
            get => (ICommand)GetValue(VerifyCommandProperty);
            set => SetValue(VerifyCommandProperty, value);
        }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the CheckpointItem class
        /// </summary>
        public CheckpointItem()
        {
            InitializeComponent();
        }
        
        #endregion
        
        #region Property Change Handlers
        
        /// <summary>
        /// Handles changes to the Checkpoint property
        /// </summary>
        private static void OnCheckpointChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            control.BindingContext = newValue; // Set the Checkpoint as the BindingContext
            control.UpdateVisualState();
        }
        
        /// <summary>
        /// Handles changes to the Distance property
        /// </summary>
        private static void OnDistanceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            // Update distance display if needed
        }
        
        /// <summary>
        /// Handles changes to the IsNearby property
        /// </summary>
        private static void OnIsNearbyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            control.UpdateVisualState();
        }
        
        /// <summary>
        /// Handles changes to the IsSelected property
        /// </summary>
        private static void OnIsSelectedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            control.UpdateVisualState();
        }
        
        /// <summary>
        /// Handles changes to the SelectCommand property
        /// </summary>
        private static void OnSelectCommandChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            // Command binding typically handled in XAML
        }
        
        /// <summary>
        /// Handles changes to the VerifyCommand property
        /// </summary>
        private static void OnVerifyCommandChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (CheckpointItem)bindable;
            // Command binding typically handled in XAML
        }
        
        #endregion
        
        #region Visual State Management
        
        /// <summary>
        /// Updates the visual state of the control based on current properties
        /// </summary>
        private void UpdateVisualState()
        {
            if (IsSelected)
            {
                VisualStateManager.GoToState(this, "Selected");
            }
            else if (IsNearby)
            {
                VisualStateManager.GoToState(this, "Nearby");
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal");
            }
        }
        
        #endregion
    }
}