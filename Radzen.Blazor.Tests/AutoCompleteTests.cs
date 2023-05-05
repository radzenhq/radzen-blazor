using Xunit;

namespace Radzen.Blazor.Tests
{
	public class AutoCompleteTests
	{
        [Fact]
        public void AutoComplete_Enum_Converts_To_Attr_Value()
        {
            // Options
            Assert.Equal("off", AutoCompleteType.Off.GetAutoCompleteValue());
            Assert.Equal("on", AutoCompleteType.On.GetAutoCompleteValue());
            Assert.Equal("name", AutoCompleteType.Name.GetAutoCompleteValue());
            Assert.Equal("honorific-prefix", AutoCompleteType.HonorificPrefix.GetAutoCompleteValue());
            Assert.Equal("given-name", AutoCompleteType.GivenName.GetAutoCompleteValue());
            Assert.Equal("additional-name", AutoCompleteType.AdditionalName.GetAutoCompleteValue());
            Assert.Equal("family-name", AutoCompleteType.FamilyName.GetAutoCompleteValue());
            Assert.Equal("honorific-suffix", AutoCompleteType.HonorificSuffix.GetAutoCompleteValue());
            Assert.Equal("nickname", AutoCompleteType.Nickname.GetAutoCompleteValue());
            Assert.Equal("email", AutoCompleteType.Email.GetAutoCompleteValue());
            Assert.Equal("username", AutoCompleteType.Username.GetAutoCompleteValue());
            Assert.Equal("new-password", AutoCompleteType.NewPassword.GetAutoCompleteValue());
            Assert.Equal("current-password", AutoCompleteType.CurrentPassword.GetAutoCompleteValue());
            Assert.Equal("one-time-code", AutoCompleteType.OneTimeCode.GetAutoCompleteValue());
            Assert.Equal("organization-title", AutoCompleteType.OrganizationTitle.GetAutoCompleteValue());
            Assert.Equal("organization", AutoCompleteType.Organization.GetAutoCompleteValue());
            Assert.Equal("street-address", AutoCompleteType.StreetAddress.GetAutoCompleteValue());
            Assert.Equal("address-line1", AutoCompleteType.AddressLine1.GetAutoCompleteValue());
            Assert.Equal("address-line2", AutoCompleteType.AddressLine2.GetAutoCompleteValue());
            Assert.Equal("address-line3", AutoCompleteType.AddressLine3.GetAutoCompleteValue());
            Assert.Equal("address-level1", AutoCompleteType.AddressLevel1.GetAutoCompleteValue());
            Assert.Equal("address-level2", AutoCompleteType.AddressLevel2.GetAutoCompleteValue());
            Assert.Equal("address-level3", AutoCompleteType.AddressLevel3.GetAutoCompleteValue());
            Assert.Equal("address-level4", AutoCompleteType.AddressLevel4.GetAutoCompleteValue());
            Assert.Equal("country", AutoCompleteType.Country.GetAutoCompleteValue());
            Assert.Equal("country-name", AutoCompleteType.CountryName.GetAutoCompleteValue());
            Assert.Equal("postal-code", AutoCompleteType.PostalCode.GetAutoCompleteValue());
            Assert.Equal("cc-name", AutoCompleteType.CcName.GetAutoCompleteValue());
            Assert.Equal("cc-given-name", AutoCompleteType.CcGivenName.GetAutoCompleteValue());
            Assert.Equal("cc-additional-name", AutoCompleteType.CcAdditionalName.GetAutoCompleteValue());
            Assert.Equal("cc-family-name", AutoCompleteType.CcFamilyName.GetAutoCompleteValue());
            Assert.Equal("cc-number", AutoCompleteType.CcNumber.GetAutoCompleteValue());
            Assert.Equal("cc-exp", AutoCompleteType.CcExp.GetAutoCompleteValue());
            Assert.Equal("cc-exp-month", AutoCompleteType.CcExpMonth.GetAutoCompleteValue());
            Assert.Equal("cc-exp-year", AutoCompleteType.CcExpYear.GetAutoCompleteValue());
            Assert.Equal("cc-csc", AutoCompleteType.CcCsc.GetAutoCompleteValue());
            Assert.Equal("cc-type", AutoCompleteType.CcType.GetAutoCompleteValue());
            Assert.Equal("transaction-currency", AutoCompleteType.TransactionCurrency.GetAutoCompleteValue());
            Assert.Equal("transaction-amount", AutoCompleteType.TransactionAmount.GetAutoCompleteValue());
            Assert.Equal("language", AutoCompleteType.Language.GetAutoCompleteValue());
            Assert.Equal("bday", AutoCompleteType.Bday.GetAutoCompleteValue());
            Assert.Equal("bday-day", AutoCompleteType.BdayDay.GetAutoCompleteValue());
            Assert.Equal("bday-month", AutoCompleteType.BdayMonth.GetAutoCompleteValue());
            Assert.Equal("bday-year", AutoCompleteType.BdayYear.GetAutoCompleteValue());
            Assert.Equal("sex", AutoCompleteType.Sex.GetAutoCompleteValue());
            Assert.Equal("tel", AutoCompleteType.Tel.GetAutoCompleteValue());
            Assert.Equal("tel-country-code", AutoCompleteType.TelCountryCode.GetAutoCompleteValue());
            Assert.Equal("tel-national", AutoCompleteType.TelNational.GetAutoCompleteValue());
            Assert.Equal("tel-area-code", AutoCompleteType.TelAreaCode.GetAutoCompleteValue());
            Assert.Equal("tel-local", AutoCompleteType.TelLocal.GetAutoCompleteValue());
            Assert.Equal("tel-extension", AutoCompleteType.TelExtension.GetAutoCompleteValue());
            Assert.Equal("impp", AutoCompleteType.Impp.GetAutoCompleteValue());
            Assert.Equal("url", AutoCompleteType.Url.GetAutoCompleteValue());
            Assert.Equal("photo", AutoCompleteType.Photo.GetAutoCompleteValue());
            // Synonyms
            Assert.Equal("address-level1", AutoCompleteType.State.GetAutoCompleteValue());
            Assert.Equal("address-level1", AutoCompleteType.Province.GetAutoCompleteValue());
            Assert.Equal("postal-code", AutoCompleteType.ZipCode.GetAutoCompleteValue());
            Assert.Equal("given-name", AutoCompleteType.FirstName.GetAutoCompleteValue());
            Assert.Equal("additional-name", AutoCompleteType.MiddleName.GetAutoCompleteValue());
            Assert.Equal("family-name", AutoCompleteType.LastName.GetAutoCompleteValue());
        }
    }
}
