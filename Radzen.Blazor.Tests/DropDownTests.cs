using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DropDownTests
    {
        class DataItem
        {
            public string Text { get; set; }
            public int Id { get; set; }
            public bool Disabled { get; set; }
        }

        private static IRenderedComponent<RadzenDropDown<T>> DropDown<T>(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenDropDown<T>>> configure = null)
        {
            var data = new[] {
                new DataItem { Text = "Item 1", Id = 1 },
                new DataItem { Text = "Item 2", Id = 2 },
            };

            var component = ctx.RenderComponent<RadzenDropDown<T>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, nameof(DataItem.Text));

                if (configure != null)
                {
                    configure.Invoke(parameters);
                }
                else
                {
                    parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
                }
            });

            return component;
        }


        [Fact]
        public async Task Dropdown_SelectItem_Method_Should_Not_Throw()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            Assert.Equal(2, items.Count);

            //this throws
            await component.InvokeAsync(async () => await component.Instance.SelectItem(1));
        }

        [Fact]
        public void DropDown_RendersItems()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleForIntValue()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<int>(ctx);

            var items = component.FindAll(".rz-dropdown-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-dropdown-item");

            Assert.Contains("rz-state-highlight", items[0].ClassList);
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleForStringValue()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Text));
            });

            var items = component.FindAll(".rz-dropdown-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-dropdown-item");

            Assert.Contains("rz-state-highlight", items[0].ClassList);
        }

        [Fact]
        public void DropDown_Respects_ItemEqualityComparer()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            List<DataItem> boundCollection = [new() { Text = "Item 2" }];

            var component = DropDown<List<DataItem>>(ctx, parameters =>
            {
                parameters.Add(p => p.ItemComparer, new DataItemComparer());
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, boundCollection);
            });

            var selectedItems = component.FindAll(".rz-state-highlight");
            Assert.Equal(1, selectedItems.Count);
            Assert.Equal("Item 2", selectedItems[0].TextContent.Trim());

            // select Item 1 in list
            var items = component.FindAll(".rz-multiselect-item");
            items[0].Click();
            component.Render();

            selectedItems = component.FindAll(".rz-state-highlight");
            Assert.Equal(2, selectedItems.Count);
            Assert.Equal("Item 1", selectedItems[0].TextContent.Trim());
        }

        [Fact]
        public void DropDown_AppliesSelectionStyleWhenMultipleSelectionIsEnabled()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Text));
                parameters.Add(p => p.Multiple, true);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();

            component.Render();

            items = component.FindAll(".rz-multiselect-item");

            items[1].Click();

            component.Render();

            var selectedItems = component.FindAll(".rz-state-highlight");

            Assert.Equal(2, selectedItems.Count);
        }

        [Fact]
        public void DropDown_AppliesValueTemplateOnMultipleSelection()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.Find(".rz-inputtext");

            Assert.Contains("value: Item 1,value: Item 2", selectedItems.Text());
        }

        [Fact]
        public void DropDown_AppliesValueTemplateWhenTepmlateDefined()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var templateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"template: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment)
                    .Add(p => p.Template, templateFragment);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.Find(".rz-inputtext");
            var itemsText = component.FindAll(".rz-multiselect-item-content");

            Assert.Collection(itemsText, item => Assert.Contains("template: Item 1", item.Text()), item => Assert.Contains("template: Item 2", item.Text()));
            Assert.Contains("value: Item 1,value: Item 2", selectedItems.Text());
        }

        [Fact]
        public void DropDown_AppliesValueTemplateOnMultipleSelectionChips()
        {
            using var ctx = new TestContext();

            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var valueTemplateFragment = (RenderFragment<dynamic>)(_context =>
            builder =>
            {
                builder.AddContent(0, $"value: {_context.Text}");
            });

            var component = DropDown<string>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true)
                    .Add(p => p.ValueTemplate, valueTemplateFragment)
                    .Add(p => p.Chips, true);
            });

            var items = component.FindAll(".rz-multiselect-item");

            items[0].Click();
            items[1].Click();

            component.Render();

            var selectedItems = component.FindAll(".rz-chip-text");

            Assert.Collection(selectedItems, item => Assert.Contains("value: Item 1", item.Text()), item => Assert.Contains("value: Item 2", item.Text()));
        }

        [Theory]
        [InlineData(false, true, false, true, "false")]
        [InlineData(true, false, true, false, "true")]
        [InlineData(true, false, false, false, "false")]
        [InlineData(true, false, false, true, "true")]
        [InlineData(false, false, false, true, "false")]
        public void DropDown_AllSelectedFalseIfListIsAllDisabled(bool item1Selected, bool item1Disabled, bool item2Selected, bool item2Disabled, string expectedAriaCheckedValue)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var data = new[] {
                new DataItem { Text = "Item 1", Id = 1, Disabled = item1Disabled },
                new DataItem { Text = "Item 2", Id = 2, Disabled = item2Disabled },
            };

            List<int> selectedValues = [];
            if (item1Selected)
            {
                selectedValues.Add(data[0].Id);
            }
            if (item2Selected)
            {
                selectedValues.Add(data[1].Id);
            }

            var component = ctx.RenderComponent<RadzenDropDown<List<int>>>(parameters => parameters
               .Add(p => p.Data, data)
               .Add(p => p.Value, selectedValues)
               .Add(p => p.Multiple, true)
               .Add(p => p.AllowSelectAll, true)
               .Add(p => p.TextProperty, nameof(DataItem.Text))
               .Add(p => p.DisabledProperty, nameof(DataItem.Disabled))
               .Add(p => p.ValueProperty, nameof(DataItem.Id)));

            Assert.NotNull(component);
            var highlightedItems = component.FindAll(".rz-state-highlight");
            Assert.Equal(selectedValues.Count, highlightedItems.Count);


            var selectAllCheckBox = component.Find(".rz-multiselect-header input[type='checkbox']");

            Assert.Equal(expectedAriaCheckedValue, selectAllCheckBox.GetAttribute("aria-checked"));
        }

        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_HashSet_ReferencesInstance()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalHashSet = new HashSet<int>();
            var capturedValue = (HashSet<int>)null;

            var component = DropDownWithReferenceCollection<HashSet<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalHashSet);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<HashSet<int>>(this, value => capturedValue = value));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select first item
            items[0].Click();
            component.Render();

            // Verify the same HashSet instance is Referenced
            Assert.Same(originalHashSet, capturedValue);
            
            // Verify the item was added correctly
            Assert.Single(originalHashSet);
            Assert.Contains(1, originalHashSet);
        }

        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_HashSet_MultipleSelections()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalHashSet = new HashSet<int> { 2 }; // Pre-populate with Item 2
            var capturedValues = new List<HashSet<int>>();

            var component = DropDownWithReferenceCollection<HashSet<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalHashSet);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<HashSet<int>>(this, value => capturedValues.Add(value)));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select first item (should add to existing collection)
            items[0].Click();
            component.Render();

            // Verify the same HashSet instance is Referenced
            Assert.Single(capturedValues);
            Assert.Same(originalHashSet, capturedValues[0]);
            
            // Verify both items are now in the collection
            Assert.Equal(2, originalHashSet.Count);
            Assert.Contains(1, originalHashSet);
            Assert.Contains(2, originalHashSet);

            // Deselect second item (should remove from collection)
            items = component.FindAll(".rz-multiselect-item"); // Re-find items after render
            items[1].Click();
            component.Render();

            // Verify the same HashSet instance is still Referenced
            Assert.Equal(2, capturedValues.Count);
            Assert.Same(originalHashSet, capturedValues[1]);
            
            // Verify only first item remains
            Assert.Single(originalHashSet);
            Assert.Contains(1, originalHashSet);
            Assert.DoesNotContain(2, originalHashSet);
        }

        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_SortedSet_ReferencesInstance()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalSortedSet = new SortedSet<int>();
            var capturedValue = (SortedSet<int>)null;

            var component = DropDownWithReferenceCollection<SortedSet<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalSortedSet);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<SortedSet<int>>(this, value => capturedValue = value));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select both items
            items[0].Click();
            component.Render();
            items = component.FindAll(".rz-multiselect-item"); // Re-find items after first click
            items[1].Click();
            component.Render();

            // Verify the same SortedSet instance is Referenced
            Assert.Same(originalSortedSet, capturedValue);
            
            // Verify items are sorted correctly
            Assert.Equal(2, originalSortedSet.Count);
            var sortedItems = originalSortedSet.ToList();
            Assert.Equal(1, sortedItems[0]);
            Assert.Equal(2, sortedItems[1]);
        }

        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_CustomCollection_ReferencesInstance()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalCollection = new CustomCollection<int>();
            var capturedValue = (CustomCollection<int>)null;

            var component = DropDownWithReferenceCollection<CustomCollection<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalCollection);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<CustomCollection<int>>(this, value => capturedValue = value));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select first item
            items[0].Click();
            component.Render();

            // Verify the same custom collection instance is Referenced
            Assert.Same(originalCollection, capturedValue);
            
            // Verify the item was added correctly
            Assert.Single(originalCollection);
            Assert.Contains(1, originalCollection);
        }


        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_List_ReferencesInstance()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalList = new List<int>();
            var capturedValue = (List<int>)null;

            var component = DropDownWithReferenceCollection<List<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalList);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<List<int>>(this, value => capturedValue = value));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select first item
            items[0].Click();
            component.Render();

            // For List<T>, it should now Reference the instance since we removed the IList exclusion
            // Arrays are now excluded instead
            Assert.Same(originalList, capturedValue);
            
            // And the content should be correct
            Assert.Single(capturedValue);
            Assert.Contains(1, capturedValue);
        }

        [Fact]
        public void DropDown_ReferenceGenericCollectionAssignment_DisabledByDefault()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalList = new List<int>();
            var capturedValue = (List<int>)null;

            var component = DropDown<List<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalList);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<List<int>>(this, value => capturedValue = value));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            var items = component.FindAll(".rz-multiselect-item");
            
            // Select first item
            items[0].Click();
            component.Render();

            // When ReferenceCollectionOnSelection is false (default), a new instance should be created
            Assert.NotSame(originalList, capturedValue);
            
            // But the content should still be correct
            Assert.Single(capturedValue);
            Assert.Contains(1, capturedValue);
        }

        [Fact]
        public void DropDown_Reset_PreservesCollectionInstanceButClears()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalHashSet = new HashSet<int> { 1, 2 }; // Pre-populate
            var capturedValues = new List<HashSet<int>>();

            var component = DropDownWithReferenceCollection<HashSet<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Value, originalHashSet);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<HashSet<int>>(this, value => capturedValues.Add(value)));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            // Verify initial state - collection should have 2 items
            Assert.Equal(2, originalHashSet.Count);
            Assert.Contains(1, originalHashSet);
            Assert.Contains(2, originalHashSet);

            // Call Reset (public method that calls ClearAll internally)
            component.InvokeAsync(() => component.Instance.Reset());
            component.Render();

            // Verify the same HashSet instance is preserved
            Assert.Single(capturedValues);
            Assert.Same(originalHashSet, capturedValues[0]);
            
            // Verify the collection is now cleared
            Assert.Empty(originalHashSet);
        }

        [Fact]
        public void DropDown_SelectAll_PreservesCollectionInstanceAndPopulates()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var originalHashSet = new HashSet<int>(); // Start empty
            var capturedValues = new List<HashSet<int>>();

            var component = DropDownWithReferenceCollection<HashSet<int>>(ctx, parameters =>
            {
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.AllowSelectAll, true);
                parameters.Add(p => p.Value, originalHashSet);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<HashSet<int>>(this, value => capturedValues.Add(value)));
                parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
            });

            // Verify initial state - collection should be empty
            Assert.Empty(originalHashSet);

            // Find and click the "Select All" checkbox
            var selectAllCheckBox = component.Find(".rz-multiselect-header input[type='checkbox']");
            selectAllCheckBox.Click();
            component.Render();

            // Verify the same HashSet instance is preserved
            Assert.Single(capturedValues);
            Assert.Same(originalHashSet, capturedValues[0]);
            
            // Verify the collection now contains both items
            Assert.Equal(2, originalHashSet.Count);
            Assert.Contains(1, originalHashSet);
            Assert.Contains(2, originalHashSet);
        }

        class ReferenceCollectionDropDown<T> : Radzen.Blazor.RadzenDropDown<T>
        {
            protected override void OnInitialized()
            {
                PreserveCollectionOnSelection = true;
                base.OnInitialized();
            }
        }

        private static IRenderedComponent<ReferenceCollectionDropDown<T>> DropDownWithReferenceCollection<T>(TestContext ctx, Action<ComponentParameterCollectionBuilder<ReferenceCollectionDropDown<T>>> configure = null)
        {
            var data = new[] {
                new DataItem { Text = "Item 1", Id = 1 },
                new DataItem { Text = "Item 2", Id = 2 },
            };

            var component = ctx.RenderComponent<ReferenceCollectionDropDown<T>>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.TextProperty, nameof(DataItem.Text));

                if (configure != null)
                {
                    configure.Invoke(parameters);
                }
                else
                {
                    parameters.Add(p => p.ValueProperty, nameof(DataItem.Id));
                }
            });

            return component;
        }

        class DataItemComparer : IEqualityComparer<DataItem>, IEqualityComparer<object>
        {
            public bool Equals(DataItem x, DataItem y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null) return false;
                if (y is null) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Text == y.Text;
            }

            public int GetHashCode(DataItem obj)
            {
                return obj.Text.GetHashCode();
            }

            public new bool Equals(object x, object y)
            {
                return Equals((DataItem)x, (DataItem)y);
            }

            public int GetHashCode(object obj)
            {
                return GetHashCode((DataItem)obj);

            }
        }

        class CustomCollection<T> : ICollection<T>
        {
            private readonly List<T> _items = new();

            public int Count => _items.Count;
            public bool IsReadOnly => false;

            public void Add(T item) => _items.Add(item);
            public void Clear() => _items.Clear();
            public bool Contains(T item) => _items.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public bool Remove(T item) => _items.Remove(item);
            public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
