using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using Microsoft.Maui.Graphics; // Microsoft.Maui.Graphics 8.0.0

namespace SecurityPatrol.Resources
{
    /// <summary>
    /// Code-behind for AppStyles.xaml resource dictionary.
    /// Implements application-wide styling to ensure:
    /// - Simplicity: Clean, uncluttered interfaces
    /// - Consistency: Uniform patterns and components
    /// - Accessibility: High contrast and clear visual elements
    /// </summary>
    public partial class AppStyles : ResourceDictionary
    {
        /// <summary>
        /// Initializes a new instance of the AppStyles class and loads the XAML content.
        /// </summary>
        public AppStyles()
        {
            InitializeComponent();
            
            // Additional runtime style modifications can be added here if needed
            // This allows for dynamic styling adjustments based on user preferences or device capabilities
        }
    }
}