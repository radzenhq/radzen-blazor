using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenTree.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenTree : RadzenComponent
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-tree";
        }

        /// <summary>
        /// Gets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        internal RadzenTreeItem SelectedItem { get; private set; }

        /// <summary>
        /// Gets or sets the expanded item.
        /// </summary>
        /// <value>The expanded item.</value>
        RadzenTreeItem ExpandedItem { get; set; }

        /// <summary>
        /// Gets or sets the levels.
        /// </summary>
        /// <value>The levels.</value>
        IList<RadzenTreeLevel> Levels { get; set; } = new List<RadzenTreeLevel>();

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<TreeEventArgs> Change { get; set; }

        /// <summary>
        /// Gets or sets the expand.
        /// </summary>
        /// <value>The expand.</value>
        [Parameter]
        public EventCallback<TreeExpandEventArgs> Expand { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable Data { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<object> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow check boxes].
        /// </summary>
        /// <value><c>true</c> if [allow check boxes]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCheckBoxes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow check children].
        /// </summary>
        /// <value><c>true</c> if [allow check children]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCheckChildren { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [allow check parents].
        /// </summary>
        /// <value><c>true</c> if [allow check parents]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowCheckParents { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [single expand].
        /// </summary>
        /// <value><c>true</c> if [single expand]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool SingleExpand { get; set; }

        /// <summary>
        /// Gets or sets the checked values.
        /// </summary>
        /// <value>The checked values.</value>
        [Parameter]
        public IEnumerable<object> CheckedValues { get; set; } = Enumerable.Empty<object>();

        /// <summary>
        /// The items
        /// </summary>
        internal List<RadzenTreeItem> items = new List<RadzenTreeItem>();

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal void AddItem(RadzenTreeItem item)
        {
            if (items.IndexOf(item) == -1)
            {
                items.Add(item);
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal void RemoveItem(RadzenTreeItem item)
        {
            if (items.IndexOf(item) != -1)
            {
                items.Remove(item);
            }
        }

        /// <summary>
        /// Sets the checked values.
        /// </summary>
        /// <param name="values">The values.</param>
        internal async Task SetCheckedValues(IEnumerable<object> values)
        {
            CheckedValues = values.ToList();
            await CheckedValuesChanged.InvokeAsync(CheckedValues);
        }

        /// <summary>
        /// Gets or sets the unchecked values.
        /// </summary>
        /// <value>The unchecked values.</value>
        internal IEnumerable<object> UncheckedValues { get; set; } = Enumerable.Empty<object>();
        /// <summary>
        /// Sets the unchecked values.
        /// </summary>
        /// <param name="values">The values.</param>
        internal void SetUncheckedValues(IEnumerable<object> values)
        {
            UncheckedValues = values.ToList();
        }

        /// <summary>
        /// Gets or sets the checked values changed.
        /// </summary>
        /// <value>The checked values changed.</value>
        [Parameter]
        public EventCallback<IEnumerable<object>> CheckedValuesChanged { get; set; }

        /// <summary>
        /// Renders the tree item.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="data">The data.</param>
        /// <param name="template">The template.</param>
        /// <param name="text">The text.</param>
        /// <param name="hasChildren">The has children.</param>
        /// <param name="expanded">The expanded.</param>
        /// <param name="selected">The selected.</param>
        void RenderTreeItem(RenderTreeBuilder builder, object data, RenderFragment<RadzenTreeItem> template, Func<object, string> text,
            Func<object, bool> hasChildren, Func<object, bool> expanded, Func<object, bool> selected)
        {
            builder.OpenComponent<RadzenTreeItem>(0);
            builder.AddAttribute(1, nameof(RadzenTreeItem.Text), text(data));
            builder.AddAttribute(2, nameof(RadzenTreeItem.Value), data);
            builder.AddAttribute(3, nameof(RadzenTreeItem.HasChildren), hasChildren(data));
            builder.AddAttribute(4, nameof(RadzenTreeItem.Template), template);
            builder.AddAttribute(5, nameof(RadzenTreeItem.Expanded), expanded(data));
            builder.AddAttribute(6, nameof(RadzenTreeItem.Selected), Value == data || selected(data));
            builder.SetKey(data);
        }

        /// <summary>
        /// Renders the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="depth">The depth.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderChildren(IEnumerable children, int depth)
        {
            var level = depth < Levels.Count() ? Levels.ElementAt(depth) : Levels.Last();

            return new RenderFragment(builder =>
            {
                Func<object, string> text = null;

                foreach (var data in children)
                {
                    if (text == null)
                    {
                        text = level.Text ?? Getter<string>(data, level.TextProperty);
                    }

                    RenderTreeItem(builder, data, level.Template, text, level.HasChildren, level.Expanded, level.Selected);

                    var hasChildren = level.HasChildren(data);

                    if (!string.IsNullOrEmpty(level.ChildrenProperty))
                    {
                        var grandChildren = PropertyAccess.GetValue(data, level.ChildrenProperty) as IEnumerable;

                        if (grandChildren != null && hasChildren)
                        {
                            builder.AddAttribute(7, "ChildContent", RenderChildren(grandChildren, depth + 1));
                        }
                    }

                    builder.CloseComponent();
                }
            });
        }

        /// <summary>
        /// Selects the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal async Task SelectItem(RadzenTreeItem item)
        {
            var selectedItem = SelectedItem;

            if (selectedItem != item)
            {
                SelectedItem = item;

                selectedItem?.Unselect();

                if (Value != item.Value)
                {
                    await ValueChanged.InvokeAsync(item.Value);
                }

                await Change.InvokeAsync(new TreeEventArgs()
                {
                    Text = item?.Text,
                    Value = item?.Value
                });
            }
        }

        /// <summary>
        /// Expands the item.
        /// </summary>
        /// <param name="item">The item.</param>
        internal async Task ExpandItem(RadzenTreeItem item)
        {
            var args = new TreeExpandEventArgs()
            {
                Text = item?.Text,
                Value = item?.Value,
                Children = new TreeItemSettings()
            };

            await Expand.InvokeAsync(args);

            if (args.Children.Data != null)
            {
                var childContent = new RenderFragment(builder =>
                {
                    Func<object, string> text = null;
                    var children = args.Children;

                    foreach (var data in children.Data)
                    {
                        if (text == null)
                        {
                            text = children.Text ?? Getter<string>(data, children.TextProperty);
                        }

                        RenderTreeItem(builder, data, children.Template, text, children.HasChildren, children.Expanded, children.Selected);
                        builder.CloseComponent();
                    }
                });

                item.RenderChildContent(childContent);

                if (AllowCheckBoxes && AllowCheckChildren && args.Children.Data != null)
                {
                    if (CheckedValues != null && CheckedValues.Contains(item.Value))
                    {
                        await SetCheckedValues(CheckedValues.Union(args.Children.Data.Cast<object>().Except(UncheckedValues)));
                    }
                    else
                    {
                        await SetCheckedValues(CheckedValues.Except(args.Children.Data.Cast<object>()));
                    }
                }
            }
        }

        /// <summary>
        /// Getters the specified data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="property">The property.</param>
        /// <returns>Func&lt;System.Object, T&gt;.</returns>
        Func<object, T> Getter<T>(object data, string property)
        {
            if (string.IsNullOrEmpty(property))
            {
                return (value) => (T)value;
            }

            return PropertyAccess.Getter<T>(data, property);
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                var value = parameters.GetValueOrDefault<object>(nameof(Value));

                if (value == null)
                {
                    SelectedItem = null;
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Adds the level.
        /// </summary>
        /// <param name="level">The level.</param>
        internal void AddLevel(RadzenTreeLevel level)
        {
            if (!Levels.Contains(level))
            {
                Levels.Add(level);
                StateHasChanged();
            }
        }
    }
}