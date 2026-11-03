using Microsoft.Playwright;

namespace RestaurantAqaFramework.Screenplay.UI;

/// <summary>
/// Locators for the Guest Join & Pre-Order page (/join/{token}).
/// Matches guest-pre-order-page.component.html, guest-name-modal.component.html, and guest-cart-drawer.component.html.
/// </summary>
public sealed class GuestJoinPage(IPage page)
{
    public ILocator GuestNameInput => page.GetByLabel("Enter Your Name");
    public ILocator BrowseMenuButton => page.GetByRole(AriaRole.Button, new() { Name = "Browse Menu" });

    public ILocator MenuHeading => page.GetByRole(AriaRole.Heading, new() { Name = "Menu", Exact = true });
    public ILocator DishCards => page.Locator("app-dish-card");
    public ILocator OpenCartButton => page.GetByRole(AriaRole.Button, new() { Name = "Open cart" });

    public ILocator CartDrawer => page.GetByRole(AriaRole.Dialog, new() { Name = "Cart" });
    public ILocator CartItems => CartDrawer.Locator(".item");
    public ILocator CartTotalValue => CartDrawer.Locator(".total-value");
    public ILocator SubmitGuestCartButton => CartDrawer.GetByRole(AriaRole.Button, new() { Name = "Submit Pre-Order" });

    public ILocator SubmittedConfirmationComponent => page.Locator("app-guest-preorder-submitted");
}

