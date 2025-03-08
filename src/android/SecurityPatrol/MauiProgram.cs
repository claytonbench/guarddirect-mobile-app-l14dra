using Microsoft.Extensions.Logging; // Microsoft.Extensions.Logging 8.0.0
using Microsoft.Extensions.DependencyInjection; // Microsoft.Extensions.DependencyInjection 8.0.0
using Microsoft.Maui; // Microsoft.Maui 8.0.0
using Microsoft.Maui.Controls.Hosting; // Microsoft.Maui.Controls.Hosting 8.0.0
using Microsoft.Maui.Controls; // Microsoft.Maui.Controls 8.0.0
using CommunityToolkit.Maui; // CommunityToolkit.Maui Latest
using CommunityToolkit.Mvvm; // CommunityToolkit.Mvvm Latest
using SecurityPatrol.Services; // Internal import
using SecurityPatrol.ViewModels; // Internal import
using SecurityPatrol.Database; // Internal import

namespace SecurityPatrol
{
    /// <summary>
    /// Static class that serves as the entry point for the .NET MAUI application.
    /// Configures the application's services, dependencies, and UI settings.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the .NET MAUI application.
        /// </summary>
        /// <returns>A configured <see cref="MauiApp"/> instance.</returns>
        public static MauiApp CreateMauiApp()
        {
            // Create a MauiAppBuilder instance
            var builder = MauiApp.CreateBuilder();

            // Configure the application with default settings
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Initializes the Community Toolkit for MAUI
                .ConfigureFonts(ConfigureFonts); // Configure fonts

            // Register services in the dependency injection container
            ConfigureServices(builder.Services);

            // Register ViewModels in the dependency injection container
            ConfigureViewModels(builder.Services);

            // Configure logging
#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Build and return the MauiApp instance
            return builder.Build();
        }

        /// <summary>
        /// Configures the dependency injection container with all required services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            // Register singleton services (IAuthenticationStateProvider, ISettingsService, etc.)
            services.AddSingleton<IAuthenticationStateProvider, AuthenticationStateProvider>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
            services.AddSingleton<INetworkService, NetworkService>();
            services.AddSingleton<ITelemetryService, TelemetryService>();

            // Register transient services (INavigationService, IApiService, etc.)
            services.AddTransient<INavigationService, NavigationService>();
            services.AddTransient<IApiService, ApiService>();

            // Register scoped services (IAuthenticationService, ITimeTrackingService, etc.)
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ITimeTrackingService, TimeTrackingService>();
            services.AddScoped<ILocationService, LocationService>();
            services.AddScoped<IPhotoService, PhotoService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IPatrolService, PatrolService>();
            services.AddScoped<ISyncService, SyncService>();
        }

        /// <summary>
        /// Configures the fonts used in the application.
        /// </summary>
        /// <param name="fonts">The <see cref="IFontCollection"/> to configure.</param>
        private static void ConfigureFonts(IFontCollection fonts)
        {
            // Add OpenSans-Regular.ttf font
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");

            // Add OpenSans-SemiBold.ttf font
            fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBold");

            // Add FontAwesome.ttf font for icons
            fonts.AddFont("FontAwesome.ttf", "FontAwesome");
        }

        /// <summary>
        /// Configures the dependency injection container with all ViewModels.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        private static void ConfigureViewModels(IServiceCollection services)
        {
            // Register AuthenticationViewModel
            services.AddTransient<AuthenticationViewModel>();

            // Register PhoneEntryViewModel
            services.AddTransient<PhoneEntryViewModel>();

            // Register TimeTrackingViewModel
            services.AddTransient<TimeTrackingViewModel>();

            // Register PatrolViewModel
            services.AddTransient<PatrolViewModel>();

            // Register PhotoCaptureViewModel
            services.AddTransient<PhotoCaptureViewModel>();

            // Register ActivityReportViewModel
            services.AddTransient<ActivityReportViewModel>();

            // Register MainViewModel
            services.AddTransient<MainViewModel>();

            // Register other ViewModels as needed
        }
    }
}