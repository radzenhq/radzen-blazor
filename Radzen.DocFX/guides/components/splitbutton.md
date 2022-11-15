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

## Disabled states
You can disable the whole RadzenSplitButton or only some items of it.

```
<RadzenSplitButton Disabled="true" Text="SplitButton">
    <ChildContent>
        <RadzenSplitButtonItem Text="Item1" Value="1" />
        <RadzenSplitButtonItem Text="Item2" Value="2" />
    </ChildContent>
</RadzenSplitButton>
```

```
<RadzenSplitButton Text="SplitButton">
    <ChildContent>
        <RadzenSplitButtonItem Text="Item1" Value="1" />
        <RadzenSplitButtonItem Text="Disabled Item2" Value="2"  Disabled="true" />
    </ChildContent>
</RadzenSplitButton>
```

## Always open popup with items
Sometimes you wish do not have default click handler on the main button rather show all available items.

```
<RadzenSplitButton AlwaysOpenPopup=true Text="SplitButton">
    <ChildContent>
        <RadzenSplitButtonItem Text="Item1" Value="1" />
        <RadzenSplitButtonItem Text="Item2" Value="2" />
    </ChildContent>
</RadzenSplitButton>
```