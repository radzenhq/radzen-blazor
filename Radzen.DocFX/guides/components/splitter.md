# Splitter component
This article demonstrates how to use Splitter.

```
<RadzenSplitter Orientation="Orientation.Vertical" Collapse=@OnCollapse Expand=@OnExpand style="height: 400px; border: 1px solid #777777;">
    <RadzenSplitterPane Size="100px">
        <RadzenSplitter Collapse=@OnCollapse Expand=@OnExpand>
            <RadzenSplitterPane Size="50%" Min="30px" Max="70%">
                Pane A1
                <div style="font-size: 10px;">
                    50% Min 30px Max 70%
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane>
                Pane A2
            </RadzenSplitterPane>
        </RadzenSplitter>
    </RadzenSplitterPane>
    <RadzenSplitterPane Size="100px">
        <RadzenSplitter Collapse=@OnCollapse Expand=@OnExpand Resize=@OnResize>
            <RadzenSplitterPane Size="50px" Min="30px">
                Pane B1
                <div style="font-size: 10px;">
                    Size 50px Min 30px
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane>
                Pane B2
            </RadzenSplitterPane>
            <RadzenSplitterPane Resizable="false">
                Pane B3
                <div style="font-size: 10px;">
                    not resizable
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane Min="10%">
                Pane B4
                <div style="font-size: 10px;">
                    Min 10%
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane Collapsible="false">
                Pane B5
                <div style="font-size: 10px;">
                    not collapsible
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane Visible="false">
                Pane B6
            </RadzenSplitterPane>
            <RadzenSplitterPane Resizable="false">
                Pane B7
                <div style="font-size: 10px;">
                    not resizable
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane>
                Pane B8
            </RadzenSplitterPane>
        </RadzenSplitter>
    </RadzenSplitterPane>
    <RadzenSplitterPane>
        <RadzenSplitter Collapse=@OnCollapseDisabled Resize=@OnResizeDisabled>
            <RadzenSplitterPane>
                Pane C1
                <div style="font-size: 10px;">
                    collapse and resize programmatically disabled
                </div>
            </RadzenSplitterPane>
            <RadzenSplitterPane>
                Pane C2
                <div style="font-size: 10px;">
                    collapse and resize programmatically disabled
                </div>
            </RadzenSplitterPane>
        </RadzenSplitter>
    </RadzenSplitterPane>
</RadzenSplitter>

@code {
    void OnCollapse(RadzenSplitterEventArgs args)
    {
        Console.WriteLine($"Pane {args.PaneIndex} Collapse");
    }

    void OnExpand(RadzenSplitterEventArgs args)
    {
        Console.WriteLine($"Pane {args.PaneIndex} Expand");
    }

    void OnResize(RadzenSplitterResizeEventArgs args)
    {
        Console.WriteLine($"Pane {args.PaneIndex} Resize (New size {args.NewSize})");
    }

    void OnCollapseDisabled(RadzenSplitterEventArgs args)
    {
        args.Cancel = true;
        Console.WriteLine($"Pane {args.PaneIndex} Collapse programmatically disabled");
    }

    void OnResizeDisabled(RadzenSplitterResizeEventArgs args)
    {
        args.Cancel = true;
        Console.WriteLine($"Pane {args.PaneIndex} Resize (New size {args.NewSize}) programmatically disabled");
    }
}
```