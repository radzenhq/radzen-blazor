using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCard component.
    /// </summary>
    public partial class RadzenPickList<TItem> : RadzenComponent
    {
        /// <summary>
        /// Gets or sets a value indicating whether multiple selection is allowed.
        /// </summary>
        /// <value><c>true</c> if multiple selection is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether component is disabled.
        /// </summary>
        /// <value><c>true</c> if component is disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the source header
        /// </summary>
        /// <value>The source header.</value>
        [Parameter]
        public RenderFragment SourceHeader { get; set; }

        /// <summary>
        /// Gets or sets the target header
        /// </summary>
        /// <value>The target header.</value>
        [Parameter]
        public RenderFragment TargetHeader { get; set; }

        /// <summary>
        /// Gets or sets the text property
        /// </summary>
        /// <value>The text property.</value>
        [Parameter]
        public string TextProperty { get; set; }

        /// <summary>
        /// Gets or sets the source template
        /// </summary>
        /// <value>The source template.</value>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        private RenderFragment<dynamic> ListBoxTemplate => Template != null ? item => Template((TItem)item) : null;

        /// <summary>
        /// Gets or sets value if filtering is allowed.
        /// </summary>
        /// <value>The allow filtering value.</value>
        [Parameter]
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// Gets or sets value if headers are shown.
        /// </summary>
        /// <value>If headers are shown value.</value>
        [Parameter]
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Gets or sets the buttons spacing
        /// </summary>
        /// <value>The buttons spacing.</value>
        [Parameter]
        public string ButtonGap { get; set; }

        /// <summary>
        /// Gets or sets the orientation
        /// </summary>
        /// <value>The orientation.</value>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the buttons style
        /// </summary>
        /// <value>The buttons style.</value>
        [Parameter]
        public JustifyContent ButtonJustifyContent { get; set; } = JustifyContent.Center;

        /// <summary>
        /// Gets or sets the buttons style
        /// </summary>
        /// <value>The buttons style.</value>
        [Parameter]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        /// <summary>
        /// Gets or sets the design variant of the buttons.
        /// </summary>
        /// <value>The variant of the buttons.</value>
        [Parameter]
        public Variant ButtonVariant { get; set; } = Variant.Text;

        /// <summary>
        /// Gets or sets the color shade of the buttons.
        /// </summary>
        /// <value>The color shade of the buttons.</value>
        [Parameter]
        public Shade ButtonShade { get; set; } = Shade.Default;

        /// <summary>
        /// Gets or sets the buttons size.
        /// </summary>
        /// <value>The buttons size.</value>
        [Parameter]
        public ButtonSize ButtonSize { get; set; } = ButtonSize.Medium;

        /// <summary>
        /// Gets or sets the source to target title
        /// </summary>
        /// <value>The source to target title.</value>
        [Parameter]
        public string SourceToTargetTitle { get; set; } = "Move all items from source to target collection";

        /// <summary>
        /// Gets or sets the selected source to target title
        /// </summary>
        /// <value>The selected source to target title.</value>
        [Parameter]
        public string SelectedSourceToTargetTitle { get; set; } = "Move all selected source items to target collection";

        /// <summary>
        /// Gets or sets the target to source title
        /// </summary>
        /// <value>The target to source title.</value>
        [Parameter]
        public string TargetToSourceTitle { get; set; } = "Move all items from target to source collection";

        /// <summary>
        /// Gets or sets the selected target to source  title
        /// </summary>
        /// <value>The selected target to source title.</value>
        [Parameter]
        public string SelectedTargetToSourceTitle { get; set; } = "Move selected target items to source collection";

        /// <summary>
        /// Gets the final CSS style rendered by the component. Combines it with a <c>style</c> custom attribute.
        /// </summary>
        protected string GetStyle()
        {
            if (Attributes != null && Attributes.TryGetValue("style", out var style) && !string.IsNullOrEmpty(Convert.ToString(@style)))
            {
                return $"{Style} {@style}";
            }

            return Style;
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-picklist";
        }

        /// <summary>
        /// Gets or sets the source collection.
        /// </summary>
        /// <value>The source collection.</value>
        [Parameter]
        public IEnumerable<TItem> Source { get; set; }

        IEnumerable<TItem> source;

        /// <summary>
        /// Gets or sets the source changed.
        /// </summary>
        /// <value>The source changed.</value>
        [Parameter]
        public EventCallback<IEnumerable<TItem>> SourceChanged { get; set; }

        /// <summary>
        /// Gets or sets the target collection.
        /// </summary>
        /// <value>The target collection.</value>
        [Parameter]
        public IEnumerable<TItem> Target { get; set; }

        IEnumerable<TItem> target;

        /// <summary>
        /// Gets or sets the target changed.
        /// </summary>
        /// <value>The target changed.</value>
        [Parameter]
        public EventCallback<IEnumerable<TItem>> TargetChanged { get; set; }

        object selectedSourceItems;
        object selectedTargetItems;

        string sourceSearchText;
        string targetSearchText;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var sourceChanged = parameters.DidParameterChange(nameof(Source), Source);
            if (sourceChanged)
            {
                source = parameters.GetValueOrDefault<IEnumerable<TItem>>(nameof(Source));
            }

            var targetChanged = parameters.DidParameterChange(nameof(Target), Target);
            if (targetChanged)
            {
                target = parameters.GetValueOrDefault<IEnumerable<TItem>>(nameof(Target));
            }

            if (parameters.DidParameterChange(nameof(Multiple), Multiple))
            {
                selectedSourceItems = null;
                selectedTargetItems = null;
            }

            await base.SetParametersAsync(parameters);
        }

        async Task Update(bool sourceToTarget, IEnumerable<TItem> items)
        {
            if (sourceToTarget)
            {
                if (items != null)
                {
                    target = (target ?? Enumerable.Empty<TItem>()).Concat(items);
                    source = (source ?? Enumerable.Empty<TItem>()).Except(items);
                }
                else
                {
                    target = (target ?? Enumerable.Empty<TItem>()).Concat(source);
                    source = null;
                }
            }
            else
            {
                if (items != null)
                {
                    source = (source ?? Enumerable.Empty<TItem>()).Concat(items);
                    target = (target ?? Enumerable.Empty<TItem>()).Except(items);
                }
                else
                {
                    source = (source ?? Enumerable.Empty<TItem>()).Concat(target);
                    target = null;
                }
            }

            source = source?.Any() == true ? source : null;
            target = target?.Any() == true ? target : null;

            await SourceChanged.InvokeAsync(source);
            await TargetChanged.InvokeAsync(target);

            selectedSourceItems = null;
            selectedTargetItems = null;

            if (items == null)
            {
                sourceSearchText = null;
                targetSearchText = null;
            }

            StateHasChanged();
        }

        async Task SourceToTarget()
        {
            await Update(true, null);
        }

        async Task SelectedSourceToTarget()
        {
            await Update(true, Multiple ? (selectedSourceItems as IEnumerable)?.Cast<TItem>() : new List<TItem>() { (TItem)selectedSourceItems } );
        }

        async Task TargetToSource()
        {
            await Update(false, null);
        }

        async Task SelectedTargetToSource()
        {
            await Update(false, Multiple ? (selectedTargetItems as IEnumerable)?.Cast<TItem>() : new List<TItem>() { (TItem)selectedTargetItems });
        }
    }
}
