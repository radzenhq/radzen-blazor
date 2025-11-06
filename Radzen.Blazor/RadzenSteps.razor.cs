using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A wizard-style steps component that guides users through a multi-step process with numbered navigation.
    /// RadzenSteps displays a visual progress indicator and manages sequential navigation through each step, ideal for forms, checkout flows, or setup wizards.
    /// Provides a structured way to break complex processes into manageable sequential stages.
    /// Features numbered circles showing current/completed/upcoming steps for visual progress, Next/Previous buttons for moving between steps or clicking on step numbers,
    /// optional form validation integration to prevent advancing with invalid data, CanChange event to control when users can move between steps,
    /// navigation to specific steps via SelectedIndex binding, and optional built-in Next/Previous buttons or use your own custom navigation.
    /// Each step is defined using RadzenStepsItem components. Use the CanChange event to validate data before allowing step transitions. Integrates with Blazor EditContext for form validation.
    /// </summary>
    /// <example>
    /// Basic wizard with steps:
    /// <code>
    /// &lt;RadzenSteps @bind-SelectedIndex=@currentStep&gt;
    ///     &lt;Steps&gt;
    ///         &lt;RadzenStepsItem Text="Personal Info"&gt;
    ///             &lt;RadzenTextBox @bind-Value=@name Placeholder="Name" /&gt;
    ///             &lt;RadzenTextBox @bind-Value=@email Placeholder="Email" /&gt;
    ///         &lt;/RadzenStepsItem&gt;
    ///         &lt;RadzenStepsItem Text="Address"&gt;
    ///             &lt;RadzenTextBox @bind-Value=@street Placeholder="Street" /&gt;
    ///             &lt;RadzenTextBox @bind-Value=@city Placeholder="City" /&gt;
    ///         &lt;/RadzenStepsItem&gt;
    ///         &lt;RadzenStepsItem Text="Review"&gt;
    ///             Review and submit...
    ///         &lt;/RadzenStepsItem&gt;
    ///     &lt;/Steps&gt;
    /// &lt;/RadzenSteps&gt;
    /// </code>
    /// Steps with validation and custom buttons:
    /// <code>
    /// &lt;RadzenSteps ShowStepsButtons="false" CanChange=@OnCanChange&gt;
    ///     &lt;Steps&gt;
    ///         &lt;RadzenStepsItem Text="Step 1"&gt;Content...&lt;/RadzenStepsItem&gt;
    ///         &lt;RadzenStepsItem Text="Step 2"&gt;Content...&lt;/RadzenStepsItem&gt;
    ///     &lt;/Steps&gt;
    /// &lt;/RadzenSteps&gt;
    /// &lt;RadzenStack Orientation="Orientation.Horizontal" Gap="1rem"&gt;
    ///     &lt;RadzenButton Text="Previous" Click=@PrevStep /&gt;
    ///     &lt;RadzenButton Text="Next" Click=@NextStep /&gt;
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// </example>
    public partial class RadzenSteps : RadzenComponent
    {
        /// <summary>
        /// Gets or sets whether to display the built-in Next and Previous navigation buttons below the step content.
        /// When false, you must provide your own navigation buttons using NextStep() and PrevStep() methods.
        /// </summary>
        /// <value><c>true</c> to show built-in navigation buttons; <c>false</c> to use custom navigation. Default is <c>true</c>.</value>
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
        public IList<RadzenStepsItem> StepsCollection { get => steps; }

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
        /// Programmatically navigates to the next visible step in the sequence.
        /// If already at the last step, this method does nothing. Respects CanChange validation.
        /// </summary>
        /// <returns>A task representing the asynchronous navigation operation.</returns>
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
        /// Programmatically navigates to the previous visible step in the sequence.
        /// If already at the first step, this method does nothing. Respects CanChange validation.
        /// </summary>
        /// <returns>A task representing the asynchronous navigation operation.</returns>
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

        private int selectedIndex = 0;
        /// <summary>
        /// Gets or sets the selected index.
        /// </summary>
        /// <value>The selected index.</value>
        [Parameter]
        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
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

        /// <summary>
        /// A callback that will be invoked when the user tries to change the step.
        /// Invoke the <see cref="StepsCanChangeEventArgs.PreventDefault"/> method to prevent this change.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenSteps CanChange=@OnCanChange&gt;
        /// &lt;/RadzenSteps&gt;
        /// @code {
        ///  void OnCanChange(RadzenStepsCanChangeEventArgs args)
        ///  {
        ///     args.PreventDefault();
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<StepsCanChangeEventArgs> CanChange { get; set; }

        private string nextStep = "Next";
        /// <summary>
        /// Gets or sets the next button text.
        /// </summary>
        /// <value>The next button text.</value>
        [Parameter]
        public string NextText
        {
            get
            {
                return StepsCollection.ElementAtOrDefault(SelectedIndex)?.NextText ?? nextStep;
            }
            set
            {
                if (value != nextStep)
                {
                    nextStep = value;

                    Refresh();
                }
            }
        }

        private string previousText = "Previous";
        /// <summary>
        /// Gets or sets the previous button text.
        /// </summary>
        /// <value>The previous button text.</value>
        [Parameter]
        public string PreviousText
        {
            get
            {
                return StepsCollection.ElementAtOrDefault(SelectedIndex)?.PreviousText ?? previousText;
            }
            set
            {
                if (value != previousText)
                {
                    previousText = value;

                    Refresh();
                }
            }
        }

        private string nextTitle = "Go to the next step.";
        /// <summary>
        /// Gets or sets the next button title attribute.
        /// </summary>
        /// <value>The next button title attribute.</value>
        [Parameter]
        public string NextTitle
        {
            get
            {
                return StepsCollection.ElementAtOrDefault(SelectedIndex)?.NextTitle ?? nextTitle;
            }
            set
            {
                if (value != nextTitle)
                {
                    nextTitle = value;
                    Refresh();
                }
            }
        }

        private string previousTitle = "Go to the previous step.";
        /// <summary>
        /// Gets or sets the previous button title attribute.
        /// </summary>
        /// <value>The previous button title attribute.</value>
        [Parameter]
        public string PreviousTitle
        {
            get
            {
                return StepsCollection.ElementAtOrDefault(SelectedIndex)?.PreviousTitle ?? previousTitle;
            }
            set
            {
                if (value != previousTitle)
                {
                    previousTitle = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Gets the next button aria-label attribute.
        /// </summary>
        /// <value>The next button aria-label attribute.</value>
        public string NextAriaLabel => StepsCollection.ElementAtOrDefault(SelectedIndex)?.NextAriaLabel;

        /// <summary>
        /// Gets the previous button aria-label attribute.
        /// </summary>
        /// <value>The previous button aria-label attribute.</value>
        public string PreviousAriaLabel => StepsCollection.ElementAtOrDefault(SelectedIndex)?.PreviousAriaLabel;

        /// <summary>
        /// Gets or sets the steps.
        /// </summary>
        /// <value>The steps.</value>
        [Parameter]
        public RenderFragment Steps { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <value><c>true</c> user can jump to any step if enabled; <c>false</c> user can change steps only with step buttons (previous/next).</value>
        [Parameter]
        public bool AllowStepSelect { get; set; } = true;

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

                if (!disposed)
                {
                    try { InvokeAsync(StateHasChanged); } catch { }
                }
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
            var newIndex = steps.IndexOf(step);

            var canChangeArgs = new StepsCanChangeEventArgs { SelectedIndex = SelectedIndex, NewIndex = newIndex };

            await CanChange.InvokeAsync(canChangeArgs);

            if (canChangeArgs.IsDefaultPrevented)
            {
                return;
            }

            var valid = true;

            if (EditContext != null)
            {
                valid = EditContext.Validate();
            }

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

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-steps";
        }

        /// <inheritdoc />
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

        bool preventKeyPress = false;
        async Task OnKeyPress(KeyboardEventArgs args, Task task)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                await task;
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
