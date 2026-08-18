# Timeline

Blazor Timeline component for displaying a chronological sequence of events with flexible orientation and styling.

Keywords: timeline, time, line

> API reference: [RadzenTimeline API](https://blazor.radzen.com/api/timeline.md)

## Examples

## Blazor Timeline

A graphical representation for displaying a chronological sequence of events or milestones.

### Basic Usage

Display events chronologically in a vertical timeline with customizable points and content.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointStyle="PointStyle.Primary">
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">NOV 2022</RadzenText>
            </LabelContent>
            <ChildContent>
                Celebrating the official release of Radzen Blazor Studio.
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem>
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">JAN 2021</RadzenText>
            </LabelContent>
            <ChildContent>
                Radzen Blazor components open sourced under the MIT license.
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem>
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">JUN 2018</RadzenText>
            </LabelContent>
            <ChildContent>
                Radzen 2.0 is a fact.
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem>
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">APR 2017</RadzenText>
            </LabelContent>
            <ChildContent>
                Radzen 1.0 is out the door - automatic page generation and MS SQL support.
            </ChildContent>
        </RadzenTimelineItem>
    </Items>
</RadzenTimeline>
```


### Orientation and Position

The `Orientation` sets the timeline's alignment to horizontal or vertical. Use `LinePosition` in combination with `Reverse` to specify the position of `&lt;LabelContent&gt;` and `&lt;ChildContent&gt;` content with respect to the line.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Gap="1rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenStack Gap="1rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Orientation:" Style="width: 6rem;" Component="orientation" />
            <RadzenSelectBar @bind-Value="@orientation" Name="orientation" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Orientation)).Cast<Orientation>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
            <RadzenDropDown @bind-Value="@orientation" Name="orientation" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Orientation)).Cast<Orientation>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
        </RadzenStack>
        <RadzenStack Gap="1rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="LinePosition:" Style="width: 6rem;" Component="position" />
            <RadzenSelectBar @bind-Value="@position" Name="position" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(LinePosition)).Cast<LinePosition>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
            <RadzenDropDown @bind-Value="@position" Name="position" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(LinePosition)).Cast<LinePosition>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
        </RadzenStack>
        <RadzenStack Gap="1rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Reverse:" Style="width: 6rem;" Component="Reverse" />
            <RadzenSwitch @bind-Value="@reverse" Style="margin-top: 4px;" Name="Reverse" />
        </RadzenStack>
    </RadzenStack>
    <RadzenTimeline Orientation="@orientation" LinePosition="@position" Reverse="@reverse" class="rz-m-4">
        <Items>
            <RadzenTimelineItem PointStyle="PointStyle.Primary">
                <LabelContent>
                    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0 rz-color-primary">NOV 2022</RadzenText>
                </LabelContent>
                <ChildContent>
                    Celebrating the official release of Radzen Blazor Studio.
                </ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem>
                <LabelContent>
                    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0 rz-color-primary">JAN 2021</RadzenText>
                </LabelContent>
                <ChildContent>
                    Radzen Blazor components open sourced under the MIT license.
                </ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem>
                <LabelContent>
                    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0 rz-color-primary">JUN 2018</RadzenText>
                </LabelContent>
                <ChildContent>
                    Radzen 2.0 is a fact.
                </ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem>
                <LabelContent>
                    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0 rz-color-primary">APR 2017</RadzenText>
                </LabelContent>
                <ChildContent>
                    Radzen 1.0 is out the door - automatic page generation and MS SQL support.
                </ChildContent>
            </RadzenTimelineItem>
        </Items>
    </RadzenTimeline>
</div>

@code {
    Orientation orientation = Orientation.Vertical;
    LinePosition position = LinePosition.Center;
    bool reverse;
}
```


### Align Items

Set the `AlignItems` property to `&lt;RadzenTimeLine&gt;` to specify the alignment of Timeline items' content, namely `&lt;PointContent&gt;`, `&lt;LabelContent&gt;` and `&lt;ChildContent&gt;` content.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack  Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="AlignItems:" Component="AlignItems" />
        <RadzenSelectBar @bind-Value="@alignItems" TextProperty="Text" ValueProperty="Value" Name="AlignItems"
                    Data="@(Enum.GetValues(typeof(AlignItems)).Cast<AlignItems>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@alignItems" TextProperty="Text" ValueProperty="Value" Name="AlignItems"
                    Data="@(Enum.GetValues(typeof(AlignItems)).Cast<AlignItems>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>
    <RadzenTimeline AlignItems="@alignItems" LinePosition="LinePosition.Alternate" style="max-width: 600px; margin: 0 auto;">
        <Items>
            <RadzenTimelineItem Size="PointSize.Medium" PointStyle="PointStyle.Warning">
                <LabelContent><RadzenBadge BadgeStyle="BadgeStyle.Warning" IsPill="true" Text="6th century BC" /></LabelContent>
                <ChildContent><RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0">Persian soldiers baked flatbreads with cheese and dates</RadzenText></ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem Size="PointSize.Medium" PointStyle="PointStyle.Danger">
                <LabelContent><RadzenBadge BadgeStyle="BadgeStyle.Danger" IsPill="true" Text="19 BC" /></LabelContent>
                <ChildContent><RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0">An early reference to a pizza-like food</RadzenText></ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem Size="PointSize.Medium" PointStyle="PointStyle.Warning">
                <LabelContent><RadzenBadge BadgeStyle="BadgeStyle.Warning" IsPill="true" Text="997 AD" /></LabelContent>
                <ChildContent><RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0">The word pizza was first documented</RadzenText></ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem Size="PointSize.Medium" PointStyle="PointStyle.Danger">
                <LabelContent><RadzenBadge BadgeStyle="BadgeStyle.Danger" IsPill="true" Text="16th century" /></LabelContent>
                <ChildContent><RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0">A galette flatbread was referred to as a pizza in Naples</RadzenText></ChildContent>
            </RadzenTimelineItem>
            <RadzenTimelineItem Size="PointSize.Medium" PointStyle="PointStyle.Warning">
                <LabelContent><RadzenBadge BadgeStyle="BadgeStyle.Warning" IsPill="true" Text="1843" /></LabelContent>
                <ChildContent><RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0">Alezandre Dumans described the diversity of pizza toppings</RadzenText></ChildContent>
            </RadzenTimelineItem>
        </Items>
    </RadzenTimeline>
</div>

@code {
    AlignItems alignItems = AlignItems.Center;
}
```


### Styling

Use CSS variables to adjust line width and color.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Gap="1rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenStack Gap="1rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Orientation:" Style="width: 6rem;" Component="TimelineStylingOrientation" />
            <RadzenSelectBar @bind-Value="@orientation" Name="TimelineStylingOrientation" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Orientation)).Cast<Orientation>().Select(t => new { Text = $"{t}", Value = t }))" PointSize="ButtonPointSize.Small" class="rz-display-none rz-display-xl-flex" />
            <RadzenDropDown @bind-Value="@orientation" Name="TimelineStylingOrientation" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Orientation)).Cast<Orientation>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
        </RadzenStack>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" FlexWrap="FlexWrap.Wrap" Gap="1rem" class="rz-p-4 rz-mb-6">
        <RadzenTimeline Orientation="@orientation" LinePosition="LinePosition.Start"
            style="--rz-timeline-line-width: 36px;
                   max-width: 600px;
                   margin: 0 auto;">
            <Items>
                <RadzenTimelineItem PointVariant="Variant.Flat" PointSize="PointSize.Small" PointStyle="PointStyle.Info">
                    <PointContent><RadzenIcon Icon="check" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 1</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Register Your Account</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Flat" PointSize="PointSize.Small" PointStyle="PointStyle.Info">
                    <PointContent><RadzenIcon Icon="check" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 2</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Verify Your Identity</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Text" PointStyle="PointStyle.Info">
                    <PointContent><RadzenIcon Icon="more_horiz" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 3</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Complete Self-Certification</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Text" PointSize="PointSize.Small">
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 4</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Complete Your Profile</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
            </Items>
        </RadzenTimeline>

        <RadzenTimeline Orientation="@orientation" LinePosition="LinePosition.Start"
            style="--rz-timeline-line-width: 3px;
                   --rz-timeline-line-color: var(--rz-info);
                   --rz-timeline-axis-size: 72px;
                   max-width: 600px;
                   margin: 0 auto;">
            <Items>
                <RadzenTimelineItem PointVariant="Variant.Outlined" PointStyle="PointStyle.Info" PointShadow="0">
                    <PointContent><RadzenIcon Icon="how_to_reg" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 1</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Register Your Account</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Outlined" PointStyle="PointStyle.Info" PointShadow="0">
                    <PointContent><RadzenIcon Icon="fingerprint" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 2</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Verify Your Identity</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Outlined" PointSize="PointSize.Large" PointStyle="PointStyle.Info" PointShadow="0">
                    <PointContent><RadzenIcon Icon="workspace_premium" /></PointContent>
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0 rz-color-info">Step 3</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0 rz-color-info">Complete Self-Certification</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
                <RadzenTimelineItem PointVariant="Variant.Outlined" PointSize="PointSize.Small" PointStyle="PointStyle.Info" PointShadow="0">
                    <ChildContent>
                        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-m-0">Step 4</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-m-0">Complete Your Profile</RadzenText>
                    </ChildContent>
                </RadzenTimelineItem>
            </Items>
        </RadzenTimeline>
    </RadzenStack>
</div>

@code {
    Orientation orientation = Orientation.Vertical;
}
```


### Point Size

Set the `PointSize` property to `&lt;RadzenTimeLineItem&gt;` to specify the item's point size.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointSize="PointSize.ExtraSmall">
            <ChildContent>
                <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="ExtraSmall" />
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointSize="PointSize.Small">
            <ChildContent>
                <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="Small" />
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointSize="PointSize.Medium">
            <ChildContent>
                <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="Medium" />
            </ChildContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointSize="PointSize.Large">
            <ChildContent>
                <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="Large" />
            </ChildContent>
        </RadzenTimelineItem>
    </Items>
</RadzenTimeline>
```


### Point Style

Set the `PointStyle` property to `&lt;RadzenTimeLineItem&gt;` to change the item's point style.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" Text="Primary" />
        <RadzenTimelineItem PointStyle="PointStyle.Secondary" Text="Secondary" />
        <RadzenTimelineItem PointStyle="PointStyle.Info" Text="Info" />
        <RadzenTimelineItem PointStyle="PointStyle.Warning" Text="Warning" />
        <RadzenTimelineItem PointStyle="PointStyle.Danger" Text="Danger" />
        <RadzenTimelineItem PointStyle="PointStyle.Success" Text="Success" />
        <RadzenTimelineItem PointStyle="PointStyle.Light" Text="Light" />
        <RadzenTimelineItem PointStyle="PointStyle.Base" Text="Base" />
        <RadzenTimelineItem PointStyle="PointStyle.Dark" Text="Dark" />
    </Items>
</RadzenTimeline>
```


### Point Variant

Set the `PointVariant` property to `&lt;RadzenTimeLineItem&gt;` to change the item's point variant.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointSize="PointSize.Large">
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">Filled</RadzenText>
            </LabelContent>
            <PointContent>
                A
            </PointContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointSize="PointSize.Large" PointVariant="Variant.Flat">
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">Flat</RadzenText>
            </LabelContent>
            <PointContent>
                B
            </PointContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointSize="PointSize.Large" PointVariant="Variant.Outlined">
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">Outlined</RadzenText>
            </LabelContent>
            <PointContent>
                C
            </PointContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointSize="PointSize.Large" PointVariant="Variant.Text">
            <LabelContent>
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-m-0">Text</RadzenText>
            </LabelContent>
            <PointContent>
                D
            </PointContent>
        </RadzenTimelineItem>
    </Items>
</RadzenTimeline>
```


### Point Shadow

Set the `PointShadow` property to `&lt;RadzenTimeLineItem&gt;` to specify the size of the item's point shadow.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="0">PointShadow 0</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="1">PointShadow 1</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="2">PointShadow 2</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="3">PointShadow 3</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="4">PointShadow 4</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="5">PointShadow 5</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="6">PointShadow 6</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="7">PointShadow 7</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="8">PointShadow 8</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="9">PointShadow 9</RadzenTimelineItem>
        <RadzenTimelineItem PointStyle="PointStyle.Primary" PointShadow="10">PointShadow 10</RadzenTimelineItem>
    </Items>
</RadzenTimeline>
```


### Point Content

The `&lt;PointContent&gt;` can fit in text and imagery.

```razor
<RadzenTimeline>
    <Items>
        <RadzenTimelineItem PointSize="PointSize.Large" PointStyle="PointStyle.Success" PointVariant="Variant.Text" PointShadow="0" Text="Text">
            <PointContent>
                A
            </PointContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointSize="PointSize.Large" PointStyle="PointStyle.Success" PointVariant="Variant.Text" PointShadow="0" Text="RadzenIcon">
            <PointContent>
                <RadzenIcon Icon="alarm_on" />
            </PointContent>
        </RadzenTimelineItem>
        <RadzenTimelineItem PointSize="PointSize.Large" PointVariant="Variant.Text" PointShadow="0" Text="RadzenGravatar">
            <PointContent>
                <RadzenGravatar Email="info@radzen.com" />
            </PointContent>
        </RadzenTimelineItem>
    </Items>
</RadzenTimeline>
```
