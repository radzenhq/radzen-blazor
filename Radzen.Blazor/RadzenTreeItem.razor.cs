using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenTreeItem.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    public partial class RadzenTreeItem : IDisposable
    {
        /// <summary>
        /// Gets the content class list.
        /// </summary>
        /// <value>The content class list.</value>
        ClassList ContentClassList => ClassList.Create("rz-treenode-content")
                                               .Add("rz-treenode-content-selected", selected);
        /// <summary>
        /// Gets the icon class list.
        /// </summary>
        /// <value>The icon class list.</value>
        ClassList IconClassList => ClassList.Create("rz-tree-toggler rzi")
                                               .Add("rzi-caret-down", expanded)
                                               .Add("rzi-caret-right", !expanded);
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<RadzenTreeItem> Template { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// The expanded
        /// </summary>
        private bool expanded;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenTreeItem"/> is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Expanded { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has children.
        /// </summary>
        /// <value><c>true</c> if this instance has children; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool HasChildren { get; set; }

        /// <summary>
        /// The selected
        /// </summary>
        private bool selected;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenTreeItem"/> is selected.
        /// </summary>
        /// <value><c>true</c> if selected; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Selected { get; set; }

        /// <summary>
        /// Gets or sets the tree.
        /// </summary>
        /// <value>The tree.</value>
        [CascadingParameter]
        public RadzenTree Tree { get; set; }

        /// <summary>
        /// Gets or sets the parent item.
        /// </summary>
        /// <value>The parent item.</value>
        [CascadingParameter]
        public RadzenTreeItem ParentItem { get; set; }

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
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            if (ParentItem != null)
            {
                ParentItem.RemoveItem(this);
            }
            else if (Tree != null)
            {
                Tree.RemoveItem(this);
            }
        }

        /// <summary>
        /// Toggles this instance.
        /// </summary>
        internal async Task Toggle()
        {
            expanded = !expanded;

            if (expanded)
            {
                if (Tree != null)
                {
                    await Tree.ExpandItem(this);

                    if (Tree.SingleExpand)
                    {
                        var siblings = ParentItem?.items ?? Tree.items;

                        foreach (var sibling in siblings)
                        {
                            if (sibling != this && sibling.expanded)
                            {
                                sibling.Toggle();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Selects this instance.
        /// </summary>
        void Select()
        {
            selected = true;
            Tree?.SelectItem(this);
        }

        /// <summary>
        /// Unselects this instance.
        /// </summary>
        internal void Unselect()
        {
            selected = false;
            StateHasChanged();
        }

        /// <summary>
        /// Renders the content of the child.
        /// </summary>
        /// <param name="content">The content.</param>
        internal void RenderChildContent(RenderFragment content)
        {
            ChildContent = content;
        }

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        override protected void OnInitialized()
        {
            expanded = Expanded;

            if (expanded)
            {
                Tree?.ExpandItem(this);
            }

            selected = Selected;

            if (selected)
            {
                Tree?.SelectItem(this);
            }

            if (Tree != null && ParentItem == null)
            {
                Tree.AddItem(this);
            }

            if (ParentItem != null)
            {
                ParentItem.AddItem(this);
            }
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldExpand = false;

            if (parameters.DidParameterChange(nameof(Expanded), Expanded))
            {
                // The Expanded property has changed - update the expanded state
                expanded = parameters.GetValueOrDefault<bool>(nameof(Expanded));
                shouldExpand = true;
            }

            if (parameters.DidParameterChange(nameof(Value), Value))
            {
                // The Value property has changed - the children may have also changed
                shouldExpand = expanded;
            }

            if (shouldExpand)
            {
                // Either the expanded state or Value changed - expand the node to render its children
                Tree?.ExpandItem(this);
            }

            if (parameters.DidParameterChange(nameof(Selected), Selected))
            {
                selected = parameters.GetValueOrDefault<bool>(nameof(Selected));

                if (selected)
                {
                    Tree?.SelectItem(this);
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Checkeds the change.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        async Task CheckedChange(bool? value)
        {
            if (Tree != null)
            {
                var checkedValues = GetCheckedValues();

                if (Tree.AllowCheckChildren)
                {
                    if (value == true)
                    {
                        var valueAndChildren = GetValueAndAllChildValues();
                        checkedValues = checkedValues.Union(valueAndChildren);
                        Tree.SetUncheckedValues(Tree.UncheckedValues.Except(valueAndChildren));
                    }
                    else
                    {
                        var valueAndChildren = GetValueAndAllChildValues();
                        checkedValues = checkedValues.Except(valueAndChildren);
                        Tree.SetUncheckedValues(valueAndChildren.Union(Tree.UncheckedValues));
                    }
                }
                else
                {
                    if (value == true)
                    {
                        var valueWithoutChildren = new[] { Value };
                        checkedValues = checkedValues.Union(valueWithoutChildren);
                        Tree.SetUncheckedValues(Tree.UncheckedValues.Except(valueWithoutChildren));
                    }
                    else
                    {
                        var valueWithoutChildren = new[] { Value };
                        checkedValues = checkedValues.Except(valueWithoutChildren);
                        Tree.SetUncheckedValues(valueWithoutChildren.Union(Tree.UncheckedValues));
                    }
                }

                if (Tree.AllowCheckParents)
                {
                    checkedValues = UpdateCheckedValuesWithParents(checkedValues, value);
                }

                await Tree.SetCheckedValues(checkedValues);
            }
        }

        /// <summary>
        /// Determines whether this instance is checked.
        /// </summary>
        /// <returns><c>true</c> if this instance is checked; otherwise, <c>false</c>.</returns>
        bool? IsChecked()
        {
            var checkedValues = GetCheckedValues();

            if (HasChildren && IsOneChildUnchecked() && IsOneChildChecked())
            {
                return null;
            }

            return checkedValues.Contains(Value);
        }

        /// <summary>
        /// Gets the checked values.
        /// </summary>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> GetCheckedValues()
        {
            return Tree.CheckedValues != null ? Tree.CheckedValues : Enumerable.Empty<object>();
        }

        /// <summary>
        /// Gets all child values.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> GetAllChildValues(Func<object, bool> predicate = null)
        {
            var children = items.Concat(items.SelectManyRecursive(i => i.items)).Select(i => i.Value);

            return predicate != null ? children.Where(predicate) : children;
        }

        /// <summary>
        /// Gets the value and all child values.
        /// </summary>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> GetValueAndAllChildValues()
        {
            return new object[] { Value }.Concat(GetAllChildValues());
        }

        /// <summary>
        /// Ares all children checked.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool AreAllChildrenChecked(Func<object, bool> predicate = null)
        {
            var checkedValues = GetCheckedValues();
            return GetAllChildValues(predicate).All(i => checkedValues.Contains(i));
        }

        /// <summary>
        /// Ares all children unchecked.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool AreAllChildrenUnchecked(Func<object, bool> predicate = null)
        {
            var checkedValues = GetCheckedValues();
            return GetAllChildValues(predicate).All(i => !checkedValues.Contains(i));
        }

        /// <summary>
        /// Determines whether [is one child unchecked].
        /// </summary>
        /// <returns><c>true</c> if [is one child unchecked]; otherwise, <c>false</c>.</returns>
        bool IsOneChildUnchecked()
        {
            var checkedValues = GetCheckedValues();
            return GetAllChildValues().Any(i => !checkedValues.Contains(i));
        }

        /// <summary>
        /// Determines whether [is one child checked].
        /// </summary>
        /// <returns><c>true</c> if [is one child checked]; otherwise, <c>false</c>.</returns>
        bool IsOneChildChecked()
        {
            var checkedValues = GetCheckedValues();
            return GetAllChildValues().Any(i => checkedValues.Contains(i));
        }

        /// <summary>
        /// Updates the checked values with parents.
        /// </summary>
        /// <param name="checkedValues">The checked values.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>IEnumerable&lt;System.Object&gt;.</returns>
        IEnumerable<object> UpdateCheckedValuesWithParents(IEnumerable<object> checkedValues, bool? value)
        {
            var p = ParentItem;
            while (p != null)
            {
                if (value == false && p.AreAllChildrenUnchecked(i => !object.Equals(i, Value)))
                {
                    checkedValues = checkedValues.Except(new object[] { p.Value });
                }
                else if (value == true && p.AreAllChildrenChecked(i => !object.Equals(i, Value)))
                {
                    checkedValues = checkedValues.Union(new object[] { p.Value });
                }

                p = p.ParentItem;
            }

            return checkedValues;
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns><c>true</c> if [contains] [the specified child]; otherwise, <c>false</c>.</returns>
        internal bool Contains(RadzenTreeItem child)
        {
            var parent = child.ParentItem;

            while (parent != null)
            {
                if (parent == this)
                {
                    return true;
                }

                parent = parent.ParentItem;
            }

            return false;
        }
    }
}