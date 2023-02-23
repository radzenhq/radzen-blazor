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
        // Options
        Off,
        On,
        Name,
        HonorificPrefix,
        GivenName,
        AdditionalName,
        FamilyName,
        HonorificSuffix,
        Nickname,
        Email,
        Username,
        NewPassword,
        CurrentPassword,
        OneTimeCode,
        OrganizationTitle,
        Organization,
        StreetAddress,
        AddressLine1,
        AddressLine2,
        AddressLine3,
        AddressLevel1,
        AddressLevel2,
        AddressLevel3,
        AddressLevel4,
        Country,
        CountryName,
        PostalCode,
        CcName,
        CcGivenName,
        CcAdditionalName,
        CcFamilyName,
        CcNumber,
        CcExp,
        CcExpMonth,
        CcExpYear,
        CcCsc,
        CcType,
        TransactionCurrency,
        TransactionAmount,
        Language,
        Bday,
        BdayDay,
        BdayMonth,
        BdayYear,
        Sex,
        Tel,
        TelCountryCode,
        TelNational,
        TelAreaCode,
        TelLocal,
        TelExtension,
        Impp,
        Url,
        Photo,

        // Synonyms
        State,
        Province,
        ZipCode,
        FirstName,
        MiddleName,
        LastName,
    }
}
