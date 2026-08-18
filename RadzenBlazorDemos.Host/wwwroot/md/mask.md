# Mask

The Blazor Masked TextBox formats input as the user types using a pattern - phone numbers, dates, IP addresses, and more.

Keywords: input, form, edit, mask

> API reference: [RadzenMask API](https://blazor.radzen.com/api/mask.md)

## Examples

## Blazor Mask

The Blazor Masked TextBox formats input as the user types using a pattern - phone numbers, dates, IP addresses, and more.

```razor
<RadzenTemplateForm TItem="MyObject" Data=@obj Submit=@OnSubmit>
    <RadzenCard class="rz-my-12 rz-mx-auto" Style="max-width: 400px;">
        <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3">Telephone</RadzenText>
        <RadzenMask Mask="(***) ***-****" CharacterPattern="[0-9]" Placeholder="(000) 000-0000" Name="Phone" @bind-Value=@obj.Phone Change=@(args => OnChange(args, "Telephone")) 
            Style="width: 100%;" aria-label="Phone" />
        <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3" class="rz-mt-6">Credit Card</RadzenText>
        <RadzenMask Mask="**** **** **** ****" CharacterPattern="[0-9]" Placeholder="0000 0000 0000 0000" Name="CardNr" @bind-Value=@obj.CardNr
            Change=@(args => OnChange(args, "Credit Card")) Style="width: 100%;" aria-label="Credit Card" />
        <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3" class="rz-mt-6">SSN</RadzenText>
        <RadzenMask Mask="***-**-****" CharacterPattern="[0-9]" Placeholder="000-00-0000" Name="SSN" @bind-Value=@obj.SSN Change=@(args => OnChange(args, "SSN")) 
            Style="width: 100%;" aria-label="SSN"/>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" class="rz-mt-6">
            <RadzenButton ButtonType="ButtonType.Submit" Icon="save" Text="Save" />
            <RadzenButton ButtonStyle="ButtonStyle.Light" Icon="cancel" Text="Cancel" />
        </RadzenStack>
    </RadzenCard>
</RadzenTemplateForm>

<EventConsole @ref=@console />

@code { 
    public class MyObject
    {
        public string Phone { get; set; }
        public string CardNr { get; set; }
        public string SSN { get; set; }
    }

    MyObject obj = new MyObject();

    EventConsole console;

    void OnChange(string value, string name)
    {
        console.Log($"{name} value changed to {value}");
    }

    void OnSubmit(MyObject arg)
    {
        console.Log($"Form submitted with values {JsonSerializer.Serialize(arg)}");
    }
}
```


### Mask Sizes

Use the `InputSize` property to set the Mask size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenMask Mask="(***) ***-****" Placeholder="(000) 000-0000" InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenMask Mask="(***) ***-****" Placeholder="(000) 000-0000" InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenMask Mask="(***) ***-****" Placeholder="(000) 000-0000" InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenMask Mask="(***) ***-****" Placeholder="(000) 000-0000" InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>
```
