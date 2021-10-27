# Notification component
This article demonstrates how to use the Notification component. Use `NotificationService` to open and close notifications. 

## Show notification with specific `Severity`: Info, Warning, Danger and Success
```
@inject NotificationService NotificationService

<RadzenButton Text="Show info notification" Style="margin-bottom: 20px; width: 200px"
                    ButtonStyle="ButtonStyle.Info"
                    Click=@(args => ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Info Summary", Detail = "Info Detail", Duration = 4000 })) />

@code {
    void ShowNotification(NotificationMessage message)
    {
        NotificationService.Notify(message);

        Console.WriteLine($"{message.Severity} notification");
    }
}
```

## Show notification with custom position
```
@inject NotificationService NotificationService

<RadzenButton Text="Show notification with custom position" Style="margin-bottom: 20px;"
                    Click=@(args => ShowNotification(new NotificationMessage { Style = "position: absolute; left: -1000px;", Severity = NotificationSeverity.Success, Summary = "Success Summary", Detail = "Success Detail", Duration = 40000 })) />

@code {
    void ShowNotification(NotificationMessage message)
    {
        NotificationService.Notify(message);

        Console.WriteLine($"{message.Severity} notification");
    }
}
```
