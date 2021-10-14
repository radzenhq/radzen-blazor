using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSteps component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenSteps : RadzenComponent
    {
        /// <summary>
        /// Gets or sets a value indicating whether to show steps buttons.
        /// </summary>
        /// <value><c>true</c> if steps buttons are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowStepsButtons { get; set; } = true;

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Gets the steps collection.
        /// </summary>
        /// <value>The steps collection.</value>
        public IList<RadzenStepsItem> StepsCollection
        {
            get
            {
                return steps;
            }
        }

        bool IsFirstVisibleStep()
        {
            var firstVisibleStep = steps.Where(s => s.Visible).FirstOrDefault();
            if (firstVisibleStep != null)
            {
                return steps.IndexOf(firstVisibleStep) == SelectedIndex;
            }

            return false;
        }

        bool IsLastVisibleStep()
        {
            var lastVisibleStep = steps.Where(s => s.Visible).LastOrDefault();
            if (lastVisibleStep != null)
            {
                return steps.IndexOf(lastVisibleStep) == SelectedIndex;
            }

            return false;
        }

        /// <summary>
        /// Goes to next step.
        /// </summary>
        public async System.Threading.Tasks.Task NextStep()
        {
            if (!IsLastVisibleStep())
            {
                var nextIndex = SelectedIndex + 1;
                while (nextIndex < steps.Count)
                {
                    if (!steps[nextIndex].Visible)
                    {
                        nextIndex++;
                        continue;
                    }

                    break;
                }

                await SelectStepFromIndex(nextIndex);
            }
        }

        /// <summary>
        /// Goes to previous step.
        /// </summary>
        public async System.Threading.Tasks.Task PrevStep()
        {
            if (!IsFirstVisibleStep())
            {
                var prevIndex = SelectedIndex - 1;
                while (prevIndex >= 0)
                {
                    if (!steps[prevIndex].Visible)
                    {
                        prevIndex--;
                        continue;
                    }

                    break;
                }

                await SelectStepFromIndex(prevIndex);
            }
        }

        async System.Threading.Tasks.Task SelectStepFromIndex(int index)
        {
            if (index >= 0 && index < steps.Count)
            {
                var stepToSelect = steps[index];

                if (stepToSelect != null && !stepToSelect.Disabled)
                {
                    await SelectStep(stepToSelect, true);
                }
            }
        }

        int _selectedIndex = 0;
        /// <summary>
        /// Gets or sets the selected index.
        /// </summary>
        /// <value>The selected index.</value>
        [Parameter]
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected index changed callback.
        /// </summary>
        /// <value>The selected index changed callback.</value>
        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<int> Change { get; set; }

        private string _nextStep = "Next";

        /// <summary>
        /// Gets or sets the next button text.
        /// </summary>
        /// <value>The next button text.</value>
        [Parameter]
        public string NextText
        {
            get { return _nextStep; }
            set
            {
                if (value != _nextStep)
                {
                    _nextStep = value;

                    Refresh();
                }
            }
        }

        private string _previousText = "Previous";
        /// <summary>
        /// Gets or sets the previous button text.
        /// </summary>
        /// <value>The previous button text.</value>
        [Parameter]
        public string PreviousText
        {
            get { return _previousText; }
            set
            {
                if (value != _previousText)
                {
                    _previousText = value;

                    Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the steps.
        /// </summary>
        /// <value>The steps.</value>
        [Parameter]
        public RenderFragment Steps { get; set; }

        List<RadzenStepsItem> steps = new List<RadzenStepsItem>();

        /// <summary>
        /// Adds the step.
        /// </summary>
        /// <param name="step">The step.</param>
        public void AddStep(RadzenStepsItem step)
        {
            if (steps.IndexOf(step) == -1)
            {
                if (step.Selected)
                {
                    SelectedIndex = steps.Count;
                }

                steps.Add(step);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Removes the step.
        /// </summary>
        /// <param name="item">The item.</param>
        public void RemoveStep(RadzenStepsItem item)
        {
            if (steps.Contains(item))
            {
                steps.Remove(item);

                StateHasChanged();
            }
        }

        internal void Refresh()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Determines whether the specified index is selected.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="step">The step.</param>
        /// <returns><c>true</c> if the specified index is selected; otherwise, <c>false</c>.</returns>
        protected bool IsSelected(int index, RadzenStepsItem step)
        {
            return SelectedIndex == index;
        }

        internal async System.Threading.Tasks.Task SelectStep(RadzenStepsItem step, bool raiseChange = false)
        {
            var valid = true;

            if (EditContext != null)
            {
                valid = EditContext.Validate();
            }

            var newIndex = steps.IndexOf(step);

            if (valid || newIndex < SelectedIndex)
            {
                SelectedIndex = newIndex;

                if (raiseChange)
                {
                    await Change.InvokeAsync(SelectedIndex);
                    await SelectedIndexChanged.InvokeAsync(SelectedIndex);
                    StateHasChanged();
                }
            }
        }

        internal void SelectFirst()
        {
            SelectedIndex = 0;
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-steps";
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var selectedIndexChanged = parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex);
            if (selectedIndexChanged)
            {
                if (SelectedIndex >= 0 && SelectedIndex < steps.Count)
                {
                    var stepToSelect = steps[SelectedIndex];

                    if (stepToSelect != null)
                    {
                        await SelectStep(stepToSelect);
                    }
                }
            }

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            steps.Clear();
        }
    }
}