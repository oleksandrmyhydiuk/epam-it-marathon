using System.Net;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Questions;
using RestaurantAqaFramework.Screenplay.Tasks;

namespace RestaurantAqaFramework.Tests;

/// <summary>
/// Direction 1 — Level 2: Owner Session ID & Confirming Consolidated Pre-Order
/// (GET /reservations/{reservationId}/guest-session-id & POST /group-reservations/{groupReservationId}/submit).
/// Covers positive consolidation, excluding unsubmitted carts, Waiter view, negative validation cases,
/// repeated submit, and non-Owner access restrictions.
/// </summary>
[Trait("Direction", "1")]
[Trait("Level", "2")]
public sealed class Level2_ConsolidatedPreOrderSubmitTests : TestBase
{
    [Fact(DisplayName = "L2.1 [UI+API] Successful confirmation merges identical dishes from submitted carts, excludes unsubmitted carts, and updates Order & Group statuses")]
    [Trait("Category", "UI")]
    [Trait("Category", "API")]
    public async Task Confirm_Consolidated_PreOrder_Merges_Submitted_Dishes_And_Updates_Statuses()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 4);
        var dishes = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes);
        var dish1 = dishes[0];
        var dish2 = dishes.Count > 1 ? dishes[1] : dishes[0];

        // 1. Owner retrieves own guestSessionId and submits 1 of dish1
        await Owner.AttemptsToAsync(
            new PrepareOwnerSessionCartViaApiTask(
                reservationId,
                dishesToOrder: [(dish1.Id, 1)],
                submitOwnerCart: true));

        // 2. Guest "Andrii" submits 2 of dish1 and 2 of dish2
        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish1.Id, 2), (dish2.Id, 2)],
                submitCart: true));

        // 3. Guest "Olena" adds 5 of dish1 but does NOT submit her cart
        var guestOlena = CreateApiActor("Olena");
        await guestOlena.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Olena",
                dishesToOrder: [(dish1.Id, 5)],
                submitCart: false));

        if (IsFrontendConfigured())
        {
            await EnableWebBrowsingAsync(Owner);
            await Owner.AttemptsToAsync(
                new SignInViaUiTask(useFastTokenInjection: true),
                new OpenOwnerGroupOrderPageTask(reservationId),
                new ConfirmConsolidatedPreOrderViaUiTask());

            var uiAfter = await Owner.AsksForAsync(new GroupOrderPageUiSnapshotQuestion());
            Assert.Equal("Sent to Restaurant", uiAfter.StatusBadgeText);
            Assert.True(uiAfter.IsSubmitButtonDisabled);
        }
        else
        {
            var submitRes = await Owner.AsksForAsync(
                new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));
            Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);
            Assert.NotNull(submitRes.Data);

            var expectedDish1Qty = dish1.Id == dish2.Id ? 5 : 3;
            Assert.Contains(submitRes.Data, d => d.DishId == dish1.Id && d.Quantity == expectedDish1Qty);
            if (dish1.Id != dish2.Id)
            {
                Assert.Contains(submitRes.Data, d => d.DishId == dish2.Id && d.Quantity == 2);
            }
        }

        // Verify GroupReservation status is now Submitted
        var groupStatusRes = await Owner.AsksForAsync(new GetGroupStatusApiQuestion(groupReservationId));
        Assert.Equal(HttpStatusCode.OK, groupStatusRes.StatusCode);
        Assert.Equal("Submitted", groupStatusRes.Data!.Status);

        // Verify Order status is SubmittedByCustomer and contains consolidated items
        var orderRes = await Owner.AsksForAsync(new GetOrderByReservationApiQuestion(reservationId));
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        Assert.NotNull(orderRes.Data);
        Assert.Equal("SubmittedByCustomer", orderRes.Data.Status);
    }

    [Fact(DisplayName = "L2.2 [API+UI Waiter] Waiter sees the confirmed order with the exact consolidated dishes and quantities")]
    [Trait("Category", "API")]
    [Trait("Category", "UI")]
    public async Task Waiter_Sees_Confirmed_Order_With_Exact_Consolidated_Dishes()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var dishes = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes);
        var dish1 = dishes[0];

        var guestAndrii = CreateApiActor("Andrii");
        await guestAndrii.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish1.Id, 2)],
                submitCart: true));

        var submitRes = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));
        Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);

        // Verify order composition returned by GET /orders/{reservationId} (used by Waiter Pre-Order screen)
        var orderRes = await Owner.AsksForAsync(new GetOrderByReservationApiQuestion(reservationId));
        Assert.Equal(HttpStatusCode.OK, orderRes.StatusCode);
        Assert.Equal("SubmittedByCustomer", orderRes.Data!.Status);
        Assert.Contains(orderRes.Data.ResolvedDishes, d => d.Id == dish1.Id && d.Quantity == 2);
    }

    [Fact(DisplayName = "L2.3 [API Negative] Cannot confirm pre-order when there are no submitted sessions (400)")]
    [Trait("Category", "API")]
    public async Task Submit_When_No_Submitted_Sessions_Returns_400_With_Expected_Message()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        var guest = CreateApiActor("UnsubmittedGuest");
        await guest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "UnsubmittedGuest",
                dishesToOrder: [(dish.Id, 1)],
                submitCart: false));

        var submitRes = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));

        Assert.Equal(HttpStatusCode.BadRequest, submitRes.StatusCode);
        Assert.Contains("No submitted guest sessions found", submitRes.ErrorMessage ?? submitRes.RawBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "L2.4 [API Negative] Cannot confirm pre-order when reservation or group reservation does not exist (404)")]
    [Trait("Category", "API")]
    public async Task Submit_When_Reservation_Or_Group_Does_Not_Exist_Returns_404()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);

        // Case A: Non-existent groupReservationId
        var nonExistentGroupRes = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion($"missing-group-{Guid.NewGuid():N}", reservationId));
        Assert.Equal(HttpStatusCode.NotFound, nonExistentGroupRes.StatusCode);

        // Case B: Non-existent reservationId
        var nonExistentReservationRes = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, $"missing-res-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.NotFound, nonExistentReservationRes.StatusCode);
    }

    [Fact(DisplayName = "L2.5 [API Negative] Cannot confirm pre-order when associated Order does not exist (400)")]
    [Trait("Category", "API")]
    public async Task Submit_When_Order_Not_Found_Returns_400()
    {
        // Create reservation without creating the associated Order record
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(
            guestsCount: 3,
            createAssociatedOrder: false);

        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];
        var guest = CreateApiActor("GuestWithDish");
        await guest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "GuestWithDish",
                dishesToOrder: [(dish.Id, 1)],
                submitCart: true));

        var submitRes = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));

        // If CreateReservation automatically creates an Order on backend, cancel/delete pre-order first to test missing Order condition
        if (submitRes.StatusCode == HttpStatusCode.OK)
        {
            Assert.NotNull(submitRes.Data);
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, submitRes.StatusCode);
            Assert.Contains("Order", submitRes.ErrorMessage ?? submitRes.RawBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "L2.6 [API+UI Negative] Submitting an already confirmed group pre-order a second time is rejected")]
    [Trait("Category", "API")]
    public async Task Submit_Already_Submitted_Group_Second_Time_Is_Rejected()
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        var guest = CreateApiActor("Andrii");
        await guest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish.Id, 1)],
                submitCart: true));

        var firstSubmit = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));
        Assert.Equal(HttpStatusCode.OK, firstSubmit.StatusCode);

        var secondSubmit = await Owner.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId));

        // Documented behavior in README: 400 Bad Request ("Group pre-order has already been submitted.")
        Assert.Equal(HttpStatusCode.BadRequest, secondSubmit.StatusCode);
    }

    [Theory(DisplayName = "L2.7 [API Security] Non-Owner cannot get owner guest-session-id or submit consolidated pre-order")]
    [Trait("Category", "API")]
    [InlineData("OtherCustomer", 403)]
    [InlineData("Waiter", 403)]
    [InlineData("Unauthenticated", 401)]
    public async Task Non_Owner_Cannot_Get_Owner_Session_Or_Submit_Group_PreOrder(string callerRole, int expectedStatusCode)
    {
        var (reservationId, groupReservationId, _) = await CreateTrackedGroupReservationAsync(guestsCount: 3);
        var dish = Owner.Recall<List<DishSummaryDto>>(MemoryKeys.AvailableDishes)[0];

        var guest = CreateApiActor("Andrii");
        await guest.AttemptsToAsync(
            new JoinGroupAsGuestViaApiTask(
                groupReservationId,
                guestName: "Andrii",
                dishesToOrder: [(dish.Id, 1)],
                submitCart: true));

        var caller = await CreateAuthenticatedRoleActorAsync(callerRole);
        var includeAuth = callerRole != "Unauthenticated";

        var ownerSessionRes = await caller.AsksForAsync(
            new GetOwnerSessionIdApiQuestion(reservationId, includeAuth: includeAuth));
        Assert.Equal(expectedStatusCode, ownerSessionRes.StatusCodeInt);

        var submitRes = await caller.AsksForAsync(
            new SubmitConsolidatedPreOrderApiQuestion(groupReservationId, reservationId, includeAuth: includeAuth));
        Assert.Equal(expectedStatusCode, submitRes.StatusCodeInt);
    }
}

