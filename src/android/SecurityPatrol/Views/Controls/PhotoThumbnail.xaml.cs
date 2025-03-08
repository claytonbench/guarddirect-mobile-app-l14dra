using Microsoft.Maui.Controls;
using System;
using System.Windows.Input;
using SecurityPatrol.Models;
using SecurityPatrol.Converters;

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// A custom control that displays a photo thumbnail with synchronization status indicator.
    /// The control supports binding to a PhotoModel and provides a tap command for interaction.
    /// </summary>
    public partial class PhotoThumbnail : ContentView
    {
        /// <summary>
        /// Bindable property for the PhotoModel to display in this thumbnail
        /// </summary>
        public static readonly BindableProperty PhotoProperty = BindableProperty.Create(
            propertyName: nameof(Photo),
            returnType: typeof(PhotoModel),
            declaringType: typeof(PhotoThumbnail),
            defaultValue: null,
            propertyChanged: OnPhotoPropertyChanged);

        /// <summary>
        /// Bindable property for the command to execute when the thumbnail is tapped
        /// </summary>
        public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
            propertyName: nameof(TapCommand),
            returnType: typeof(ICommand),
            declaringType: typeof(PhotoThumbnail),
            defaultValue: null,
            propertyChanged: OnTapCommandPropertyChanged);

        /// <summary>
        /// Gets or sets the photo model to display in this thumbnail
        /// </summary>
        public PhotoModel Photo
        {
            get => (PhotoModel)GetValue(PhotoProperty);
            set => SetValue(PhotoProperty, value);
        }

        /// <summary>
        /// Gets or sets the command to execute when the thumbnail is tapped
        /// </summary>
        public ICommand TapCommand
        {
            get => (ICommand)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the PhotoThumbnail control
        /// </summary>
        public PhotoThumbnail()
        {
            InitializeComponent();
            BindingContext = this;
        }

        /// <summary>
        /// Called when the Photo property changes to update the control's state
        /// </summary>
        private static void OnPhotoPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PhotoThumbnail)bindable;
            control.BindingContext = control;
        }

        /// <summary>
        /// Called when the TapCommand property changes to update the control's command binding
        /// </summary>
        private static void OnTapCommandPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PhotoThumbnail)bindable;
            // The command binding will be handled in XAML through the TapGestureRecognizer
        }
    }
}