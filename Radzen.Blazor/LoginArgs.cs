namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenLogin.Login" /> event that is being raised.
/// </summary>
public class LoginArgs
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; }

    /// <summary> 
    /// Gets or sets a value indicating whether the user wants to remember their credentials. 
    /// </summary>
    public bool RememberMe { get; set; }
}

