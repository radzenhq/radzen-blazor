using Microsoft.AspNetCore.Components;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenSplitterPane.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenSplitterPane : RadzenComponent
    {
        /// <summary>
        /// The splitter
        /// </summary>
        RadzenSplitter _splitter;
        /// <summary>
        /// The size
        /// </summary>
        private string _size;

        /// <summary>
        /// Gets or sets the size runtine.
        /// </summary>
        /// <value>The size runtine.</value>
        internal string SizeRuntine { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [size automatic].
        /// </summary>
        /// <value><c>true</c> if [size automatic]; otherwise, <c>false</c>.</value>
        internal bool SizeAuto { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        internal int Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is last resizable.
        /// </summary>
        /// <value><c>true</c> if this instance is last resizable; otherwise, <c>false</c>.</value>
        internal bool IsLastResizable
        {
            get { return Splitter.Panes.Last(o => o.Resizable && !o.Collapsed) == this; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is last.
        /// </summary>
        /// <value><c>true</c> if this instance is last; otherwise, <c>false</c>.</value>
        internal bool IsLast => Splitter.Panes.Count - 1 == Index;

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns>RadzenSplitterPane.</returns>
        internal RadzenSplitterPane Next()
        {
            return Index <= Splitter.Panes.Count - 2
                ? Splitter.Panes[Index + 1]
                : null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is resizable.
        /// </summary>
        /// <value><c>true</c> if this instance is resizable; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether this instance is collapsible.
        /// </summary>
        /// <value><c>true</c> if this instance is collapsible; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether this instance is expandable.
        /// </summary>
        /// <value><c>true</c> if this instance is expandable; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
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
        /// Determines the maximum of the parameters.
        /// </summary>
        /// <value>The maximum.</value>
        [Parameter]
        public string Max { get; set; }

        /// <summary>
        /// Determines the minimum of the parameters.
        /// </summary>
        /// <value>The minimum.</value>
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