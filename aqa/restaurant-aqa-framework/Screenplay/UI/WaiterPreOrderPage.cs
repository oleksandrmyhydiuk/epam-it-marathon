using Microsoft.Playwright;

namespace RestaurantAqaFramework.Screenplay.UI;

/// <summary>
/// Locators for the Waiter Dashboard (/waiter-reservations) and Waiter Pre-Order page (/waiter-pre-order).
/// </summary>
public sealed class WaiterPreOrderPage(IPage page)
{
    public ILocator PageTitle => page.Locator("h2.title");
    public ILocator PreOrderDishCards => page.Locator("app-dish-card.pre-order-dish");
    public ILocator ConfirmDishButtons => page.Locator("app-custom-button.btn-confirm button");
    public ILocator CancelDishButtons => page.Locator("app-custom-button.btn-cancel button");
}

