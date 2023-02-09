namespace Radzen.Blazor
{
    /// <summary>
    /// The <c>ACG</c> is a string-associated enum of browser-supported
    /// autocomplete attribute values.
    /// </summary>
    /// <remarks>
    /// <c>ACG</c> stands for Auto Complete Guidance. An initialism is used for
    /// brevity. This class lists the autocomplete attirbute options allowing
    /// developers to provide the browser with guidance on how to pre-populate
    /// the form fields. It is a class rather than a simpler enum to associate
    /// each option with the string the browser expects. For more information
    /// please review the list of options (https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete)
    /// and the spec (https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill).
    /// </remarks>
    public static class ACG
	{
        // Options
		public static string Off { get => "off"; }
        public static string On { get => "on"; }
        public static string Name { get => "name"; }
        public static string HonorificPrefix { get => "honorific-prefix"; }
        public static string GivenName { get => "given-name"; }
        public static string AdditionalName { get => "additional-name"; }
        public static string FamilyName { get => "family-name"; }
        public static string HonorificSuffix { get => "honorific-suffix"; }
        public static string NickName { get => "nickname"; }
        public static string Email { get => "email"; }
        public static string Username { get => "username"; }
        public static string NewPassword { get => "new-password"; }
        public static string CurrentPassword { get => "current-password"; }
        public static string OneTimeCode { get => "one-time-code"; }
        public static string OrganizationTitle { get => "organization-title"; }
        public static string Organization { get => "organization"; }
        public static string StreetAddress { get => "street-address"; }
        public static string AddressLine1 { get => "address-line1"; }
        public static string AddressLine2 { get => "address-line2"; }
        public static string AddressLine3 { get => "address-line3"; }
        public static string AddressLevel1 { get => "address-level1"; }
        public static string AddressLevel2 { get => "address-level2"; }
        public static string AddressLevel3 { get => "address-level3"; }
        public static string AddressLevel4 { get => "address-level4"; }
        public static string Country { get => "country"; }
        public static string CountryName { get => "country-name"; }
        public static string PostalCode { get => "postal-code"; }
        public static string CcName { get => "cc-name"; }
        public static string CcGivenName { get => "cc-given-name"; }
        public static string CcAdditionalName { get => "cc-additional-name"; }
        public static string CcFamilyName { get => "cc-family-name"; }
        public static string CcNumber { get => "cc-number"; }
        public static string CcExp { get => "cc-exp"; }
        public static string CcExpMonth { get => "cc-exp-month"; }
        public static string CcExpYear { get => "cc-exp-year"; }
        public static string CcCsc { get => "cc-csc"; }
        public static string CcType { get => "cc-type"; }
        public static string TransactionCurrency { get => "transaction-currency"; }
        public static string TransactionAmount { get => "transaction-amount"; }
        public static string Language { get => "language"; }
        public static string BDay { get => "bday"; }
        public static string BDayDay { get => "bday-day"; }
        public static string BDayMonth { get => "bday-month"; }
        public static string BdayYear { get => "bday-year"; }
        public static string Sex { get => "sex"; }
        public static string Tel { get => "tel"; }
        public static string TelCountryCode { get => "tel-country-code"; }
        public static string TelNational { get => "tel-national"; }
        public static string TelAreaCode { get => "tel-area-code"; }
        public static string TelLocal { get => "tel-local"; }
        public static string TelExtension { get => "tel-extension"; }
        public static string Impp { get => "impp"; }
        public static string Url { get => "url"; }
        public static string Photo { get => "photo"; }

        // Synonyms
        public static string State { get => AddressLevel1; }
        public static string Province { get => AddressLevel1; }
        public static string ZipCode { get => PostalCode; }
        public static string FirstName { get => GivenName; }
        public static string MiddleName { get => AdditionalName; }
        public static string LastName { get => FamilyName; }
    }
}