// MauiProgram.cs
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using InventoryPredictor.MauiBlazor.Services;

namespace InventoryPredictor.MauiBlazor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add Blazor Web View
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
#if DEBUG
        builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
#endif

        // Register HttpClient with base address
        builder.Services.AddHttpClient("InventoryAPI", client =>
        {
            var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://api.inventorypredictor.com/api/v1";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Services
        builder.Services.AddScoped(sp => 
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("InventoryAPI"));
        
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddScoped<IPredictionService, PredictionService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<SignalRService>();
        builder.Services.AddSingleton<LocalStorageService>();
        builder.Services.AddScoped<IDataAgentService, DataAgentService>();

        // Platform-specific services
#if ANDROID
        builder.Services.AddSingleton<INotificationService, Android.NotificationService>();
        builder.Services.AddSingleton<IFileService, Android.FileService>();
#elif IOS
        builder.Services.AddSingleton<INotificationService, iOS.NotificationService>();
        builder.Services.AddSingleton<IFileService, iOS.FileService>();
#elif WINDOWS
        builder.Services.AddSingleton<INotificationService, Windows.NotificationService>();
        builder.Services.AddSingleton<IFileService, Windows.FileService>();
#endif

        // Configure lifecycle events
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnCreate((activity, bundle) => 
                {
                    // Initialize Android-specific services
                }));
#elif IOS
            events.AddiOS(ios => ios
                .FinishedLaunching((app, launchOptions) =>
                {
                    // Initialize iOS-specific services
                    return true;
                }));
#endif
        });

        return builder.Build();
    }
}

// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "https://api.inventorypredictor.com/api/v1",
    "SignalRHub": "https://api.inventorypredictor.com/hubs/inventory",
    "Timeout": 30
  },
  "Fabric": {
    "EventhouseEndpoint": "https://your-workspace.kusto.fabric.microsoft.com",
    "Database": "inventory_realtime_db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "App": {
    "AppName": "Kenya Inventory Predictor AI",
    "Version": "1.0.0",
    "SupportEmail": "support@inventorypredictor.com"
  },
  "Features": {
    "OfflineMode": true,
    "PushNotifications": true,
    "DataSync": true
  }
}

// appsettings.Development.json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/api/v1",
    "SignalRHub": "https://localhost:7001/hubs/inventory"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug"
    }
  }
}

// App.xaml.cs
namespace InventoryPredictor.MauiBlazor;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Set main page
        MainPage = new MainPage();
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);

        // Set window properties
        window.Title = "Kenya Inventory Predictor AI";

#if WINDOWS
        // Set minimum window size for Windows
        window.MinimumHeight = 600;
        window.MinimumWidth = 800;
#endif

        return window;
    }
}

// MainPage.xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:InventoryPredictor.MauiBlazor"
             x:Class="InventoryPredictor.MauiBlazor.MainPage"
             BackgroundColor="{DynamicResource PageBackgroundColor}">

    <BlazorWebView x:Name="blazorWebView" HostPage="wwwroot/index.html">
        <BlazorWebView.RootComponents>
            <RootComponent Selector="#app" ComponentType="{x:Type local:Components.Routes}" />
        </BlazorWebView.RootComponents>
    </BlazorWebView>

</ContentPage>

// MainPage.xaml.cs
namespace InventoryPredictor.MauiBlazor;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }
}

// wwwroot/index.html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover" />
    <title>Kenya Inventory Predictor AI</title>
    <base href="/" />
    <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="stylesheet" href="InventoryPredictor.MauiBlazor.styles.css" />
</head>

<body>
    <div class="status-bar-safe-area"></div>

    <div id="app">
        <div class="loading-screen">
            <div class="spinner"></div>
            <h2>Loading Kenya Inventory Predictor AI...</h2>
            <p>Powered by Microsoft Fabric & AI</p>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="_framework/blazor.webview.js" autostart="false"></script>
    <script src="js/app.js"></script>
</body>
</html>

// wwwroot/css/app.css
:root {
    --primary-color: #007bff;
    --secondary-color: #6c757d;
    --success-color: #28a745;
    --danger-color: #dc3545;
    --warning-color: #ffc107;
    --info-color: #17a2b8;
    --background-color: #f8f9fa;
    --text-color: #333;
    --border-color: #dee2e6;
}

* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

html, body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    height: 100%;
    background-color: var(--background-color);
    color: var(--text-color);
}

#app {
    height: 100%;
}

/* Loading Screen */
.loading-screen {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100vh;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
}

.spinner {
    width: 50px;
    height: 50px;
    border: 5px solid rgba(255, 255, 255, 0.3);
    border-top-color: white;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 1.5rem;
}

@keyframes spin {
    to { transform: rotate(360deg); }
}

.loading-screen h2 {
    font-size: 1.5rem;
    margin-bottom: 0.5rem;
}

.loading-screen p {
    opacity: 0.9;
    font-size: 0.9rem;
}

/* Status bar safe area for mobile */
.status-bar-safe-area {
    height: env(safe-area-inset-top);
    background-color: var(--primary-color);
}

/* Error UI */
#blazor-error-ui {
    background: lightyellow;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
    display: none;
    left: 0;
    padding: 0.6rem 1.25rem 0.7rem 1.25rem;
    position: fixed;
    width: 100%;
    z-index: 1000;
}

#blazor-error-ui .dismiss {
    cursor: pointer;
    position: absolute;
    right: 0.75rem;
    top: 0.5rem;
}

/* Button Styles */
.btn-primary {
    background-color: var(--primary-color);
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 8px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
}

.btn-primary:hover {
    background-color: #0056b3;
    transform: translateY(-1px);
    box-shadow: 0 4px 8px rgba(0, 123, 255, 0.3);
}

.btn-secondary {
    background-color: var(--secondary-color);
    color: white;
    border: none;
    padding: 0.5rem 1rem;
    border-radius: 8px;
    font-weight: 500;
    cursor: pointer;
    transition: all 0.2s;
}

.btn-sm {
    padding: 0.25rem 0.75rem;
    font-size: 0.875rem;
}

/* Card Styles */
.card {
    background: white;
    border-radius: 12px;
    padding: 1.5rem;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    margin-bottom: 1rem;
}

/* Responsive Design */
@media (max-width: 768px) {
    .card {
        padding: 1rem;
    }
}

/* iOS specific styles */
@supports (-webkit-touch-callout: none) {
    .status-bar-safe-area {
        padding-top: constant(safe-area-inset-top);
        padding-top: env(safe-area-inset-top);
    }
}

// wwwroot/js/app.js
window.addEventListener('load', function () {
    // Custom JavaScript initialization
    console.log('Kenya Inventory Predictor AI - Loaded');

    // Handle offline/online status
    window.addEventListener('online', () => {
        console.log('App is online');
        // Trigger sync
    });

    window.addEventListener('offline', () => {
        console.log('App is offline');
        // Show offline indicator
    });
});

// Utility functions for Blazor interop
window.downloadFile = (filename, content) => {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/plain;charset=utf-8,' + encodeURIComponent(content));
    element.setAttribute('download', filename);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
};

window.copyToClipboard = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy:', err);
        return false;
    }
};

window.shareData = async (title, text, url) => {
    if (navigator.share) {
        try {
            await navigator.share({ title, text, url });
            return true;
        } catch (err) {
            console.error('Error sharing:', err);
            return false;
        }
    }
    return false;
};

// _Imports.razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using InventoryPredictor.MauiBlazor
@using InventoryPredictor.MauiBlazor.Components
@using InventoryPredictor.MauiBlazor.Components.Shared
@using InventoryPredictor.MauiBlazor.Services
@using InventoryPredictor.Shared.Models
