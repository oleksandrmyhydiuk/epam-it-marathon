using System.Text.RegularExpressions;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Core;
using RestaurantAqaFramework.Screenplay.UI;

namespace RestaurantAqaFramework.Screenplay.Questions;

/// <summary>
/// Reads the complete rendered UI state of the Owner's Group Pre-Order page (/reservations/{reservationId}/group-order).
/// </summary>
public sealed class GroupOrderPageUiSnapshotQuestion : IQuestion<UiGroupOrderPageSnapshot>
{
    public async Task<UiGroupOrderPageSnapshot> AnsweredByAsync(Actor actor)
    {
        var page = actor.Using<BrowseTheWeb>().Page;
        var ui = new GroupOrderPage(page);

        var statusBadgeText = (await ui.StatusBadge.InnerTextAsync()).Trim();
        var showsNotFound = await ui.NotFoundAlert.IsVisibleAsync();

        if (showsNotFound)
        {
            return new UiGroupOrderPageSnapshot(
                StatusBadgeText: statusBadgeText,
                WarningBannerText: null,
                ShowsEmptyParticipantsState: false,
                ShowsNotFoundError: true,
                ParticipantCards: [],
                ConsolidatedItems: [],
                GrandTotalText: string.Empty,
                IsSubmitButtonDisabled: true);
        }

        var warningBannerText = await ui.WarningBanner.IsVisibleAsync()
            ? (await ui.WarningBanner.InnerTextAsync()).Trim()
            : null;

        var showsEmptyState = await ui.EmptyParticipantsMessage.IsVisibleAsync();

        var cardCount = await ui.ParticipantCards.CountAsync();
        var cards = new List<UiParticipantCardSnapshot>(cardCount);

        for (var i = 0; i < cardCount; i++)
        {
            var cardLoc = ui.ParticipantCards.Nth(i);
            var title = (await cardLoc.Locator(".title").InnerTextAsync()).Trim();
            var chip = (await cardLoc.Locator(".chip").InnerTextAsync()).Trim();
            var hasRemove = await cardLoc.Locator("button.remove-btn").IsVisibleAsync();
            var showsEmpty = await cardLoc.Locator("p.empty").IsVisibleAsync();
            var subtotalText = (await cardLoc.Locator("p.subtotal span").Last.InnerTextAsync()).Trim();

            var itemLocators = cardLoc.Locator("ul.items li.item");
            var itemCount = await itemLocators.CountAsync();
            var items = new List<(string Name, int Quantity, string PriceText)>(itemCount);

            for (var j = 0; j < itemCount; j++)
            {
                var itemLoc = itemLocators.Nth(j);
                var name = (await itemLoc.Locator(".name").InnerTextAsync()).Trim();
                var qtyRaw = (await itemLoc.Locator(".qty").InnerTextAsync()).Trim();
                var priceText = (await itemLoc.Locator(".price").InnerTextAsync()).Trim();
                var qty = ParseQuantity(qtyRaw);
                items.Add((name, qty, priceText));
            }

            cards.Add(new UiParticipantCardSnapshot(
                Title: title,
                StatusChip: chip,
                HasRemoveButton: hasRemove,
                Items: items,
                ShowsEmptyMessage: showsEmpty,
                SubtotalText: subtotalText));
        }

        var consolidatedItems = new List<(string Name, int Quantity, string PriceText)>();
        if (await ui.ConsolidatedSection.IsVisibleAsync())
        {
            var consCount = await ui.ConsolidatedItems.CountAsync();
            for (var i = 0; i < consCount; i++)
            {
                var row = ui.ConsolidatedItems.Nth(i);
                var name = (await row.Locator(".consolidated-name").InnerTextAsync()).Trim();
                var qtyRaw = (await row.Locator(".consolidated-qty").InnerTextAsync()).Trim();
                var priceText = (await row.Locator(".consolidated-price").InnerTextAsync()).Trim();
                consolidatedItems.Add((name, ParseQuantity(qtyRaw), priceText));
            }
        }

        var grandTotalText = (await ui.GrandTotalAmount.InnerTextAsync()).Trim();
        var isSubmitDisabled = await ui.SubmitPreOrderButton.IsDisabledAsync();

        return new UiGroupOrderPageSnapshot(
            StatusBadgeText: statusBadgeText,
            WarningBannerText: warningBannerText,
            ShowsEmptyParticipantsState: showsEmptyState,
            ShowsNotFoundError: false,
            ParticipantCards: cards,
            ConsolidatedItems: consolidatedItems,
            GrandTotalText: grandTotalText,
            IsSubmitButtonDisabled: isSubmitDisabled);
    }

    private static int ParseQuantity(string raw)
    {
        var match = Regex.Match(raw, @"\d+");
        return match.Success && int.TryParse(match.Value, out var q) ? q : 0;
    }
}

/// <summary>
/// Reads the dishes and quantities displayed to the Waiter on /waiter-pre-order.
/// </summary>
public sealed class WaiterPreOrderDishesUiQuestion : IQuestion<IReadOnlyList<(string DishName, int Quantity)>>
{
    public async Task<IReadOnlyList<(string DishName, int Quantity)>> AnsweredByAsync(Actor actor)
    {
        var page = actor.Using<BrowseTheWeb>().Page;
        var ui = new WaiterPreOrderPage(page);

        var count = await ui.PreOrderDishCards.CountAsync();
        var list = new List<(string DishName, int Quantity)>(count);

        for (var i = 0; i < count; i++)
        {
            var card = ui.PreOrderDishCards.Nth(i);
            var text = await card.InnerTextAsync();
            var qtyText = await card.Locator(".pre-order-quantity").InnerTextAsync();
            var match = Regex.Match(qtyText, @"\d+");
            var qty = match.Success && int.TryParse(match.Value, out var q) ? q : 0;
            var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
            list.Add((firstLine, qty));
        }

        return list;
    }
}

