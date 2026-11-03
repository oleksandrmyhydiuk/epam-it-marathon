using Microsoft.Playwright;

namespace RestaurantAqaFramework.Screenplay.UI;

/// <summary>
/// Locators for the Owner's Group Pre-Order page (/reservations/{reservationId}/group-order).
/// Matches group-order-page.component.html and participant-card.component.html.
/// </summary>
public sealed class GroupOrderPage(IPage page)
{
    public ILocator PageHeading => page.GetByRole(AriaRole.Heading, new() { Name = "Group Pre-Order", Exact = true });
    public ILocator StatusBadge => page.Locator(".status-badge");
    public ILocator RefreshButton => page.GetByRole(AriaRole.Button, new() { Name = "Refresh", Exact = true });

    public ILocator LoadingStateCard => page.Locator(".state-card", new() { HasText = "Loading group pre-order" });
    public ILocator NotFoundAlert => page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Group reservation not found" });
    public ILocator RetryableAlert => page.GetByRole(AriaRole.Alert).Filter(new() { HasText = "Could not load group pre-order" });

    public ILocator WarningBanner => page.Locator(".warning-banner[role='status']");
    public ILocator EmptyParticipantsMessage => page.Locator("p.empty-state");

    public ILocator ParticipantCards => page.Locator("app-participant-card article.card");
    public ILocator ConsolidatedSection => page.GetByRole(AriaRole.Region, new() { Name = "Consolidated pre-order" });
    public ILocator ConsolidatedItems => page.Locator("section.consolidated-section li.consolidated-item");

    public ILocator GrandTotalAmount => page.Locator(".grand-total-amount");
    public ILocator SubmitPreOrderButton => page.Locator("footer.page-footer button.submit-btn");

    // Modals
    public ILocator ConfirmSubmitModalButton => page.Locator("app-modal button.modal-submit-btn");
    public ILocator ConfirmRemoveGuestModalButton => page.Locator("app-modal button.modal-danger-btn");
    public ILocator CancelModalButton => page.Locator("app-modal button.modal-cancel-btn");

    public ILocator RemoveButtonForGuest(string guestName) =>
        page.GetByRole(AriaRole.Button, new() { Name = $"Remove {guestName}", Exact = true });
}

