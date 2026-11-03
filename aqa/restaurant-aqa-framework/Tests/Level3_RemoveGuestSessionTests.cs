using System.Net;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;

namespace RestaurantAqaFramework.Tests;

/// <summary>
/// Direction 1 — Level 3: Removing a Participant Session from Group Pre-Order
/// (DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId} + E2E UI flow).
/// Covers removing one of several guests, removing a submitted guest, removing the last guest,
/// cross-group session mismatch (400), non-existent session (404), non-Owner restrictions (401/403),
/// and the complete End-to-End flow from guest orders -> removal -> Owner submit -> Waiter verification.
/// </summary>
[Trait("Direction", "1")]
[Trait("Level", "3")]
public sealed class Level3_RemoveGuestSessionTests : TestBase
{
    [Fact(DisplayName = "L3.1 [UI+API] Removing one guest out of several keeps remaining participants and recalculates total")]
    [Trait("Category", "UI")]
    [Trait("Category", "API")]
    public async Task Remove_One_Guest_Of_Several_Recalculates_Total_And_Updates_Participants()
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dishes = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes);
        var dish1 = dishes[0];
        var dish2 = dishes.Count > 1 ? dishes[1] : dishes[0];

        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish1.Id, 2)],
                submitCart: false));
        var andriiSessionId = guestAndrii.Recall<string>(MemoryKeys.GuestSessionId);

        var guestOlena = CreateApiActor("Olena");
        await guestOlena.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Olena",
                dishesToOrder: [(dish2.Id, 1)],
                submitCart: true));

        // When Owner deletes Andrii's session
        var deleteRes = await Owner.AsksForAsync(
            new DeleteGuestSessionApiQuestion(groupReservationId, andriiSessionId));

        // Then updated snapshot contains only Olena and recalculated total
        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
        Assert.NotNull(deleteRes.Data);
        var remaining = Assert.Single(deleteRes.Data.Participants);
        Assert.Equal("Olena", remaining.Name);
        Assert.Equal(dish2.Price * 1, remaining.Total);
        Assert.Equal(dish2.Price * 1, deleteRes.Data.Total);
    }

    [Fact(DisplayName = "L3.2 [API Edge] Removing a guest who already submitted their cart and removing the last participant returns empty list and total 0")]
    [Trait("Category", "API")]
    public async Task Remove_Submitted_And_Last_Participant_Returns_Empty_List_And_Zero_Total()
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish.Id, 2)],
                submitCart: true));
        var andriiSessionId = guestAndrii.Recall<string>(MemoryKeys.GuestSessionId);

        var deleteRes = await Owner.AsksForAsync(
            new DeleteGuestSessionApiQuestion(groupReservationId, andriiSessionId));

        Assert.Equal(HttpStatusCode.OK, deleteRes.StatusCode);
        Assert.NotNull(deleteRes.Data);
        Assert.Empty(deleteRes.Data.Participants);
        Assert.Equal(0m, deleteRes.Data.Total);
    }

    [Fact(DisplayName = "L3.3 [API Negative] Removing a participant belonging to another independent group returns 400 Bad Request")]
    [Trait("Category", "API")]
    public async Task Remove_Participant_From_Another_Group_Returns_400_Bad_Request()
    {
        // Group A
        var (_, groupAId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);

        // Group B (second independent reservation by Owner)
        var ownerForGroupB = CreateApiActor("OwnerGroupB");
        var ownerEmail = Owner.Recall<string>(MemoryKeys.Email);
        var ownerPass = Owner.Recall<string>(MemoryKeys.Password);
        await ownerForGroupB.AttemptsToAsync(new AuthenticateCustomerViaApiTask(ownerEmail, ownerPass));
        var (_, groupBId, _) = await CreateTrackedGroupReservationAsync(ownerForGroupB, guestsCount: 3);

        // Guest joins Group B
        var foreignGuest = CreateApiActor("ForeignGuest");
        await foreignGuest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(groupBId, guestName: "ForeignGuest"));
        var foreignSessionId = foreignGuest.Recall<string>(MemoryKeys.GuestSessionId);

        // Attempt to delete Group B's guest session via Group A's URL
        var deleteRes = await Owner.AsksForAsync(
            new DeleteGuestSessionApiQuestion(groupAId, foreignSessionId));

        Assert.Equal(HttpStatusCode.BadRequest, deleteRes.StatusCode);
        Assert.Contains(
            "Guest session does not belong to this group reservation",
            deleteRes.ErrorMessage ?? deleteRes.RawBody,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "L3.4 [API Negative] Removing a non-existent guestSessionId returns 404 Not Found")]
    [Trait("Category", "API")]
    public async Task Remove_Non_Existent_Guest_Session_Returns_404_Not_Found()
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var fakeSessionId = $"missing-session-{Guid.NewGuid():N}";

        var deleteRes = await Owner.AsksForAsync(
            new DeleteGuestSessionApiQuestion(groupReservationId, fakeSessionId));

        Assert.Equal(HttpStatusCode.NotFound, deleteRes.StatusCode);
    }

    [Theory(DisplayName = "L3.5 [API Security] Non-Owner cannot delete a guest session from a group reservation")]
    [Trait("Category", "API")]
    [InlineData("OtherCustomer", 403)]
    [InlineData("Waiter", 403)]
    [InlineData("GuestSession", 401)]
    [InlineData("Unauthenticated", 401)]
    public async Task Non_Owner_Cannot_Delete_Guest_Session(string callerRole, int expectedStatusCode)
    {
        var (_, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);

        var guest = CreateApiActor("Andrii");
        await guest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(groupReservationId, guestName: "Andrii"));
        var guestSessionId = guest.Recall<string>(MemoryKeys.GuestSessionId);

        var caller = await CreateAuthenticatedRoleActorAsync(callerRole);
        var includeBearer = callerRole is "OtherCustomer" or "Waiter";
        var useGuestToken = callerRole == "GuestSession";

        var deleteRes = await caller.AsksForAsync(
            new DeleteGuestSessionApiQuestion(
                groupReservationId,
                guestSessionId,
                includeAuth: includeBearer,
                useGuestToken: useGuestToken));

        Assert.Equal(expectedStatusCode, deleteRes.StatusCodeInt);
    }

    [Fact(DisplayName = "L3.6 [Full E2E UI+API] Multiple guests add dishes -> Owner views -> removes one guest -> confirms -> Waiter sees order without removed guest")]
    [Trait("Category", "UI")]
    [Trait("Category", "API")]
    public async Task Full_E2E_Guests_Order_Owner_Removes_One_Guest_Submits_Waiter_Sees_Order_Without_Removed_Guest()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dishes = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes);
        var dish1 = dishes[0];
        var dish2 = dishes.Count > 1 ? dishes[1] : dishes[0];

        // 1. Two guests join, add dishes, and submit their carts
        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish1.Id, 2)],
                submitCart: true));
        var andriiSessionId = guestAndrii.Recall<string>(MemoryKeys.GuestSessionId);

        var guestOlena = CreateApiActor("Olena");
        await guestOlena.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Olena",
                dishesToOrder: [(dish2.Id, 1)],
                submitCart: true));

        // 2. Owner views both participants
        var beforeRemove = await Owner.AsksForAsync(new GetGroupSessionsSnapshotApiQuestion(groupReservationId));
        Assert.Equal(2, beforeRemove.Data!.Participants.Count);

        // 3. Owner removes Andrii and confirms the consolidated pre-order (via UI if configured, or API)
        if (IsFrontendConfigured())
        {
            await EnableWebBrowsingAsync(Owner);
            await Owner.AttemptsToAsync(
                new SignInViaUiTask(useFastTokenInjection: true),
                new OpenOwnerGroupOrderPageTask(reservationId),
                new RemoveGuestViaUiTask("Andrii"),
                new ConfirmConsolidatedPreOrderViaUiTask());
        }
        else
        {
            var afterRemove = await Owner.AsksForAsync(
                new DeleteGuestSessionApiQuestion(groupReservationId, andriiSessionId));
            Assert.Equal(HttpStatusCode.OK, afterRemove.StatusCode);
            Assert.Single(afterRemove.Data!.Participants);

            var submitRes = await Owner.AsksForAsync(
                new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));
            Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);
            Assert.Single(submitRes.Data!);
            Assert.Equal(dish2.Id, submitRes.Data![0].DishId);
            Assert.Equal(1, submitRes.Data[0].Quantity);
        }

        // 4. Waiter / Order view contains ONLY Olena's dish (dish2 x 1) and NOT removed Andrii's dish
        var orderRes = await Owner.AsksForAsync(new GetOrderByReservationApiQuestion(reservationId));
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        Assert.Equal("SubmittedByCustomer", orderRes.Data!.Status);

        var orderDish = Assert.Single(orderRes.Data.ResolvedDishes);
        Assert.Equal(dish2.Id, orderDish.Id);
        Assert.Equal(1, orderDish.Quantity);
    }
}

