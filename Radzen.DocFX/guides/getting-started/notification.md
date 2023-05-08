# Notification setup
This article shows the additional setup steps required to use the RadzenNotification component.

1. [Register](#service-registration) the service.
1. [Add](#add-to-layout) the component to your layout.

## Service registration
The RadzenNotification is used via the [NotificationService](xref:Radzen.NotificationService) class which must be registered as a service.

# [Blazor Server before .NET 6](#tab/server-side)
1. Open `Startup.cs`
1. Import the Radzen namespace
   ```
   using Radzen;
   ```
1. Register the `NotificationService` in the `ConfigureServices` method.
   ```
   public void ConfigureServices(IServiceCollection services)
   {
       // Other registrations
       services.AddScoped<NotificationService>();
       // Other registrations
   }
   ```
# [Blazor WebAssembly or Blazor Server after .NET 6](#tab/client-side)
1. Open `Program.cs`
1. Import the Radzen namespace
   ```
   using Radzen;
   ```
1. Register the `NotificationService` in the `Main` method.
   ```
   public static async Task Main(string[] args)
   {
       // Other registrations
       builder.Services.AddScoped<NotificationService>();
       // Other registrations
   }
   ```
***
## Add to layout
You also need to add the RadzeNotification component to the layout used by your pages (most commonly `MainLayout.razor`).
```
<RadzenNotification />
```
>> Important! Make sure that you do not nest `<RadzenNotification />` inside a positioned element (i.e. with position: relative, position: absolute or position: fixed). To be safe you can add it at the end of the layout file after all other elements.