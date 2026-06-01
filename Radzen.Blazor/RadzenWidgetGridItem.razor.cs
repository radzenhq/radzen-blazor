using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// A widget displayed inside a <see cref="RadzenWidgetGrid" />. Each widget occupies a
    /// rectangular area of the grid defined by its <see cref="Col" />, <see cref="Row" />,
    /// <see cref="ColSpan" /> and <see cref="RowSpan" /> and can be moved and resized while the
    /// parent grid is in edit mode.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenWidgetGrid Columns="12" EditMode="true"&gt;
    ///     &lt;RadzenWidgetGridItem Title="Sales" Icon="paid" Col="1" Row="1" ColSpan="4" RowSpan="2"&gt;
    ///         Content
    ///     &lt;/RadzenWidgetGridItem&gt;
    /// &lt;/RadzenWidgetGrid&gt;
    /// </code>
    /// </example>
    public partial class RadzenWidgetGridItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the parent grid.
        /// </summary>
        [CascadingParameter]
        public RadzenWidgetGrid? Grid { get; set; }

        /// <summary>
        /// Gets or sets the body content of the widget.
        /// </summary>
        /// <value>The body content render fragment.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a custom header template. When set it replaces the default header built
        /// from <see cref="Title" /> and <see cref="Icon" />.
        /// </summary>
        /// <value>The header render fragment.</value>
        [Parameter]
        public RenderFragment? HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the widget title displayed in the default header.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the name of the icon displayed before the <see cref="Title" /> in the default header.
        /// </summary>
        /// <value>The icon name.</value>
        [Parameter]
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the one-based column the widget starts at.
        /// </summary>
        /// <value>The start column. Default is <c>1</c>.</value>
        [Parameter]
        public int Col { get; set; } = 1;

        /// <summary>
        /// Gets or sets the callback raised when <see cref="Col" /> changes. Enables <c>@bind-Col</c>.
        /// </summary>
        /// <value>The column changed callback.</value>
        [Parameter]
        public EventCallback<int> ColChanged { get; set; }

        /// <summary>
        /// Gets or sets the one-based row the widget starts at.
        /// </summary>
        /// <value>The start row. Default is <c>1</c>.</value>
        [Parameter]
        public int Row { get; set; } = 1;

        /// <summary>
        /// Gets or sets the callback raised when <see cref="Row" /> changes. Enables <c>@bind-Row</c>.
        /// </summary>
        /// <value>The row changed callback.</value>
        [Parameter]
        public EventCallback<int> RowChanged { get; set; }

        /// <summary>
        /// Gets or sets how many columns the widget spans.
        /// </summary>
        /// <value>The column span. Default is <c>1</c>.</value>
        [Parameter]
        public int ColSpan { get; set; } = 1;

        /// <summary>
        /// Gets or sets the callback raised when <see cref="ColSpan" /> changes. Enables <c>@bind-ColSpan</c>.
        /// </summary>
        /// <value>The column span changed callback.</value>
        [Parameter]
        public EventCallback<int> ColSpanChanged { get; set; }

        /// <summary>
        /// Gets or sets how many rows the widget spans.
        /// </summary>
        /// <value>The row span. Default is <c>1</c>.</value>
        [Parameter]
        public int RowSpan { get; set; } = 1;

        /// <summary>
        /// Gets or sets the callback raised when <see cref="RowSpan" /> changes. Enables <c>@bind-RowSpan</c>.
        /// </summary>
        /// <value>The row span changed callback.</value>
        [Parameter]
        public EventCallback<int> RowSpanChanged { get; set; }

        internal int CurrentCol { get; private set; } = 1;
        internal int CurrentRow { get; private set; } = 1;
        internal int CurrentColSpan { get; private set; } = 1;
        internal int CurrentRowSpan { get; private set; } = 1;

        internal bool IsDragging { get; private set; }

        private int previousCol;
        private int previousRow;
        private int previousColSpan;
        private int previousRowSpan;

        private bool ShowHeader => Grid != null && (Grid.CanMove || HeaderTemplate != null
            || !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Icon));

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return IsDragging ? "rz-widget-grid-item rz-widget-grid-item-active" : "rz-widget-grid-item";
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            CurrentCol = previousCol = Col;
            CurrentRow = previousRow = Row;
            CurrentColSpan = previousColSpan = ColSpan;
            CurrentRowSpan = previousRowSpan = RowSpan;

            Grid?.AddItem(this);
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            // Do not let an external re-render reset the position while the user is dragging.
            if (IsDragging)
            {
                return;
            }

            if (Col != previousCol)
            {
                CurrentCol = previousCol = Col;
            }

            if (Row != previousRow)
            {
                CurrentRow = previousRow = Row;
            }

            if (ColSpan != previousColSpan)
            {
                CurrentColSpan = previousColSpan = ColSpan;
            }

            if (RowSpan != previousRowSpan)
            {
                CurrentRowSpan = previousRowSpan = RowSpan;
            }
        }

        internal void SetPosition(int col, int row)
        {
            CurrentCol = col;
            CurrentRow = row;
        }

        internal void SetSpan(int colSpan, int rowSpan)
        {
            CurrentColSpan = colSpan;
            CurrentRowSpan = rowSpan;
        }

        internal void SetDragging(bool dragging)
        {
            IsDragging = dragging;
        }

        internal async Task<bool> CommitAsync()
        {
            var changed = false;

            if (CurrentCol != Col)
            {
                changed = true;
                Col = previousCol = CurrentCol;
                await ColChanged.InvokeAsync(CurrentCol);
            }

            if (CurrentRow != Row)
            {
                changed = true;
                Row = previousRow = CurrentRow;
                await RowChanged.InvokeAsync(CurrentRow);
            }

            if (CurrentColSpan != ColSpan)
            {
                changed = true;
                ColSpan = previousColSpan = CurrentColSpan;
                await ColSpanChanged.InvokeAsync(CurrentColSpan);
            }

            if (CurrentRowSpan != RowSpan)
            {
                changed = true;
                RowSpan = previousRowSpan = CurrentRowSpan;
                await RowSpanChanged.InvokeAsync(CurrentRowSpan);
            }

            return changed;
        }

        private string GetWidgetStyle()
        {
            var widgetStyle = string.Create(CultureInfo.InvariantCulture,
                $"grid-column: {CurrentCol} / span {CurrentColSpan}; grid-row: {CurrentRow} / span {CurrentRowSpan}; box-sizing: border-box; display: flex; flex-direction: column; position: relative; min-width: 0; min-height: 0; overflow: hidden; background-color: var(--rz-widget-grid-item-background-color, var(--rz-base-background-color)); border: var(--rz-widget-grid-item-border, var(--rz-border-normal)); border-radius: var(--rz-widget-grid-item-border-radius, var(--rz-border-radius)); box-shadow: var(--rz-widget-grid-item-shadow, var(--rz-shadow-1)); z-index: {(IsDragging ? 3 : 1)};");

            return string.IsNullOrEmpty(Style) ? widgetStyle : $"{widgetStyle} {Style}";
        }

        private string GetHeaderStyle()
        {
            var interactiveStyle = Grid != null && Grid.CanMove
                ? "cursor: move; touch-action: none;"
                : string.Empty;

            return string.Create(CultureInfo.InvariantCulture,
                $"box-sizing: border-box; display: flex; flex: 0 0 auto; align-items: center; gap: 0.25rem; padding: 0.5rem 0.75rem; background-color: var(--rz-widget-grid-item-header-background-color, var(--rz-base-100)); border-bottom: var(--rz-widget-grid-item-border, var(--rz-border-normal)); user-select: none; {interactiveStyle}");
        }

        private static string GetDragHandleStyle()
        {
            return "color: var(--rz-text-tertiary-color); cursor: move;";
        }

        private static string GetIconStyle()
        {
            return "color: var(--rz-text-secondary-color);";
        }

        private static string GetTitleStyle()
        {
            return "flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;";
        }

        private static string GetContentStyle()
        {
            return "box-sizing: border-box; flex: 1 1 auto; min-height: 0; padding: 0.75rem; overflow: auto;";
        }

        private static string GetResizeHandleStyle()
        {
            return "position: absolute; inset-inline-end: 0; bottom: 0; width: 1rem; height: 1rem; cursor: nwse-resize; touch-action: none; z-index: 4; background: linear-gradient(135deg, transparent 0%, transparent 50%, var(--rz-text-tertiary-color) 50%, var(--rz-text-tertiary-color) 60%, transparent 60%, transparent 70%, var(--rz-text-tertiary-color) 70%, var(--rz-text-tertiary-color) 80%, transparent 80%);";
        }

        private async Task OnHeaderPointerDown(PointerEventArgs args)
        {
            if (Grid != null && Grid.CanMove)
            {
                await Grid.StartMove(this, args);
            }
        }

        private async Task OnResizePointerDown(PointerEventArgs args)
        {
            if (Grid != null && Grid.CanResize)
            {
                await Grid.StartResize(this, args);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Grid?.RemoveItem(this);
            base.Dispose();
        }
    }
}
