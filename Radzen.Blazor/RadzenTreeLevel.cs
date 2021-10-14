using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTreeLevel.
    /// Implements the <see cref="ComponentBase" />
    /// </summary>
    /// <seealso cref="ComponentBase" />
    public class RadzenTreeLevel : ComponentBase
    {
        /// <summary>
        /// Gets or sets the text property.
        /// </summary>
        /// <value>The text property.</value>
        [Parameter]
        public string TextProperty { get; set; }

        /// <summary>
        /// Gets or sets the children property.
        /// </summary>
        /// <value>The children property.</value>
        [Parameter]
        public string ChildrenProperty { get; set; }

        /// <summary>
        /// Gets or sets the has children function.
        /// </summary>
        /// <value>The has children function.</value>
        [Parameter]
        public Func<object, bool> HasChildren { get; set; } = value => true;

        /// <summary>
        /// Gets or sets the expanded function.
        /// </summary>
        /// <value>The expanded function.</value>
        [Parameter]
        public Func<object, bool> Expanded { get; set; } = value => false;

        /// <summary>
        /// Gets or sets the selected function.
        /// </summary>
        /// <value>The selected function.</value>
        [Parameter]
        public Func<object, bool> Selected { get; set; } = value => false;

        /// <summary>
        /// Gets or sets the text function.
        /// </summary>
        /// <value>The text function.</value>
        [Parameter]
        public Func<object, string> Text { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<RadzenTreeItem> Template { get; set; }

        /// <summary>
        /// Gets or sets the tree.
        /// </summary>
        /// <value>The tree.</value>
        /// <exception cref="NotImplementedException"></exception>
        [CascadingParameter]
        public RadzenTree Tree 
        { 
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                value.AddLevel(this);
            }
        }
    }
}