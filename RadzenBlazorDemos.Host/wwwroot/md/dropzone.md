# DropZone

The Blazor DropZone lets users drag and drop items between zones - for kanban boards and reordering.

Keywords: dropzone, drag, drop

> API reference: [RadzenDropZone API](https://blazor.radzen.com/api/dropzone.md)

## Examples

## Radzen Blazor DropZone

The Blazor DropZone lets users drag and drop items between zones - for kanban boards and reordering.
Drag/Drop tasks between zones to update task status and order or in the same zone to reorder.
Moving tasks between "Not started" and "Completed" zones is disallowed using `CanDrop` callback.
Use `ItemSelector` callback to define which item in which zone will appear, `Drop` callback to customize the drop logic and `ItemRender` callback to customize if the item can be dragged, appearance, etc.

```razor
<RadzenDropZoneContainer TItem="MyTask" Data="data"
                         ItemSelector="@ItemSelector"
                         ItemRender="@OnItemRender"
                         CanDrop="@CanDrop"
                         Drop="@OnDrop">
    <ChildContent>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
            <RadzenDropZone Value="Status.NotStarted" class="rz-display-flex rz-flex-column rz-background-color-warning-lighter rz-border-warning-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Not started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Started" class="rz-display-flex rz-flex-column rz-background-color-info-lighter rz-border-info-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Completed" class="rz-display-flex rz-flex-column rz-background-color-success-lighter rz-border-success-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Completed" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Deleted" class="rz-display-flex rz-flex-column rz-background-color-danger-lighter rz-border-danger-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Drop here to delete" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>
        </RadzenStack>
    </ChildContent>
    <Template>
        <strong>@context.Name</strong>
    </Template>
</RadzenDropZoneContainer>

<style>
    .rz-can-drop {
        background-color: var(--rz-background-color-primary);
    }
</style>

@code {
    // Filter items by zone value
    Func<MyTask, RadzenDropZone<MyTask>, bool> ItemSelector = (item, zone) => item.Status == (Status)zone.Value && item.Status != Status.Deleted;

    Func<RadzenDropZoneItemEventArgs<MyTask>, bool> CanDrop = request =>
    {
        // Allow item drop only in the same zone, in "Deleted" zone or in the next/previous zone.
        return request.FromZone == request.ToZone || (Status)request.ToZone.Value == Status.Deleted ||
            Math.Abs((int)request.Item.Status - (int)request.ToZone.Value) == 1;
    };

    void OnItemRender(RadzenDropZoneItemRenderEventArgs<MyTask> args)
    {
        // Customize item appearance
        if (args.Item.Name == "Task2")
        {
            args.Attributes["draggable"] = "false";
            args.Attributes["style"] = "cursor:not-allowed";
            args.Attributes["class"] = "rz-card rz-variant-flat rz-background-color-primary-lighter rz-color-on-primary-lighter";
        }
        else
        {
            args.Attributes["class"] = "rz-card rz-variant-filled rz-background-color-primary-light rz-color-on-primary-light";
        }

        // Do not render item if deleted
        args.Visible = args.Item.Status != Status.Deleted;
    }

    void OnDrop(RadzenDropZoneItemEventArgs<MyTask> args)
    {
        if (args.FromZone != args.ToZone)
        {
            // update item zone
            args.Item.Status = (Status)args.ToZone.Value;
        }

        if (args.ToItem != null && args.ToItem != args.Item)
        {
            // reorder items in same zone or place the item at specific index in new zone
            data.Remove(args.Item);
            data.Insert(data.IndexOf(args.ToItem), args.Item);
        }
    }

    IList<MyTask> data;

    protected override void OnInitialized()
    {
        data = Enumerable.Range(0, 5)
            .Select(i => 
                new MyTask() 
                { 
                    Id = i, 
                    Name = $"Task{i}", 
                    Status = i < 3 ? Status.NotStarted : Status.Started 
                })
            .ToList();
    }

    public class MyTask
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
    }

    public enum Status
    {
        NotStarted,
        Started,
        Completed,
        Deleted
    }
}
```


### Define can-drop and no-drop styles

Use the built-in `.rz-can-drop` and `.rz-no-drop` CSS classes to apply styles and differentiate the DropZones that allow dropping from those that do not.

```razor
<RadzenDropZoneContainer TItem="MyTask" Data="data"
                         ItemSelector="@ItemSelector"
                         ItemRender="@OnItemRender"
                         CanDrop="@CanDrop"
                         Drop="@OnDrop">
    <ChildContent>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
            <RadzenDropZone Value="Status.NotStarted" class="rz-display-flex rz-flex-column rz-border-base-300 rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Not started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Started" class="rz-display-flex rz-flex-column rz-border-base-300 rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Completed" class="rz-display-flex rz-flex-column rz-border-base-300 rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Completed" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Deleted" class="rz-display-flex rz-flex-column rz-border-danger-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Drop here to delete" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>
        </RadzenStack>
    </ChildContent>
    <Template>
        <strong>@context.Name</strong>
    </Template>
</RadzenDropZoneContainer>

<style>
    .rz-can-drop {
        background-color: var(--rz-info-lighter);
    }
    .rz-no-drop {
        background-color: var(--rz-danger-lighter);
    }
</style>

@code {
    // Filter items by zone value
    Func<MyTask, RadzenDropZone<MyTask>, bool> ItemSelector = (item, zone) => item.Status == (Status)zone.Value && item.Status != Status.Deleted;

    Func<RadzenDropZoneItemEventArgs<MyTask>, bool> CanDrop = request =>
    {
        // Allow item drop only in the same zone, in "Deleted" zone or in the next/previous zone.
        return request.FromZone == request.ToZone || (Status)request.ToZone.Value == Status.Deleted ||
            Math.Abs((int)request.Item.Status - (int)request.ToZone.Value) == 1;
    };

    void OnItemRender(RadzenDropZoneItemRenderEventArgs<MyTask> args)
    {
        args.Attributes["class"] = "rz-card rz-variant-filled rz-background-color-primary-light rz-color-on-primary-light";

        // Do not render item if deleted
        args.Visible = args.Item.Status != Status.Deleted;
    }

    void OnDrop(RadzenDropZoneItemEventArgs<MyTask> args)
    {
        if (args.FromZone != args.ToZone)
        {
            // update item zone
            args.Item.Status = (Status)args.ToZone.Value;
        }

        if (args.ToItem != null && args.ToItem != args.Item)
        {
            // reorder items in same zone or place the item at specific index in new zone
            data.Remove(args.Item);
            data.Insert(data.IndexOf(args.ToItem), args.Item);
        }
    }

    IList<MyTask> data;

    protected override void OnInitialized()
    {
        data = Enumerable.Range(0, 5)
            .Select(i => 
                new MyTask() 
                { 
                    Id = i, 
                    Name = $"Task{i}", 
                    Status = i < 3 ? Status.NotStarted : Status.Started 
                })
            .ToList();
    }

    public class MyTask
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
    }

    public enum Status
    {
        NotStarted,
        Started,
        Completed,
        Deleted
    }
}
```


### Define a Footer Template per Drop Zone

Add a footer template to the dropzone to display items below the rendered items.

```razor
<RadzenDropZoneContainer TItem="MyTask" Data="data"
    ItemSelector="@ItemSelector"
    ItemRender="@OnItemRender"
    CanDrop="@CanDrop"
    Drop="@OnDrop">
    <ChildContent>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
            <RadzenDropZone Value="Status.NotStarted" class="rz-display-flex rz-flex-column rz-background-color-warning-lighter rz-border-warning-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <ChildContent>
                    <RadzenText Text="Not started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />    
                </ChildContent>
                <Footer>
                    <div>
                        <RadzenButton Size="ButtonSize.ExtraSmall" Icon="add" ButtonStyle="ButtonStyle.Success" Click="@CreateItem" />    
                    </div>
                </Footer>
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Started" class="rz-display-flex rz-flex-column rz-background-color-info-lighter rz-border-info-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Started" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Completed" class="rz-display-flex rz-flex-column rz-background-color-success-lighter rz-border-success-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Completed" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>

            <RadzenDropZone Value="Status.Deleted" class="rz-display-flex rz-flex-column rz-background-color-danger-lighter rz-border-danger-light rz-border-radius-2 rz-p-4" Style="flex: 1; gap: 1rem;">
                <RadzenText Text="Drop here to delete" TextStyle="TextStyle.Subtitle2" TagName="TagName.P" />
            </RadzenDropZone>
        </RadzenStack>
    </ChildContent>
    <Template>
        <strong>@context.Name</strong>
    </Template>
</RadzenDropZoneContainer>

<style>
    .rz-can-drop {
        background-color: var(--rz-background-color-primary);
    }
</style>

@code {

// Filter items by zone value
    Func<MyTask, RadzenDropZone<MyTask>, bool> ItemSelector = (item, zone) => item.Status == (Status)zone.Value && item.Status != Status.Deleted;

    Func<RadzenDropZoneItemEventArgs<MyTask>, bool> CanDrop = request =>
    {
// Allow item drop only in the same zone, in "Deleted" zone or in the next/previous zone.
        return request.FromZone == request.ToZone || (Status)request.ToZone.Value == Status.Deleted ||
               Math.Abs((int)request.Item.Status - (int)request.ToZone.Value) == 1;
    };

    void OnItemRender(RadzenDropZoneItemRenderEventArgs<MyTask> args)
    {
// Customize item appearance
        if (args.Item.Name == "Task2")
        {
            args.Attributes["draggable"] = "false";
            args.Attributes["style"] = "cursor:not-allowed";
            args.Attributes["class"] = "rz-card rz-variant-flat rz-background-color-primary-lighter rz-color-on-primary-lighter";
        }
        else
        {
            args.Attributes["class"] = "rz-card rz-variant-filled rz-background-color-primary-light rz-color-on-primary-light";
        }

// Do not render item if deleted
        args.Visible = args.Item.Status != Status.Deleted;
    }

    void OnDrop(RadzenDropZoneItemEventArgs<MyTask> args)
    {
        if (args.FromZone != args.ToZone)
        {
// update item zone
            args.Item.Status = (Status)args.ToZone.Value;
        }

        if (args.ToItem != null && args.ToItem != args.Item)
        {
// reorder items in same zone or place the item at specific index in new zone
            data.Remove(args.Item);
            data.Insert(data.IndexOf(args.ToItem), args.Item);
        }
    }

    IList<MyTask> data;

    protected override void OnInitialized()
    {
        data = Enumerable.Range(0, 5)
            .Select(i =>
                new MyTask()
                {
                    Id = i,
                    Name = $"Task{i}",
                    Status = i < 3 ? Status.NotStarted : Status.Started
                })
            .ToList();
    }

    private void CreateItem()
    {
        data.Add(new MyTask()
        {
            Id = data.Max(t => t.Id) + 1,
            Name = "New Task",
            Status = Status.NotStarted
        });
    }

    public class MyTask
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; } = Status.NotStarted;
    }

    public enum Status
    {
        NotStarted,
        Started,
        Completed,
        Deleted
    }

}
```
