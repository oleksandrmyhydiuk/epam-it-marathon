using System.Net;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;

namespace RestaurantAqaFramework.Tests;

/// <summary>
/// Direction 1 — Level 1: Viewing Group Pre-Order Participants (GET /guest/groups/{groupReservationId}/sessions + UI).
/// Covers positive E2E flows, dynamic cart updates, unsubmitted & empty carts, empty group, security matrix, and 404.
/// </summary>
[Trait("Direction", "1")]
[Trait("Level", "1")]
public sealed class Level1_GroupPreOrderParticipantsTests : TestBase
{
    [Fact(DisplayName = "L1.1 [UI+API] Owner creates reservation, guests join and add dishes, Owner sees all participants, dishes, quantities, and totals")]
    [Trait("Category", "UI")]
    [Trait("Category", "API")]
    public async Task Owner_Sees_All_Participants_Their_Dishes_Quantities_And_Totals()
    {
        // Given an authenticated Owner has created a group reservation for 4 guests
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dishes = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes);
        var dish1 = dishes[0];
        var dish2 = dishes.Count > 1 ? dishes[1] : dishes[0];

        // When Guest "Andrii" joins, adds 2 of dish1, and submits their cart
        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish1.Id, 2)],
                submitCart: true));

        // And Guest "Olena" joins, adds 1 of dish2, without submitting their cart
        var guestOlena = CreateApiActor("Olena");
        await guestOlena.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Olena",
                dishesToOrder: [(dish2.Id, 1)],
                submitCart: false));

        // Then Owner queries the group sessions snapshot
        var snapshotRes = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(HttpStatusCode.OK, snapshotRes.StatusCode);
        Assert.NotNull(snapshotRes.Data);

        var participants = snapshotRes.Data.Participants;
        Assert.Equal(2, participants.Count);

        var andrii = Assert.Single(participants, p => p.Name == "Andrii");
        Assert.Equal("Guest", andrii.Type);
        Assert.True(andrii.IsSubmitted);
        Assert.Single(andrii.Items);
        Assert.Equal(dish1.Id, andrii.Items[0].Dish.Id);
        Assert.Equal(2, andrii.Items[0].Quantity);
        Assert.Equal(dish1.Price * 2, andrii.Total);

        var olena = Assert.Single(participants, p => p.Name == "Olena");
        Assert.Equal("Guest", olena.Type);
        Assert.False(olena.IsSubmitted);
        Assert.Single(olena.Items);
        Assert.Equal(dish2.Id, olena.Items[0].Dish.Id);
        Assert.Equal(1, olena.Items[0].Quantity);
        Assert.Equal(dish2.Price * 1, olena.Total);

        Assert.Equal(andrii.Total + olena.Total, snapshotRes.Data.Total);

        // And if FRONTEND_BASE_URL is configured, verify Owner UI rendering in browser
        if (IsFrontendConfigured())
        {
            await EnableWebBrowsingAsync(Owner);
            await Owner.AttemptsToAsync(
                new SignInViaUiTask(useFastTokenInjection: true),
                new OpenOwnerGroupOrderPageTask(reservationId));

            var uiSnapshot = await Owner.AsksForAsync(new GroupOrderPageUiSnapshotQuestion());
            Assert.Equal("Collecting pre-orders", uiSnapshot.StatusBadgeText);
            Assert.Contains(uiSnapshot.ParticipantCards, c => c.Title == "Guest: Andrii" && c.StatusChip == "Submitted");
            Assert.Contains(uiSnapshot.ParticipantCards, c => c.Title == "Guest: Olena" && c.StatusChip == "Still choosing");
        }
    }

    [Fact(DisplayName = "L1.2 [API+UI] Total sum equals sum of participants and dynamically updates when a guest modifies their cart")]
    [Trait("Category", "API")]
    public async Task Total_Sum_Updates_Dynamically_When_Guest_Adds_Or_Removes_Dishes()
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        var guestTaras = CreateApiActor("Taras");
        await guestTaras.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Taras",
                dishesToOrder: [(dish.Id, 1)],
                submitCart: false));

        var sessionId = guestTaras.Recall<string>(MemoryKeys.GuestSessionId);

        // Initial check: quantity = 1
        var snap1 = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(HttpStatusCode.OK, snap1.StatusCode);
        Assert.Equal(dish.Price * 1, snap1.Data!.Total);

        // Guest adds 2 more of the same dish (total quantity = 3)
        var guestApi = guestTaras.Using<CallRestApi>();
        var addRes = await guestApi.PostAsync<object>(
            $"/guest/sessions/{sessionId}/dishes",
            new DishQuantityApiRequest(dish.Id, 2),
            includeAuth: false,
            useGuestToken: true);
        Assert.True(addRes.IsSuccess);

        var snap2 = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(dish.Price * 3, snap2.Data!.Total);
        Assert.Equal(3, Assert.Single(snap2.Data.Participants).Items[0].Quantity);

        // Guest removes 1 of the dish (total quantity = 2)
        var removeRes = await guestApi.DeleteAsync<object>(
            $"/guest/sessions/{sessionId}/dishes",
            new DishQuantityApiRequest(dish.Id, 1),
            includeAuth: false,
            useGuestToken: true);
        Assert.True(removeRes.IsSuccess);

        var snap3 = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(dish.Price * 2, snap3.Data!.Total);
        Assert.Equal(2, Assert.Single(snap3.Data.Participants).Items[0].Quantity);
    }

    [Fact(DisplayName = "L1.3 [UI+API] Unsubmitted participant and participant without dishes are both listed with proper flags")]
    [Trait("Category", "UI")]
    [Trait("Category", "API")]
    public async Task Unsubmitted_Participant_And_Participant_Without_Dishes_Are_Listed_Correctly()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        // Guest without dishes
        var guestIryna = CreateApiActor("Iryna");
        await guestIryna.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(groupReservationId, "Iryna", dishesToOrder: null, submitCart: false));

        // Guest with dishes but unsubmitted
        var guestMaksym = CreateApiActor("Maksym");
        await guestMaksym.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(groupReservationId, "Maksym", dishesToOrder: [(dish.Id, 1)], submitCart: false));

        var snapshotRes = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(HttpStatusCode.OK, snapshotRes.StatusCode);

        var iryna = Assert.Single(snapshotRes.Data!.Participants, p => p.Name == "Iryna");
        Assert.False(iryna.IsSubmitted);
        Assert.Empty(iryna.Items);
        Assert.Equal(0m, iryna.Total);

        var maksym = Assert.Single(snapshotRes.Data.Participants, p => p.Name == "Maksym");
        Assert.False(maksym.IsSubmitted);
        Assert.Single(maksym.Items);
        Assert.Equal(dish.Price, maksym.Total);

        if (IsFrontendConfigured())
        {
            await EnableWebBrowsingAsync(Owner);
            await Owner.AttemptsToAsync(
                new SignInViaUiTask(useFastTokenInjection: true),
                new OpenOwnerGroupOrderPageTask(reservationId));

            var uiSnapshot = await Owner.AsksForAsync(new GroupOrderPageUiSnapshotQuestion());
            var irynaCard = Assert.Single(uiSnapshot.ParticipantCards, c => c.Title == "Guest: Iryna");
            Assert.Equal("Still choosing", irynaCard.StatusChip);
            Assert.True(irynaCard.ShowsEmptyMessage);
            Assert.NotNull(uiSnapshot.WarningBannerText);
        }
    }

    [Fact(DisplayName = "L1.4 [API] Empty group where nobody has joined yet returns participants: [] and total: 0")]
    [Trait("Category", "API")]
    public async Task Empty_Group_Returns_Empty_Participants_And_Zero_Total()
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);

        var snapshotRes = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(HttpStatusCode.OK, snapshotRes.StatusCode);
        Assert.NotNull(snapshotRes.Data);
        Assert.Empty(snapshotRes.Data.Participants);
        Assert.Equal(0m, snapshotRes.Data.Total);
    }

    [Theory(DisplayName = "L1.5 [API Security] Only the Owner can view the group participants list")]
    [Trait("Category", "API")]
    [InlineData("OtherCustomer", 403)]
    [InlineData("Waiter", 403)]
    [InlineData("GuestSession", 401)]
    [InlineData("Unauthenticated", 401)]
    public async Task Non_Owner_Cannot_View_Group_Participants_List(string callerRole, int expectedStatusCode)
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var caller = await CreateAuthenticatedRoleActorAsync(callerRole);

        var includeBearer = callerRole is "OtherCustomer" or "Waiter";
        var useGuestToken = callerRole == "GuestSession";

        var res = await caller.AsksForAsync(
            new GetGroupSessionsSnapshotApiQuestion(
                groupReservationId,
                includeAuth: includeBearer,
                useGuestToken: useGuestToken));

        Assert.Equal(expectedStatusCode, res.StatusCodeInt);
    }

    [Fact(DisplayName = "L1.6 [API+UI Negative] Requesting participants for a non-existent group reservation returns 404")]
    [Trait("Category", "API")]
    public async Task Non_Existent_Group_Returns_404_Not_Found()
    {
        var nonExistentGroupId = $"non-existent-group-{Guid.NewGuid():N}";
        var res = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(nonExistentGroupId));

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}

