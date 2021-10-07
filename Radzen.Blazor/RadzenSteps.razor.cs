using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenSteps : RadzenComponent
    {
        [Parameter]
        public bool ShowStepsButtons { get; set; } = true;

        [CascadingParameter]
        public EditContext EditContext { get; set; }

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

        [Parameter]
        public EventCallback<int> SelectedIndexChanged { get; set; }

        [Parameter]
        public EventCallback<int> Change { get; set; }

        private string _nextStep = "Next";
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

        [Parameter]
        public RenderFragment Steps { get; set; }

        List<RadzenStepsItem> steps = new List<RadzenStepsItem>();

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

        protected override string GetComponentCssClass()
        {
            return "rz-steps";
        }

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

        public override void Dispose()
        {
            base.Dispose();
            steps.Clear();
        }
    }
}