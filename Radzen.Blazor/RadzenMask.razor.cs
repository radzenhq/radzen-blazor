using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// A masked text input component that enforces a specific format pattern as users type (e.g., phone numbers, dates, credit cards).
    /// RadzenMask guides users to enter data in the correct format by automatically formatting input according to a mask pattern.
    /// Uses a pattern string where asterisks (*) represent user-input positions and other characters are literals. As users type, the input is automatically formatted to match the mask.
    /// Features mask pattern definition using * for input positions (e.g., "(***) ***-****" for phone numbers), character filtering via Pattern (regex) to remove invalid characters and CharacterPattern (regex) to specify valid characters,
    /// automatic insertion of literal characters (parentheses, dashes, spaces, etc.), and placeholder showing the expected format to guide users.
    /// Common uses include phone numbers, dates, credit cards, SSN, postal codes, or any fixed-format data entry. The mask helps prevent input errors and improves data consistency.
    /// </summary>
    /// <example>
    /// Phone number mask:
    /// <code>
    /// &lt;RadzenMask Mask="(***) ***-****" Pattern="[^0-9]" Placeholder="(000) 000-0000" @bind-Value=@phoneNumber /&gt;
    /// </code>
    /// Date mask:
    /// <code>
    /// &lt;RadzenMask Mask="**/**/****" CharacterPattern="[0-9]" Placeholder="MM/DD/YYYY" @bind-Value=@dateString /&gt;
    /// </code>
    /// Credit card mask:
    /// <code>
    /// &lt;RadzenMask Mask="**** **** **** ****" Pattern="[^0-9]" Placeholder="0000 0000 0000 0000" @bind-Value=@cardNumber /&gt;
    /// </code>
    /// </example>
    public partial class RadzenMask : FormComponentWithAutoComplete<string>
    {
        /// <summary>
        /// Gets or sets whether the masked input is read-only and cannot be edited.
        /// When true, displays the formatted value but prevents user input.
        /// </summary>
        /// <value><c>true</c> if the input is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of characters that can be entered.
        /// Typically matches the mask length, but can be set for additional constraints.
        /// </summary>
        /// <value>The maximum character length, or null for no limit beyond the mask. Default is null.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the mask pattern that defines the input format.
        /// Use asterisks (*) for user input positions and literal characters for formatting.
        /// Example: "(***) ***-****" creates a phone number format like "(555) 123-4567".
        /// </summary>
        /// <value>The mask pattern string where * represents input positions.</value>
        [Parameter]
        public string Mask { get; set; }

        /// <summary>
        /// Gets or sets a regular expression pattern for removing invalid characters from user input.
        /// Characters matching this pattern are stripped out as the user types.
        /// Example: "[^0-9]" removes all non-digit characters for numeric-only input.
        /// </summary>
        /// <value>The regex pattern for invalid characters to remove.</value>
        [Parameter]
        public string Pattern { get; set; }

        /// <summary>
        /// Gets or sets a regular expression pattern specifying which characters are valid for user input.
        /// Only characters matching this pattern are accepted as the user types.
        /// If both <see cref="Pattern"/> and CharacterPattern are set, CharacterPattern takes precedence.
        /// Example: "[0-9]" allows only digit characters.
        /// </summary>
        /// <value>The regex pattern for valid input characters.</value>
        [Parameter]
        public string CharacterPattern { get; set; }

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }

        /// <inheritdoc />
        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JSRuntime.InvokeVoidAsync("eval", $"Radzen.mask('{GetId()}', '{Mask}', '{Pattern}', '{CharacterPattern}')");
            }
        }
    }
}
