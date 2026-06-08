using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tile displayed inside a <see cref="RadzenTileLayout" />. Each tile occupies a
    /// rectangular area of the grid defined by its <see cref="Col" />, <see cref="Row" />,
    /// <see cref="ColSpan" /> and <see cref="RowSpan" /> and can be moved and resized while the
    /// parent layout is in edit mode.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTileLayout Columns="12" EditMode="true"&gt;
    ///     &lt;RadzenTileLayoutItem Title="Sales" Icon="paid" Col="1" Row="1" ColSpan="4" RowSpan="2"&gt;
    ///         Content
    ///     &lt;/RadzenTileLayoutItem&gt;
    /// &lt;/RadzenTileLayout&gt;
    /// </code>
    /// </example>
    public partial class RadzenTileLayoutItem : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the parent layout.
        /// </summary>
        [CascadingParameter]
        public RadzenTileLayout? Layout { get; set; }

        /// <summary>
        /// Gets or sets the body content of the tile.
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
        /// Gets or sets the tile title displayed in the default header.
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
        /// Gets or sets the one-based column the tile starts at.
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
        /// Gets or sets the one-based row the tile starts at.
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
        /// Gets or sets how many columns the tile spans.
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
        /// Gets or sets how many rows the tile spans.
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

        private bool ShowHeader => Layout != null && (Layout.CanMove || HeaderTemplate != null
            || !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Icon));

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return IsDragging ? "rz-tile-layout-item rz-tile-layout-item-active" : "rz-tile-layout-item";
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            CurrentCol = previousCol = Col;
            CurrentRow = previousRow = Row;
            CurrentColSpan = previousColSpan = ColSpan;
            CurrentRowSpan = previousRowSpan = RowSpan;

            Layout?.AddItem(this);
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

        private string GetTileStyle()
        {
            var tileStyle = string.Create(CultureInfo.InvariantCulture,
                $"grid-column: {CurrentCol} / span {CurrentColSpan}; grid-row: {CurrentRow} / span {CurrentRowSpan}; box-sizing: border-box; display: flex; flex-direction: column; position: relative; min-width: 0; min-height: 0; overflow: hidden; background-color: var(--rz-tile-layout-item-background-color, var(--rz-base-background-color)); border: var(--rz-tile-layout-item-border, var(--rz-border-normal)); border-radius: var(--rz-tile-layout-item-border-radius, var(--rz-border-radius)); box-shadow: var(--rz-tile-layout-item-shadow, var(--rz-shadow-1)); z-index: {(IsDragging ? 3 : 1)};");

            return string.IsNullOrEmpty(Style) ? tileStyle : $"{tileStyle} {Style}";
        }

        private string GetHeaderStyle()
        {
            var interactiveStyle = Layout != null && Layout.CanMove
                ? "cursor: move; touch-action: none;"
                : string.Empty;

            return string.Create(CultureInfo.InvariantCulture,
                $"box-sizing: border-box; display: flex; flex: 0 0 auto; align-items: center; gap: 0.25rem; padding: 0.5rem 0.75rem; background-color: var(--rz-tile-layout-item-header-background-color, var(--rz-base-100)); border-bottom: var(--rz-tile-layout-item-border, var(--rz-border-normal)); user-select: none; {interactiveStyle}");
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
            if (Layout != null && Layout.CanMove)
            {
                await Layout.StartMove(this, args);
            }
        }

        private async Task OnResizePointerDown(PointerEventArgs args)
        {
            if (Layout != null && Layout.CanResize)
            {
                await Layout.StartResize(this, args);
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Layout?.RemoveItem(this);
            base.Dispose();
        }
    }
}
