# Badge component
This article demonstrates how to use the Badge component. 

## Badge Style
```
<RadzenBadge BadgeStyle="BadgeStyle.Primary" Text="Primary" />
<RadzenBadge BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
<RadzenBadge BadgeStyle="BadgeStyle.Light" Text="Light" />
<RadzenBadge BadgeStyle="BadgeStyle.Success" Text="Success" />
<RadzenBadge BadgeStyle="BadgeStyle.Danger" Text="Danger" />
<RadzenBadge BadgeStyle="BadgeStyle.Warning" Text="Warning" />
<RadzenBadge BadgeStyle="BadgeStyle.Info" Text="Info" />
```

## Pills
```
<RadzenBadge BadgeStyle="BadgeStyle.Primary" IsPill="true" Text="Primary" />
<RadzenBadge BadgeStyle="BadgeStyle.Secondary" IsPill="true" Text="Secondary" />
<RadzenBadge BadgeStyle="BadgeStyle.Light" IsPill="true" Text="Light" />
<RadzenBadge BadgeStyle="BadgeStyle.Success" IsPill="true" Text="Success" />
<RadzenBadge BadgeStyle="BadgeStyle.Danger" IsPill="true" Text="Danger" />
<RadzenBadge BadgeStyle="BadgeStyle.Warning" IsPill="true" Text="Warning" />
<RadzenBadge BadgeStyle="BadgeStyle.Info" IsPill="true" Text="Info" />
```

## In Button
```
<RadzenButton ButtonStyle="ButtonStyle.Info">
    Button
    <RadzenBadge BadgeStyle="BadgeStyle.Primary" Text="15" />
</RadzenButton>

<RadzenButton ButtonStyle="ButtonStyle.Light">
    Button
    <RadzenBadge BadgeStyle="BadgeStyle.Primary" IsPill="@true" Text="15" />
</RadzenButton>
```

## Define custom content
```
<RadzenBadge BadgeStyle="BadgeStyle.Primary">
    Childcontent
</RadzenBadge>
```