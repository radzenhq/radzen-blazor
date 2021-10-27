# SplitButton component
This article demonstrates how to use the SplitButton component. 

```
<RadzenSplitButton Click=@OnClick Text="SplitButton" Icon="account_circle">
    <ChildContent>
        <RadzenSplitButtonItem Text="Item1" Value="1" Icon="account_box" />
        <RadzenSplitButtonItem Text="Item2" Value="2" Icon="account_balance_wallet" />
    </ChildContent>
</RadzenSplitButton>

@code {
    void OnClick(RadzenSplitButtonItem item)
    {
        if(item != null)
        {
            Console.WriteLine($"Item with value {item.Value} clicked");
        }
        else
        {
            Console.WriteLine($"Button clicked");
        }
    }
}
```