using Microsoft.Playwright;

namespace RestaurantAqaFramework.Screenplay.UI;

/// <summary>
/// Locators for Login & Registration pages (/login, /signup).
/// Follows role/label-first locator priority.
/// </summary>
public sealed class LoginPage(IPage page)
{
    public ILocator EmailInput => page.GetByPlaceholder("e.g. username@domain.com");
    public ILocator PasswordInput => page.Locator("form.form input[formcontrolname='password']");
    public ILocator SignInButton => page.GetByRole(AriaRole.Button, new() { Name = "Sign In" });
    public ILocator ErrorMessage => page.Locator(".field-error, .banner-error");
}

