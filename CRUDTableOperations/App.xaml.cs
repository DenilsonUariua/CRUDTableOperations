﻿using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

using CRUDTableOperations.Contracts.Services;
using CRUDTableOperations.Contracts.Views;
using CRUDTableOperations.Core.Contracts.Services;
using CRUDTableOperations.Core.Services;
using CRUDTableOperations.Models;
using CRUDTableOperations.Services;
using CRUDTableOperations.ViewModels;
using CRUDTableOperations.Views;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRUDTableOperations;

// For more information about application lifecycle events see https://docs.microsoft.com/dotnet/framework/wpf/app-development/application-management-overview

// WPF UI elements use language en-US by default.
// If you need to support other cultures make sure you add converters and review dates and numbers in your UI to ensure everything adapts correctly.
// Tracking issue for improving this is https://github.com/dotnet/wpf/issues/1946
public partial class App : Application
{
    private IHost _host;

    public T GetService<T>()
        where T : class
        => _host.Services.GetService(typeof(T)) as T;

    public App()
    {
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var appLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        // For more information about .NET generic host see  https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.0
        _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureAppConfiguration(c =>
                {
                    c.SetBasePath(appLocation);
                })
                .ConfigureServices(ConfigureServices)
                .Build();

        await _host.StartAsync();
    }

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // TODO: Register your services, viewmodels and pages here

        // App Host
        services.AddHostedService<ApplicationHostService>();

        // Activation Handlers

        // Core Services

        // Services
        services.AddSingleton<IWindowManagerService, WindowManagerService>();
        services.AddSingleton<ISampleDataService, SampleDataService>();
        services.AddSingleton<IPageService, PageService>();
        services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<SQLDataService>();
        services.AddSingleton<EmployeeSQLDataService>();

		// Views and ViewModels
		services.AddTransient<IShellWindow, ShellWindow>();
        services.AddTransient<ShellViewModel>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainPage>();

        services.AddTransient<DataGridViewModel>();
        services.AddTransient<DataGridPage>();

        services.AddTransient<DataGrid1ViewModel>();
        services.AddTransient<DataGrid1Page>();

        services.AddTransient<IShellDialogWindow, ShellDialogWindow>();
        services.AddTransient<ShellDialogViewModel>();

        // Configuration
        services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
        services.AddSingleton<IRightPaneService, RightPaneService>();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // TODO: Please log and handle the exception as appropriate to your scenario
        // For more info see https://docs.microsoft.com/dotnet/api/system.windows.application.dispatcherunhandledexception?view=netcore-3.0
    }
}
