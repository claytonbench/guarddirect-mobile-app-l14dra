using Microsoft.Maui.Controls; // Version 8.0.0
using SecurityPatrol.Models;

namespace SecurityPatrol.Views.Controls
{
    /// <summary>
    /// Code-behind class for the TimeRecordItem XAML control that displays a single time record item.
    /// </summary>
    public partial class TimeRecordItem : ContentView
    {
        /// <summary>
        /// Bindable property for the time record model.
        /// </summary>
        public static readonly BindableProperty TimeRecordProperty = BindableProperty.Create(
            propertyName: nameof(TimeRecord),
            returnType: typeof(TimeRecordModel),
            declaringType: typeof(TimeRecordItem),
            defaultValue: null,
            propertyChanged: OnTimeRecordChanged);

        /// <summary>
        /// Gets or sets the time record model to display in this control.
        /// </summary>
        public TimeRecordModel TimeRecord
        {
            get => (TimeRecordModel)GetValue(TimeRecordProperty);
            set => SetValue(TimeRecordProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the TimeRecordItem class.
        /// </summary>
        public TimeRecordItem()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the TimeRecord property changes.
        /// </summary>
        /// <param name="bindable">The bindable object.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnTimeRecordChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (TimeRecordItem)bindable;
            
            if (newValue is TimeRecordModel timeRecord)
            {
                control.BindingContext = timeRecord;
            }
        }
    }
}