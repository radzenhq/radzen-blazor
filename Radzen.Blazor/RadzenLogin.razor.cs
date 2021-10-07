using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenLogin : RadzenComponent
    {
        [Parameter]
        public bool AutoComplete { get; set; } = true;

        protected override string GetComponentCssClass()
        {
            return "login";
        }

        string _username;
        [Parameter]
        public string Username
        {
            get
            {
                return _username;
            }
            set
            {
                if (_username != value)
                {
                    _username = value;
                }
            }
        }

        string _password;
        [Parameter]
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (_password != value)
                {
                    _password = value;
                }
            }
        }

        [Parameter]
        public EventCallback<Radzen.LoginArgs> Login { get; set; }

        [Parameter]
        public EventCallback Register { get; set; }

        [Parameter]
        public EventCallback<string> ResetPassword { get; set; }

        [Parameter]
        public bool AllowRegister { get; set; } = true;

        [Parameter]
        public bool AllowResetPassword { get; set; } = true;

        [Parameter]
        public string LoginText { get; set; } = "Login";

        [Parameter]
        public string RegisterText { get; set; } = "Sign up";

        [Parameter]
        public string RegisterMessageText { get; set; } = "Don't have an account yet?";

        [Parameter]
        public string ResetPasswordText { get; set; } = "Forgot password";

        [Parameter]
        public string UserText { get; set; } = "Username";

        [Parameter]
        public string UserRequired { get; set; } = "Username is required";

        [Parameter]
        public string PasswordText { get; set; } = "Password";

        [Parameter]
        public string PasswordRequired { get; set; } = "Password is required";

        protected async Task OnLogin()
        {
            if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
            {
                await Login.InvokeAsync(new Radzen.LoginArgs { Username = Username, Password = Password });
            }
        }

        protected async Task OnReset(EventArgs args)
        {
            if (!string.IsNullOrEmpty(Username))
            {
                await ResetPassword.InvokeAsync(Username);
            }
        }

        protected async Task OnRegister()
        {
            await Register.InvokeAsync(EventArgs.Empty);
        }
    }
}