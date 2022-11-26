using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LoginTests
    {
        [Fact]
        public void Login_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Login_Renders_UsernameLabel()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(p => {
                p.AddUnmatched("id", "login");
            });

            var label = component.Find($@"label[for=""login-username""]");
            Assert.NotNull(label);
        }

        [Fact]
        public void Login_Renders_UsernameInput()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            Assert.Contains(@$"<input name=""Username""", component.Markup);
        }

        [Fact]
        public void Login_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Login_Raises_LoginEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var clicked = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Username, "user");
                parameters.Add(p => p.Password, "pwd");
                parameters.Add(p => p.Login, args => { clicked = true; });
            });

            component.Find("button").Click();

            Assert.True(clicked);
        }

        [Fact]
        public void Login_NotRaises_LoginEvent_WhenEmptyUsernameOrPasword()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var clicked = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.Login, args => { clicked = true; });
            });

            component.Find("button").Click();

            Assert.True(!clicked);
        }

        [Fact]
        public void Login_Renders_LoginTextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.LoginText, "Test");
            });

            Assert.Contains(@$">Test</span>", component.Markup);
        }

        [Fact]
        public void Login_Renders_AllowResetPasswordParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowResetPassword, true);
            });

            Assert.Contains(@$"Forgot password?</a>", component.Markup);
        }

        [Fact]
        public void Login_Renders_ResetPasswordTextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowResetPassword, true);
                parameters.Add(p => p.ResetPasswordText, "Test");
            });

            Assert.Contains(@$"Test</a>", component.Markup);
        }

        [Fact]
        public void Login_Renders_AllowRegisterParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowRegister, true);
            });

            Assert.Contains(@$">Sign up</span>", component.Markup);
        }

        [Fact]
        public void Login_Renders_RegisterTextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowRegister, true);
                parameters.Add(p => p.RegisterText, "Test");
            });

            Assert.Contains(@$">Test</span>", component.Markup);
        }

        [Fact]
        public void Login_Renders_RegisterMessageTextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowRegister, true);
                parameters.Add(p => p.RegisterMessageText, "Test");
            });

            Assert.Contains(@$"Test", component.Markup);
        }

        [Fact]
        public void Login_Raises_RegisterEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var clicked = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowRegister, true);
                parameters.Add(p => p.Register, args => { clicked = true; });
            });

            component.Find(".rz-secondary").Click();

            Assert.True(clicked);
        }

        [Fact]
        public void Login_Raises_ResetPasswordEvent()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var clicked = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowResetPassword, true);
                parameters.Add(p => p.Username, "user");
                parameters.Add(p => p.ResetPassword, args => { clicked = true; });
            });

            component.Find("a").Click();

            Assert.True(clicked);
        }

        [Fact]
        public void Login_Raises_ResetPasswordEvent_WhenEmptyUsername()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenLogin>();

            var clicked = false;

            component.SetParametersAndRender(parameters => {
                parameters.Add(p => p.AllowResetPassword, true);
                parameters.Add(p => p.ResetPassword, args => { clicked = true; });
            });

            component.Find("a").Click();

            Assert.True(clicked);
        }
    }
}
