namespace Radzen.Blazor
{
    /// <summary>
    /// The <c>AutomCompleteType</c> is a string-associated enum of
    /// browser-supported autocomplete attribute values.
    /// </summary>
    /// <remarks>
    /// This class lists the autocomplete attirbute options allowing
    /// developers to provide the browser with guidance on how to pre-populate
    /// the form fields. It is a class rather than a simpler enum to associate
    /// each option with the string the browser expects. For more information
    /// please review the list of options (https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete)
    /// and the spec (https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill).
    /// </remarks>
    public enum AutoCompleteType
    {
        /// <summary>Autocomplete is disabled. </summary>
        Off,
        /// <summary>Autocomplete is enabled. The browser chooses what values to suggest. </summary>
        On,
        /// <summary>The field expects the value to be a person's full name.</summary>
        Name,
        /// <summary>The prefix or title, such as "Mrs.", "Mr.", "Miss", "Ms.", "Dr." etc.</summary>
        HonorificPrefix,
        /// <summary>The given (or "first") name.</summary>
        GivenName,
        /// <summary>The middle name.</summary>
        AdditionalName,
        /// <summary>The family (or "last") name.</summary>
        FamilyName,
        /// <summary>The suffix, such as "Jr.", "B.Sc.", "PhD.", "MBASW", etc.</summary>
        HonorificSuffix,
        /// <summary>The nickname or handle.</summary>
        Nickname,
        /// <summary>The email address.</summary>
        Email,
        /// <summary>The username or account name.</summary>
        Username,
        /// <summary>A new password. When creating a new account or changing passwords.</summary>
        NewPassword,
        /// <summary>A current password. When filling in an existing password.</summary>
        CurrentPassword,
        /// <summary>A one-time code used for verifying user identity.</summary>
        OneTimeCode,
        /// <summary>A job title, or the title a person has within an organization, such as "Senior Technical Writer", "President", or "Assistant Troop Leader".</summary>
        OrganizationTitle,
        /// <summary>A company, business, or organization name.</summary>
        Organization,
        /// <summary>A street address. Use multiple address lines when more space is needed.</summary>
        StreetAddress,
        /// <summary>The line 1 of a street address. For example, "1234 Main Street".</summary>
        AddressLine1,
        /// <summary>The line 2 of a street address. For example, "Apartment 123".</summary>
        AddressLine2,
        /// <summary>The line 3 of a street address. For example, "c/o Jane Doe".</summary>
        AddressLine3,
        /// <summary>The city or locality.</summary>
        AddressLevel1,
        /// <summary>The state, province, prefecture, or region.</summary>
        AddressLevel2,
        /// <summary>The zip code or postal code.</summary>
        AddressLevel3,
        /// <summary>The country name.</summary>
        AddressLevel4,
        /// <summary>The country code.</summary>
        Country,
        /// <summary>The country name.</summary>
        CountryName,
        /// <summary>The postal code.</summary>
        PostalCode,
        /// <summary>The full name as printed on or associated with a payment instrument such as a credit card.</summary>
        CcName,
        /// <summary>The given (or "first") name as printed on or associated with a payment instrument such as a credit card.</summary>
        CcGivenName,
        /// <summary>The middle name as printed on or associated with a payment instrument such as a credit card.</summary>
        CcAdditionalName,
        /// <summary>The family (or "last") name as printed on or associated with a payment instrument such as a credit card.</summary>
        CcFamilyName,
        /// <summary>A credit card number or other number identifying a payment method, such as an account number.</summary>
        CcNumber,
        /// <summary>A payment method expiration date, typically in the form "MM/YY" or "MM/YYYY".</summary>
        CcExp,
        /// <summary>A payment method expiration month, typically in numeric form (MM).</summary>
        CcExpMonth,
        /// <summary>A payment method expiration year, typically in numeric form (YYYY).</summary>
        CcExpYear,
        /// <summary>The security code for your payment method, such as the CVV code.</summary>
        CcCsc,
        /// <summary>The type of payment instrument, such as "Visa", "Master Card", "Checking", or "Savings".</summary>
        CcType,
        /// <summary>The currency in which the transaction was completed. Use the ISO 4217 currency codes, such as "USD" for the US dollar.</summary>
        TransactionCurrency,
        /// <summary>The amount, in the currency specified by the transaction currency attribute, of the transaction completed.</summary>
        TransactionAmount,
        /// <summary>The language in which the transaction was completed. Use the relevant BCP 47 language tag.</summary>
        Language,
        /// <summary>A birth date, as a full date.</summary>
        Bday,
        /// <summary>The day of the month of a birth date.</summary>
        BdayDay,
        /// <summary>The month of the year of a birth date.</summary>
        BdayMonth,
        /// <summary>The year of a birth date.</summary>
        BdayYear,
        /// <summary>A gender identity (such as "Female", "Fa'afafine", "Hijra", "Male", "Nonbinary"), as freeform text without newlines.</summary>
        Sex,
        /// <summary>A full telephone number, including the country code. </summary>
        Tel,
        /// <summary>A country code, such as "1" for the United States, Canada, and other areas in North America and parts of the Caribbean.</summary>
        TelCountryCode,
        /// <summary>The entire phone number without the country code component, including a country-internal prefix.</summary>
        TelNational,
        /// <summary>The area code, with any country-internal prefix applied if appropriate.</summary>
        TelAreaCode,
        /// <summary>The phone number without the country or area code.</summary>
        TelLocal,
        /// <summary>The extension number, if applicable.</summary>
        TelExtension,
        /// <summary>A URL for an instant messaging protocol endpoint, such as "xmpp:username@example.net".</summary>
        Impp,
        /// <summary>A URL, such as a home page or company website address as appropriate given the context of the other fields in the form.</summary>
        Url,
        /// <summary>The URL of an image representing the person, company, or contact information given in the other fields in the form.</summary>
        Photo,
        /// <summary>State.</summary>
        State,
        /// <summary>Province.</summary>
        Province,
        /// <summary>Zip code.</summary>
        ZipCode,
        /// <summary>Firs name.</summary>
        FirstName,
        /// <summary>Middle name.</summary>
        MiddleName,
        /// <summary>Last name.</summary>
        LastName,
    }
}
