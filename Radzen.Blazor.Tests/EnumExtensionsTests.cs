using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class EnumExtensionsTests
    {
        private enum TestEnum
        {
            Plain,

            [Display(Description = "Desc Only")]
            DescriptionOnly,

            [Display(Name = "Name Only")]
            NameOnly,

            [Display(Name = "Both Name", Description = "Both Desc")]
            NameAndDescription,

            [Description("System Desc")]
            SystemDescription,

            [Display(Name = "Display Name Wins")]
            [Description("System Desc Fallback")]
            DisplayNameAndSystemDescription,
        }

        [Fact]
        public void GetDisplayDescription_NoAttributes_ReturnsEnumName()
        {
            Assert.Equal("Plain", TestEnum.Plain.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_DisplayDescriptionOnly_ReturnsDescription()
        {
            // Preserves legacy behavior.
            Assert.Equal("Desc Only", TestEnum.DescriptionOnly.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_DisplayNameOnly_ReturnsName()
        {
            // Previously returned the raw enum string; now returns Name.
            Assert.Equal("Name Only", TestEnum.NameOnly.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_DisplayNameAndDescription_PrefersDescription()
        {
            // Preserves legacy behavior: when both are set, Description wins so
            // existing consumers relying on Description are not broken.
            Assert.Equal("Both Desc", TestEnum.NameAndDescription.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_SystemDescriptionAttribute_ReturnsDescription()
        {
            Assert.Equal("System Desc", TestEnum.SystemDescription.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_DisplayNameOverridesSystemDescription()
        {
            Assert.Equal("Display Name Wins", TestEnum.DisplayNameAndSystemDescription.GetDisplayDescription());
        }

        [Fact]
        public void GetDisplayDescription_TranslationFunction_AppliedToResult()
        {
            var result = TestEnum.NameOnly.GetDisplayDescription(s => s.ToUpperInvariant());
            Assert.Equal("NAME ONLY", result);
        }

        [Fact]
        public void GetDisplayDescription_TranslationFunction_AppliedToEnumNameFallback()
        {
            var result = TestEnum.Plain.GetDisplayDescription(s => "[" + s + "]");
            Assert.Equal("[Plain]", result);
        }
    }
}
