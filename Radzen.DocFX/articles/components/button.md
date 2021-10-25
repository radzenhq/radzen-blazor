# Button component
This article demonstrates how to use the Button component.

```
<RadzenButton IsBusy=@busy Click=@OnBusyClick Text="Save" />
@code {
    bool busy;

    async Task OnBusyClick()
    {
        busy = true;
        await Task.Delay(2000);
        busy = false;
    }
}
```