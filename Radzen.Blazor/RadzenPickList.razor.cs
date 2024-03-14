﻿using Microsoft.AspNetCore.Components;
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
        public bool Multiple { get; set; } = false;

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
        /// Gets or sets the buttons style
        /// </summary>
        /// <value>The buttons style.</value>
        [Parameter]
        public JustifyContent ButtonJustifyContent { get; set; } = JustifyContent.End;

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

        async Task Update(bool sourceToTarget, IEnumerable<TItem> items)
        {
            if (sourceToTarget)
            {
                if (items != null)
                {
                    Target = (Target ?? Enumerable.Empty<TItem>()).Concat(items);
                    Source = (Source ?? Enumerable.Empty<TItem>()).Except(items);
                }
                else
                {
                    Target = (Target ?? Enumerable.Empty<TItem>()).Concat(Source);
                    Source = null;
                }
            }
            else
            {
                if (items != null)
                {
                    Source = (Source ?? Enumerable.Empty<TItem>()).Concat(items);
                    Target = (Target ?? Enumerable.Empty<TItem>()).Except(items);
                }
                else
                {
                    Source = (Source ?? Enumerable.Empty<TItem>()).Concat(Target);
                    Target = null;
                }
            }

            Source = Source?.Any() == true ? Source : null;
            Target = Target?.Any() == true ? Target : null;

            await SourceChanged.InvokeAsync(Source);
            await TargetChanged.InvokeAsync(Target);

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
            await Update(true, Multiple ? (IEnumerable<TItem>)selectedSourceItems : new List<TItem>() { (TItem)selectedSourceItems } );
        }

        async Task TargetToSource()
        {
            await Update(false, null);
        }

        async Task SelectedTargetToSource()
        {
            await Update(false, Multiple ? (IEnumerable<TItem>)selectedTargetItems : new List<TItem>() { (TItem)selectedTargetItems });
        }
    }
}
