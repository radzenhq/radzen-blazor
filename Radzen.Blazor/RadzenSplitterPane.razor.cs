using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSplitterPane component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenSplitterPane : RadzenComponent
    {
        RadzenSplitter _splitter;
        private string _size;

        internal string SizeRuntine { get; set; }

        internal bool SizeAuto { get; set; }

        internal int Index { get; set; }

        internal bool IsLastResizable
        {
            get { return Splitter.Panes.Last(o => o.Resizable && !o.Collapsed) == this; }
        }

        internal bool IsLast => Splitter.Panes.Count - 1 == Index;

        internal RadzenSplitterPane Next()
        {
            return Index <= Splitter.Panes.Count - 2
                ? Splitter.Panes[Index + 1]
                : null;
        }

        internal bool IsResizable
        {
            get
            {
                var paneNext = Next();

                if (Collapsed
                    || (Index == Splitter.Panes.Count - 2 && !paneNext.IsResizable)
                    || (IsLastResizable && paneNext != null && paneNext.Collapsed)
                    )
                    return false;


                return Resizable;
            }
        }

        internal bool IsCollapsible
        {
            get
            {
                if (Collapsible && !Collapsed)
                    return true;

                var paneNext = Next();
                if (paneNext == null)
                    return false;

                return paneNext.IsLast && paneNext.Collapsible && paneNext.Collapsed;
            }
        }

        internal bool IsExpandable
        {
            get
            {
                if (Collapsed)
                    return true;

                var paneNext = Next();
                if (paneNext == null)
                    return false;

                return paneNext.IsLast && paneNext.Collapsible && !paneNext.Collapsed;
            }
        }

        internal string ClassName
        {
            get
            {
                if (Collapsed)
                    return "collapsed";

                if (IsLastResizable)
                    return "lastresizable";

                if (IsResizable)
                    return "resizable";

                return "locked";
            }
        }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is collapsible.
        /// </summary>
        /// <value><c>true</c> if collapsible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is collapsed.
        /// </summary>
        /// <value><c>true</c> if collapsed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Collapsed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterPane"/> is resizable.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Resizable { get; set; } = true;

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public string Max { get; set; }

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public string Min { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public string Size
        {
            get => SizeRuntine ?? _size;
            set => _size = value;
        }

        /// <summary>
        /// Gets or sets the splitter.
        /// </summary>
        /// <value>The splitter.</value>
        [CascadingParameter]
        public RadzenSplitter Splitter
        {
            get => _splitter;
            set
            {
                if (_splitter != value)
                {
                    _splitter = value;
                    _splitter.AddPane(this);
                }
            }
        }


        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            Splitter?.RemovePane(this);
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return $"rz-splitter-pane rz-splitter-pane-{ClassName}";
        }

        /// <summary>
        /// Gets the component bar CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetComponentBarCssClass()
        {
            return $"rz-splitter-bar rz-splitter-bar-{ClassName}";
        }
    }
}