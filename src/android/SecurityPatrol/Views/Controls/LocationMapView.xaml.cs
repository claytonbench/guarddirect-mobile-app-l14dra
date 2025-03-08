using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using SecurityPatrol.Models;

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// Custom control that provides an interactive map view for displaying user location and patrol checkpoints.
    /// </summary>
    public partial class LocationMapView : ContentView
    {
        #region Bindable Properties

        /// <summary>
        /// Bindable property for the Map control.
        /// </summary>
        public static readonly BindableProperty MapProperty = BindableProperty.Create(
            nameof(Map),
            typeof(Microsoft.Maui.Controls.Maps.Map),
            typeof(LocationMapView),
            null,
            propertyChanged: OnMapPropertyChanged);

        /// <summary>
        /// Gets or sets the Map control.
        /// </summary>
        public Microsoft.Maui.Controls.Maps.Map Map
        {
            get => (Microsoft.Maui.Controls.Maps.Map)GetValue(MapProperty);
            set => SetValue(MapProperty, value);
        }

        /// <summary>
        /// Bindable property for the collection of checkpoints to display on the map.
        /// </summary>
        public static readonly BindableProperty CheckpointsProperty = BindableProperty.Create(
            nameof(Checkpoints),
            typeof(IEnumerable<CheckpointModel>),
            typeof(LocationMapView),
            null,
            propertyChanged: OnCheckpointsPropertyChanged);

        /// <summary>
        /// Gets or sets the collection of checkpoints to display on the map.
        /// </summary>
        public IEnumerable<CheckpointModel> Checkpoints
        {
            get => (IEnumerable<CheckpointModel>)GetValue(CheckpointsProperty);
            set => SetValue(CheckpointsProperty, value);
        }

        /// <summary>
        /// Bindable property to determine if the map should show the user's current location.
        /// </summary>
        public static readonly BindableProperty IsShowingUserProperty = BindableProperty.Create(
            nameof(IsShowingUser),
            typeof(bool),
            typeof(LocationMapView),
            false,
            propertyChanged: OnIsShowingUserPropertyChanged);

        /// <summary>
        /// Gets or sets a value indicating whether the map should show the user's current location.
        /// </summary>
        public bool IsShowingUser
        {
            get => (bool)GetValue(IsShowingUserProperty);
            set => SetValue(IsShowingUserProperty, value);
        }

        /// <summary>
        /// Bindable property for the currently selected checkpoint.
        /// </summary>
        public static readonly BindableProperty SelectedCheckpointProperty = BindableProperty.Create(
            nameof(SelectedCheckpoint),
            typeof(CheckpointModel),
            typeof(LocationMapView),
            null,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedCheckpointPropertyChanged);

        /// <summary>
        /// Gets or sets the currently selected checkpoint.
        /// </summary>
        public CheckpointModel SelectedCheckpoint
        {
            get => (CheckpointModel)GetValue(SelectedCheckpointProperty);
            set => SetValue(SelectedCheckpointProperty, value);
        }

        /// <summary>
        /// Bindable property to indicate if the map is busy (loading data or processing).
        /// </summary>
        public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(LocationMapView),
            false);

        /// <summary>
        /// Gets or sets a value indicating whether the map is busy (loading data or processing).
        /// </summary>
        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        #endregion

        // Dictionary to keep track of pins corresponding to checkpoints by checkpoint ID
        private Dictionary<int, Pin> _checkpointPins;
        
        // MAUI map control instance
        private Microsoft.Maui.Controls.Maps.Map mapControl;

        /// <summary>
        /// Initializes a new instance of the LocationMapView class.
        /// </summary>
        public LocationMapView()
        {
            InitializeComponent();
            
            // Initialize the pins dictionary
            _checkpointPins = new Dictionary<int, Pin>();
            
            // Set up event handlers for the map control
            mapControl = this.FindByName<Microsoft.Maui.Controls.Maps.Map>("MapControl");
            
            if (mapControl != null)
            {
                mapControl.PinClicked += OnMapPinClicked;
            }
        }

        #region Property Change Handlers

        /// <summary>
        /// Handles changes to the Map bindable property.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnMapPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mapView = (LocationMapView)bindable;
            
            if (mapView.mapControl == null)
                return;
                
            if (newValue is Microsoft.Maui.Controls.Maps.Map newMap)
            {
                // Update map properties
                mapView.mapControl.MapType = newMap.MapType;
                mapView.mapControl.IsScrollEnabled = newMap.IsScrollEnabled;
                mapView.mapControl.IsZoomEnabled = newMap.IsZoomEnabled;
                mapView.mapControl.IsShowingUser = newMap.IsShowingUser;
            }
        }

        /// <summary>
        /// Handles changes to the Checkpoints bindable property.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnCheckpointsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mapView = (LocationMapView)bindable;
            var checkpoints = newValue as IEnumerable<CheckpointModel>;
            
            if (checkpoints != null)
            {
                mapView.UpdateCheckpoints(checkpoints);
            }
            else
            {
                // Clear all pins if checkpoints collection is null
                if (mapView.mapControl != null)
                {
                    mapView.mapControl.Pins.Clear();
                    mapView._checkpointPins.Clear();
                }
            }
        }

        /// <summary>
        /// Handles changes to the IsShowingUser bindable property.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnIsShowingUserPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mapView = (LocationMapView)bindable;
            
            if (mapView.mapControl != null)
            {
                mapView.mapControl.IsShowingUser = (bool)newValue;
            }
        }

        /// <summary>
        /// Handles changes to the SelectedCheckpoint bindable property.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnSelectedCheckpointPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var mapView = (LocationMapView)bindable;
            var oldCheckpoint = oldValue as CheckpointModel;
            var newCheckpoint = newValue as CheckpointModel;
            
            // Unhighlight the previously selected checkpoint
            if (oldCheckpoint != null)
            {
                mapView.HighlightCheckpoint(oldCheckpoint.Id, false);
            }
            
            // Highlight the newly selected checkpoint
            if (newCheckpoint != null)
            {
                mapView.HighlightCheckpoint(newCheckpoint.Id, true);
                
                // Center the map on the selected checkpoint
                mapView.CenterMapOnLocation(newCheckpoint.Latitude, newCheckpoint.Longitude, 0.5);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the map with the provided checkpoints.
        /// </summary>
        /// <param name="checkpoints">The collection of checkpoints to display on the map.</param>
        public void UpdateCheckpoints(IEnumerable<CheckpointModel> checkpoints)
        {
            if (mapControl == null)
                return;
                
            // Clear existing pins and dictionary
            mapControl.Pins.Clear();
            _checkpointPins.Clear();
            
            if (checkpoints == null || !checkpoints.Any())
                return;
                
            // Add pins for each checkpoint
            foreach (var checkpoint in checkpoints)
            {
                var pin = CreatePin(checkpoint);
                mapControl.Pins.Add(pin);
                _checkpointPins[checkpoint.Id] = pin;
            }
            
            // Calculate and move to a region that shows all checkpoints
            var region = CalculateMapRegion(checkpoints);
            mapControl.MoveToRegion(region);
        }

        /// <summary>
        /// Updates the visual status of a checkpoint on the map.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to update.</param>
        /// <param name="isVerified">Whether the checkpoint is verified.</param>
        public void UpdateCheckpointStatus(int checkpointId, bool isVerified)
        {
            if (_checkpointPins.TryGetValue(checkpointId, out Pin pin))
            {
                if (isVerified)
                {
                    // Update to verified state
                    pin.Type = PinType.SavedPin;
                    
                    // Make sure we don't duplicate the verification mark
                    if (!pin.Label.StartsWith("✓"))
                    {
                        pin.Label = $"✓ {pin.Label}";
                    }
                }
                else
                {
                    // Update to unverified state
                    pin.Type = PinType.Place;
                    pin.Label = pin.Label.Replace("✓ ", "");
                }
            }
        }

        /// <summary>
        /// Centers the map on the specified coordinates with the given radius.
        /// </summary>
        /// <param name="latitude">The latitude coordinate.</param>
        /// <param name="longitude">The longitude coordinate.</param>
        /// <param name="radius">The radius in kilometers to show around the location.</param>
        public void CenterMapOnLocation(double latitude, double longitude, double radius)
        {
            if (mapControl == null)
                return;
                
            var position = new Location(latitude, longitude);
            
            // Calculate appropriate span based on the radius
            // Approximately 111km per degree of latitude
            double latitudeDegrees = radius / 111.0;
            
            // Longitude degrees per km varies with latitude
            double longitudeDegrees = radius / (111.0 * Math.Cos(latitude * Math.PI / 180.0));
            
            var mapSpan = new MapSpan(position, latitudeDegrees, longitudeDegrees);
            mapControl.MoveToRegion(mapSpan);
        }

        /// <summary>
        /// Highlights or un-highlights a specific checkpoint on the map.
        /// </summary>
        /// <param name="checkpointId">The ID of the checkpoint to highlight.</param>
        /// <param name="highlight">Whether to highlight the checkpoint.</param>
        public void HighlightCheckpoint(int checkpointId, bool highlight)
        {
            if (_checkpointPins.TryGetValue(checkpointId, out Pin pin))
            {
                if (highlight)
                {
                    // Highlight the pin - change appearance to stand out
                    pin.ZIndex = 1; // Bring to front
                    pin.Type = PinType.SearchResult; // Different visual appearance
                }
                else
                {
                    // Reset to normal appearance
                    pin.ZIndex = 0;
                    
                    // Determine if this checkpoint is verified by checking the label
                    bool isVerified = pin.Label.StartsWith("✓");
                    pin.Type = isVerified ? PinType.SavedPin : PinType.Place;
                }
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Calculates a map region that encompasses all checkpoints.
        /// </summary>
        /// <param name="checkpoints">The checkpoints to include in the region.</param>
        /// <returns>A MapSpan that encompasses all checkpoints.</returns>
        private MapSpan CalculateMapRegion(IEnumerable<CheckpointModel> checkpoints)
        {
            if (checkpoints == null || !checkpoints.Any())
            {
                // Default map span if no checkpoints
                return new MapSpan(new Location(0, 0), 0.1, 0.1);
            }
            
            // Find min/max coordinates to determine the bounding box
            double minLatitude = checkpoints.Min(c => c.Latitude);
            double maxLatitude = checkpoints.Max(c => c.Latitude);
            double minLongitude = checkpoints.Min(c => c.Longitude);
            double maxLongitude = checkpoints.Max(c => c.Longitude);
            
            // Calculate center point
            double centerLatitude = (minLatitude + maxLatitude) / 2;
            double centerLongitude = (minLongitude + maxLongitude) / 2;
            
            // Calculate span with padding (20% larger than the actual area)
            double latitudeSpan = (maxLatitude - minLatitude) * 1.2;
            double longitudeSpan = (maxLongitude - minLongitude) * 1.2;
            
            // Ensure minimum span for visibility
            latitudeSpan = Math.Max(latitudeSpan, 0.02);
            longitudeSpan = Math.Max(longitudeSpan, 0.02);
            
            return new MapSpan(new Location(centerLatitude, centerLongitude), latitudeSpan, longitudeSpan);
        }

        /// <summary>
        /// Creates a map pin for a checkpoint with appropriate styling.
        /// </summary>
        /// <param name="checkpoint">The checkpoint to create a pin for.</param>
        /// <returns>A configured Pin object for the checkpoint.</returns>
        private Pin CreatePin(CheckpointModel checkpoint)
        {
            var pin = new Pin
            {
                Type = checkpoint.IsVerified ? PinType.SavedPin : PinType.Place,
                Position = new Location(checkpoint.Latitude, checkpoint.Longitude),
                Label = checkpoint.IsVerified ? $"✓ {checkpoint.Name}" : checkpoint.Name,
                Address = $"Checkpoint {checkpoint.Id}"
            };
            
            return pin;
        }

        /// <summary>
        /// Handles the pin clicked event on the map.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMapPinClicked(object sender, PinClickedEventArgs e)
        {
            // Find the checkpoint associated with the clicked pin
            var pin = e.Pin;
            var checkpoint = Checkpoints?.FirstOrDefault(c => 
                Math.Abs(c.Latitude - pin.Position.Latitude) < 0.0001 && 
                Math.Abs(c.Longitude - pin.Position.Longitude) < 0.0001);
            
            if (checkpoint != null)
            {
                // Update the selected checkpoint
                SelectedCheckpoint = checkpoint;
                
                // Prevent the default info window from showing
                e.HideInfoWindow = true;
            }
        }

        #endregion
    }
}