using System.Text.Json;
using Microsoft.Playwright;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Core;
using RestaurantAqaFramework.Screenplay.UI;

namespace RestaurantAqaFramework.Screenplay.Tasks;

/// <summary>
/// Signs in an Actor via the Angular Login UI (/login) or seeds localStorage with the Actor's JWT tokens.
/// </summary>
public sealed class SignInViaUiTask(bool useFastTokenInjection = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var page = web.Page;

        if (useFastTokenInjection && actor.RecallOrDefault<string>(MemoryKeys.IdToken) is { Length: > 0 } idToken)
        {
            var accessToken = actor.RecallOrDefault<string>(MemoryKeys.AccessToken) ?? idToken;
            var authJson = JsonSerializer.Serialize(new { idToken, accessToken });

            await page.GotoAsync($"{web.FrontendBaseUrl}/main", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.EvaluateAsync("(authStr) => localStorage.setItem('auth', authStr)", authJson);
            return;
        }

        var email = actor.Recall<string>(MemoryKeys.Email);
        var password = actor.Recall<string>(MemoryKeys.Password);

        await page.GotoAsync($"{web.FrontendBaseUrl}/login", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        var loginPage = new LoginPage(page);

        await loginPage.EmailInput.FillAsync(email);
        await loginPage.PasswordInput.FillAsync(password);
        await loginPage.SignInButton.ClickAsync();

        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

/// <summary>
/// Navigates the Owner Actor to /reservations/{reservationId}/group-order and waits until loading finishes.
/// </summary>
public sealed class OpenOwnerGroupOrderPageTask(string? reservationId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var page = web.Page;
        var targetReservationId = reservationId ?? actor.Recall<string>(MemoryKeys.ReservationId);

        await page.GotoAsync(
            $"{web.FrontendBaseUrl}/reservations/{Uri.EscapeDataString(targetReservationId)}/group-order",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var groupOrderPage = new GroupOrderPage(page);
        await groupOrderPage.PageHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await groupOrderPage.LoadingStateCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

/// <summary>
/// Clicks the Refresh button on the Owner's Group Pre-Order page to synchronize participant sessions immediately.
/// </summary>
public sealed class RefreshOwnerGroupOrderPageTask : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var groupOrderPage = new GroupOrderPage(web.Page);

        await groupOrderPage.RefreshButton.ClickAsync();
        await web.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}

/// <summary>
/// Opens the Guest Invite link (/join/{token}) in the Guest Actor's browser, enters the guest name,
/// adds dishes from the menu, and optionally submits the guest pre-order cart via the Cart Drawer.
/// </summary>
public sealed class JoinAndOrderAsGuestViaUiTask(
    string inviteToken,
    string guestName,
    IReadOnlyList<(string DishName, int Quantity)> dishesToSelect,
    bool submitCart = true) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var page = web.Page;
        var guestPage = new GuestJoinPage(page);

        await page.GotoAsync(
            $"{web.FrontendBaseUrl}/join/{Uri.EscapeDataString(inviteToken)}",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await guestPage.GuestNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await guestPage.GuestNameInput.FillAsync(guestName);
        await guestPage.BrowseMenuButton.ClickAsync();

        await guestPage.MenuHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        foreach (var (dishName, quantity) in dishesToSelect)
        {
            var card = guestPage.DishCards.Filter(new LocatorFilterOptions { HasText = dishName }).First;
            var preOrderBtn = card.GetByRole(AriaRole.Button, new() { Name = "Pre-order" });

            for (var i = 0; i < quantity; i++)
            {
                await preOrderBtn.ClickAsync();
            }
        }

        if (submitCart && dishesToSelect.Count > 0)
        {
            await guestPage.OpenCartButton.ClickAsync();
            await guestPage.CartDrawer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await guestPage.SubmitGuestCartButton.ClickAsync();
            await guestPage.SubmittedConfirmationComponent.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
    }
}

/// <summary>
/// Clicks "Submit Pre-Order" on the Owner's Group Order page and confirms inside the modal dialog.
/// </summary>
public sealed class ConfirmConsolidatedPreOrderViaUiTask : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var groupOrderPage = new GroupOrderPage(web.Page);

        await groupOrderPage.SubmitPreOrderButton.ClickAsync();
        await groupOrderPage.ConfirmSubmitModalButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await groupOrderPage.ConfirmSubmitModalButton.ClickAsync();

        await groupOrderPage.StatusBadge.Filter(new LocatorFilterOptions { HasText = "Sent to Restaurant" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}

/// <summary>
/// Clicks "Remove Guest" on a specific participant's card and confirms removal in the modal dialog.
/// </summary>
public sealed class RemoveGuestViaUiTask(string guestName) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var web = actor.Using<BrowseTheWeb>();
        var groupOrderPage = new GroupOrderPage(web.Page);

        var removeBtn = groupOrderPage.RemoveButtonForGuest(guestName);
        await removeBtn.ClickAsync();

        await groupOrderPage.ConfirmRemoveGuestModalButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await groupOrderPage.ConfirmRemoveGuestModalButton.ClickAsync();
        await groupOrderPage.ConfirmRemoveGuestModalButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }
}

