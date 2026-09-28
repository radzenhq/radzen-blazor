using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;

namespace Radzen.Blazor.Tests
{
    public static class ODataParser
    {
        private static readonly IEdmModel Model = ODataServer.Model();

        public static void AssertParses(string entitySet, string option)
        {
            var separator = option.IndexOf('=');
            AssertQueryParses(entitySet, $"{option[..separator]}={Uri.EscapeDataString(option[(separator + 1)..])}");
        }

        public static void AssertQueryParses(string entitySet, string query)
        {
            var parser = new ODataUriParser(Model, new Uri("http://localhost/odata/"), new Uri($"http://localhost/odata/{entitySet}?{query}"));

            parser.ParseApply();
            parser.ParseFilter();
            parser.ParseSelectAndExpand();
            parser.ParseOrderBy();
            parser.ParseSkip();
            parser.ParseTop();
            parser.ParseCount();
        }
    }
}
