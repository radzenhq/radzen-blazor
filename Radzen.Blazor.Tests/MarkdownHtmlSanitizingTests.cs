using System.Diagnostics;
using System.Linq;
using Bunit;
using Radzen.Documents.Markdown;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MarkdownHtmlSanitizingTests
    {
        private static IRenderedComponent<RadzenMarkdown> Render(TestContext ctx, string text, string[] tags = null, string[] attributes = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, text);

                if (tags != null)
                {
                    parameters.Add(p => p.AllowedHtmlTags, tags);
                }

                if (attributes != null)
                {
                    parameters.Add(p => p.AllowedHtmlAttributes, attributes);
                }
            });
        }

        [Fact]
        public void Img_With_Slash_Before_Attributes_In_Div_Block_Renders_As_Text()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div>\n<img/src=x onerror=alert(1)>\n</div>");

            Assert.Empty(component.FindAll("[onerror]"));
            Assert.Empty(component.FindAll("img"));
            Assert.Contains("&lt;img/src=x onerror=alert(1)&gt;", component.Markup);
        }

        [Theory]
        [InlineData("<a href='x\" onclick=\"alert(1)'>y</a>")]
        [InlineData("text <a href='x\" onclick=\"alert(1)'>y</a>")]
        [InlineData("<a href='x\" onclick=\"alert(1)'>\n\ny\n\n</a>")]
        public void Single_Quoted_Attribute_With_Double_Quotes_Stays_One_Attribute(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            var anchor = Assert.Single(component.FindAll("a"));
            Assert.Equal("x\" onclick=\"alert(1)", anchor.GetAttribute("href"));
            Assert.Single(anchor.Attributes);
            Assert.Empty(component.FindAll("[onclick]"));
        }

        [Theory]
        [InlineData("<div>\n<svg/onload=alert(1)>\n</div>")]
        [InlineData("<div><svg onload=alert(1)></svg></div>")]
        public void Svg_Is_Not_Rendered(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Empty(component.FindAll("svg"));
            Assert.Empty(component.FindAll("[onload]"));
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("<script>\nalert(1)\n</script>")]
        [InlineData("<div>\n<script>alert(1)</script>\n</div>")]
        [InlineData("<style>body { display: none }</style>")]
        public void Script_And_Style_Blocks_Render_No_Element_And_No_Content(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Empty(component.FindAll("script"));
            Assert.Empty(component.FindAll("style"));
            Assert.DoesNotContain("alert", component.Markup);
            Assert.DoesNotContain("display", component.Markup);
        }

        [Theory]
        [InlineData("<a href=\"java\tscript:alert(1)\">x</a>")]
        [InlineData("<a href=\"java&#9;script:alert(1)\">x</a>")]
        [InlineData("<a href=\"&#106;avascript:alert(1)\">x</a>")]
        [InlineData("<a href=\"&#x6A;avascript&#58;alert(1)\">x</a>")]
        [InlineData("<a href=\" \u0001javascript:alert(1)\">x</a>")]
        [InlineData("<a href=\"data:text/html,<script>alert(1)</script>\">x</a>")]
        [InlineData("<a href=\"vbscript:msgbox(1)\">x</a>")]
        [InlineData("<div><a href=\"java&#9;script:alert(1)\">x</a></div>")]
        [InlineData("<div><a href=\"data:text/html;base64,PHNjcmlwdD4=\">x</a></div>")]
        [InlineData("<div><img src=\"javascript:alert(1)\"></div>")]
        [InlineData("<div><img src=\"data:text/html,x\"></div>")]
        [InlineData("[x](<java\tscript:alert(1)>)")]
        [InlineData("[x](java&#9;script:alert(1))")]
        [InlineData("[x](&#106;avascript:alert(1))")]
        [InlineData("[x](data:text/html,alert)")]
        [InlineData("[x](data:image/png;base64,AAAA)")]
        [InlineData("![x](<java\tscript:alert(1)>)")]
        [InlineData("![x](&#106;avascript:alert(1))")]
        [InlineData("![x](data:text/html,alert)")]
        public void Unsafe_Urls_Are_Dropped(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.All(component.FindAll("[href]"), element => Assert.Empty(element.GetAttribute("href")));
            Assert.All(component.FindAll("[src]"), element => Assert.Empty(element.GetAttribute("src")));
        }

        [Theory]
        [InlineData("<a href=\"https://radzen.com\">x</a>", "href", "https://radzen.com")]
        [InlineData("<a href=\"mailto:info@example.com\">x</a>", "href", "mailto:info@example.com")]
        [InlineData("<a href=\"/docs?a=1&amp;b=2\">x</a>", "href", "/docs?a=1&b=2")]
        [InlineData("<a href=\"docs/a:b\">x</a>", "href", "docs/a:b")]
        [InlineData("<div><img src=\"data:image/png;base64,AAAA\"></div>", "src", "data:image/png;base64,AAAA")]
        [InlineData("![x](data:image/png;base64,AAAA)", "src", "data:image/png;base64,AAAA")]
        [InlineData("![x](https://radzen.com/a.png)", "src", "https://radzen.com/a.png")]
        public void Safe_Urls_Are_Kept(string markdown, string attribute, string expected)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Equal(expected, component.Find($"[{attribute}]").GetAttribute(attribute));
        }

        [Theory]
        [InlineData("a </b> b")]
        [InlineData("</b>")]
        [InlineData("</div>")]
        [InlineData("a <b> b")]
        [InlineData("<b>*x</b>*")]
        [InlineData("*<b>x*</b>")]
        [InlineData("<div>\n\n*x*")]
        [InlineData("<div><span>x</div></span>")]
        [InlineData("> <div>\n\nx\n\n</div>")]
        [InlineData("- <b>x\n- y</b>")]
        [InlineData("| <b>a | b</b> |\n| - | - |\n| <i>c | d |")]
        [InlineData("# <b>x\n\ny</b>")]
        [InlineData("[<b>x](y)</b>")]
        public void Unbalanced_Tags_Render_Without_Throwing(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Contains("rz-markdown", component.Markup);
        }

        [Fact]
        public void Unclosed_Inline_Tag_Is_Closed_At_End_Of_Paragraph()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "a <b>x\n\ny");

            Assert.Equal("x", component.Find("b").TextContent);
        }

        [Fact]
        public void Closing_Tag_Inside_Emphasis_Does_Not_Close_Outer_Element()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<b>*x</b>*");

            Assert.Equal("x", component.Find("b > em").TextContent);
        }

        [Fact]
        public void Unclosed_Html_Block_Element_Is_Closed_Within_The_Block()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div class=\"note\">\n\n*x*\n\n</div>\n\nafter");

            Assert.Empty(component.Find("div.note").TextContent);
            Assert.Equal("x", component.Find("em").TextContent);
        }

        [Theory]
        [InlineData("<div style=\"position:fixed\" id=\"x\">a</div>", "div:not(.rz-markdown)")]
        [InlineData("<span style=\"color:red\" id=\"x\">a</span>", "span")]
        public void Style_And_Id_Are_Dropped_By_Default(string markdown, string tag)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            var element = component.Find(tag);
            Assert.False(element.HasAttribute("style"));
            Assert.False(element.HasAttribute("id"));
        }

        [Fact]
        public void Style_And_Id_Are_Accepted_When_Allowed()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div style=\"color:red\" id=\"x\">a</div>", attributes: ["style", "id"]);

            var element = component.Find("div#x");
            Assert.Equal("color:red", element.GetAttribute("style"));
        }

        [Theory]
        [InlineData("<form action=\"https://example.com\">\n<input name=\"password\">\n</form>")]
        [InlineData("x <form action=\"https://example.com\"><input name=\"password\"></form>")]
        public void Form_And_Input_Are_Dropped_By_Default(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Empty(component.FindAll("form"));
            Assert.Empty(component.FindAll("input"));
        }

        [Fact]
        public void Form_And_Input_Are_Accepted_When_Allowed()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<form action=\"https://example.com\">\n<input name=\"q\">\n</form>", tags: ["form", "input"], attributes: ["action", "name"]);

            Assert.Equal("https://example.com", component.Find("form").GetAttribute("action"));
            Assert.Equal("q", component.Find("form > input").GetAttribute("name"));
        }

        [Fact]
        public void Allowed_Lists_Match_Exact_Names()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div onclick=\"alert(1)\" data-x=\"1\">a</div>", tags: ["div"], attributes: ["on", "data"]);

            var element = component.Find("div:not(.rz-markdown)");
            Assert.Empty(element.Attributes);
        }

        [Fact]
        public void Entities_In_Html_Block_Text_Are_Decoded_Once()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div>\na &amp; b &lt;c&gt;\n</div>");

            Assert.Equal("a & b <c>", component.Find("div:not(.rz-markdown)").TextContent.Trim());
            Assert.DoesNotContain("&amp;amp;", component.Markup);
        }

        [Fact]
        public void Comments_And_Declarations_In_Html_Block_Are_Dropped()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "<div>\n<!-- secret --><![CDATA[x]]><?pi?>y\n</div>");

            Assert.DoesNotContain("secret", component.Markup);
            Assert.DoesNotContain("CDATA", component.Markup);
            Assert.Contains("y", component.Find("div:not(.rz-markdown)").TextContent);
        }

        [Fact]
        public void Html_Is_Rendered_As_Text_When_AllowHtml_Is_False()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenMarkdown>(parameters =>
            {
                parameters.Add(p => p.Text, "<div>\n<b>x</b>\n</div>\n\ny <i>z</i>");
                parameters.Add(p => p.AllowHtml, false);
            });

            Assert.Empty(component.FindAll("b"));
            Assert.Empty(component.FindAll("i"));
            Assert.Contains("&lt;b&gt;x&lt;/b&gt;", component.Markup);
        }

        [Theory]
        [InlineData("[c](//)")]
        [InlineData("[c](https:x)")]
        [InlineData("[c](//x:99999)")]
        [InlineData("[c](//[::1)")]
        [InlineData("[c](///)")]
        [InlineData("[c](//%)")]
        [InlineData("[c](//x:y@[z])")]
        [InlineData("[c](https://)")]
        [InlineData("[c](javascript:alert(1))")]
        public void Unresolvable_Or_Unsafe_Link_Renders_Text(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Empty(component.FindAll("a"));
            Assert.Equal("c", component.Find("p").TextContent);
        }

        [Theory]
        [InlineData("[c](/docs)", "/docs")]
        [InlineData("[c](https://radzen.com/a?b=1)", "https://radzen.com/a?b=1")]
        [InlineData("[c](mailto:a@example.com)", "mailto:a@example.com")]
        public void Resolvable_Link_Renders_Anchor(string markdown, string expected)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Equal(expected, component.Find("a").GetAttribute("href"));
        }

        [Theory]
        [InlineData("a <!--rz-outlet-99999999999999999999--> b")]
        [InlineData("<!--rz-outlet-99999999999999999999-->")]
        [InlineData("a <!--rz-outlet-\u0663--> b")]
        [InlineData("<!--rz-outlet-\u0663-->")]
        public void Malformed_Outlet_Markers_Render_Without_Throwing(string markdown)
        {
            using var ctx = new TestContext();

            var component = Render(ctx, markdown);

            Assert.Contains("rz-markdown", component.Markup);
        }

        [Fact]
        public void Tag_After_Unterminated_Comment_Is_Still_Html()
        {
            using var ctx = new TestContext();

            var component = Render(ctx, "x <!-- <b>y</b>\n\n<div>\n<!-- <b>z</b>\n</div>");

            Assert.Equal("y", component.Find("p b").TextContent);
            Assert.Equal("z", component.Find("div:not(.rz-markdown) b").TextContent);
        }

        [Theory]
        [InlineData("<!--")]
        [InlineData("<?")]
        [InlineData("<![CDATA[")]
        [InlineData("<!A")]
        [InlineData("<a ")]
        [InlineData("<a b=\"")]
        public void Unterminated_Html_Parses_And_Renders_In_Linear_Time(string opener)
        {
            var repeated = string.Concat(Enumerable.Repeat(opener, 65536 / opener.Length));

            var stopwatch = Stopwatch.StartNew();
            MarkdownParser.Parse("x " + repeated);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"inline took {stopwatch.ElapsedMilliseconds} ms");

            using var ctx = new TestContext();
            stopwatch.Restart();
            Render(ctx, "<div>\n" + repeated);
            Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"block took {stopwatch.ElapsedMilliseconds} ms");
        }

        [Fact]
        public void Open_Tag_With_Unicode_Spaces_Parses_In_Linear_Time()
        {
            var markdown = "x <a b=" + new string('\u00A0', 2000) + "c";

            var stopwatch = Stopwatch.StartNew();
            MarkdownParser.Parse(markdown);

            Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"took {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
