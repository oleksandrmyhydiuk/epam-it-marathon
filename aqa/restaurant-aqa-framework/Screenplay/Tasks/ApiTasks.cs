using System.Net;
using RestaurantAqaFramework.Config;
using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Core;

namespace RestaurantAqaFramework.Screenplay.Tasks;

public static class MemoryKeys
{
    public const string IdToken = "IdToken";
    public const string AccessToken = "AccessToken";
    public const string Email = "Email";
    public const string Password = "Password";
    public const string ReservationId = "ReservationId";
    public const string GroupReservationId = "GroupReservationId";
    public const string InviteToken = "InviteToken";
    public const string LocationId = "LocationId";
    public const string TableId = "TableId";
    public const string TableNumber = "TableNumber";
    public const string AvailableDishes = "AvailableDishes";
    public const string OwnerGuestSessionId = "OwnerGuestSessionId";
    public const string GuestSessionId = "GuestSessionId";
    public const string GuestSessionToken = "GuestSessionToken";
    public const string OrderId = "OrderId";
}

/// <summary>
/// Authenticates an Actor via REST API. If credentials are not provided in .env,
/// dynamically registers a unique customer account for isolated parallel test execution.
/// </summary>
public sealed class AuthenticateCustomerViaApiTask(
    string? email = null,
    string? password = null,
    string firstName = "Andrii",
    string lastName = "Tester") : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        var targetEmail = email;
        var targetPassword = password ?? "AqaTestPass123!";

        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..10];
            targetEmail = $"aqa.{actor.Name.ToLowerInvariant()}.{uniqueSuffix}@example.com";

            var signUpReq = new SignUpRequest(firstName, lastName, targetEmail, targetPassword);
            var signUpRes = await api.PostAsync<object>("/register", signUpReq, includeAuth: false);
            if (!signUpRes.IsSuccess && signUpRes.StatusCode != HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(
                    $"Failed to register dynamic test user '{targetEmail}': {(int)signUpRes.StatusCode} {signUpRes.RawBody}");
            }
        }

        var signInRes = await api.PostAsync<SignInResponse>(
            "/signin",
            new SignInRequest(targetEmail, targetPassword),
            includeAuth: false);

        if (!signInRes.IsSuccess || string.IsNullOrWhiteSpace(signInRes.Data?.IdToken))
        {
            throw new InvalidOperationException(
                $"Failed to sign in as '{targetEmail}': {(int)signInRes.StatusCode} {signInRes.RawBody}");
        }

        api.WithBearerToken(signInRes.Data.IdToken, targetEmail, targetPassword);
        actor.Remember(MemoryKeys.IdToken, signInRes.Data.IdToken);
        actor.Remember(MemoryKeys.AccessToken, signInRes.Data.AccessToken ?? signInRes.Data.IdToken);
        actor.Remember(MemoryKeys.Email, targetEmail);
        actor.Remember(MemoryKeys.Password, targetPassword);
    }
}

/// <summary>
/// Creates a dynamic future group reservation (GuestCount >= 2), discovers location dishes,
/// creates the associated Order record, and resolves both InviteToken and GroupReservationId.
/// </summary>
public sealed class CreateGroupReservationFixtureTask(
    int guestsCount = 4,
    bool createAssociatedOrder = true,
    int daysOffsetFromToday = 2) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();

        // 1. Discover Locations
        var locationsRes = await api.GetAsync<List<LocationDto>>("/locations", includeAuth: false);
        var locations = locationsRes.Data ?? [];
        if (locations.Count == 0)
            throw new InvalidOperationException($"No locations returned by /locations: {locationsRes.RawBody}");

        var preferredLocationId = TestSettings.Current.DefaultLocationId;
        var candidateLocations = !string.IsNullOrWhiteSpace(preferredLocationId)
            ? locations.Where(l => l.Id == preferredLocationId).Concat(locations).DistinctBy(l => l.Id).ToList()
            : locations;

        ReservationApiResponse? createdReservation = null;
        string? selectedLocationId = null;
        List<DishSummaryDto> locationDishes = [];

        // Try dynamic future dates/time slots so parallel runs never collide on the same table & slot
        var timeSlots = new (string From, string To)[]
        {
            ("12:00", "13:30"),
            ("14:00", "15:30"),
            ("16:00", "17:30"),
            ("18:00", "19:30"),
            ("20:00", "21:30")
        };

        var randomOffset = Random.Shared.Next(daysOffsetFromToday, daysOffsetFromToday + 25);

        foreach (var loc in candidateLocations)
        {
            var dishesRes = await api.GetAsync<List<DishSummaryDto>>($"/locations/{loc.Id}/dishes", includeAuth: false);
            var availableDishes = (dishesRes.Data ?? [])
                .Where(d => string.IsNullOrEmpty(d.State) || d.State.Equals("Available", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (availableDishes.Count == 0)
                continue;

            for (var dayTry = 0; dayTry < 10 && createdReservation is null; dayTry++)
            {
                var targetDate = DateTime.UtcNow.Date.AddDays(randomOffset + dayTry).ToString("yyyy-MM-dd");
                var tablesRes = await api.GetAsync<List<AvailableTableDto>>(
                    $"/available-tables?locationId={Uri.EscapeDataString(loc.Id)}&date={targetDate}&guests={guestsCount}",
                    includeAuth: false);

                var tables = (tablesRes.Data ?? [])
                    .Where(t => t.Capacity >= guestsCount && !string.IsNullOrWhiteSpace(t.ResolvedTableId))
                    .ToList();

                foreach (var table in tables)
                {
                    foreach (var slot in timeSlots)
                    {
                        var createReq = new CreateReservationApiRequest(
                            LocationId: loc.Id,
                            TableNumber: table.TableNumber,
                            TableId: table.ResolvedTableId,
                            Date: targetDate,
                            TimeFrom: slot.From,
                            TimeTo: slot.To,
                            GuestsNumber: guestsCount);

                        var createRes = await api.PostAsync<ReservationApiResponse>("/reservations", createReq);
                        if (createRes.IsSuccess && createRes.Data is not null && !string.IsNullOrWhiteSpace(createRes.Data.Id))
                        {
                            createdReservation = createRes.Data;
                            selectedLocationId = loc.Id;
                            locationDishes = availableDishes;
                            actor.Remember(MemoryKeys.TableId, table.ResolvedTableId);
                            actor.Remember(MemoryKeys.TableNumber, table.TableNumber);
                            break;
                        }
                    }

                    if (createdReservation is not null)
                        break;
                }
            }

            if (createdReservation is not null)
                break;
        }

        if (createdReservation is null || selectedLocationId is null)
        {
            throw new InvalidOperationException("Could not find an available table slot to create a group reservation.");
        }

        var reservationId = createdReservation.Id;
        actor.Remember(MemoryKeys.ReservationId, reservationId);
        actor.Remember(MemoryKeys.LocationId, selectedLocationId);
        actor.Remember(MemoryKeys.AvailableDishes, locationDishes);

        // 2. Ensure associated Order exists if requested (used by POST /group-reservations/{id}/submit)
        if (createAssociatedOrder && string.IsNullOrWhiteSpace(createdReservation.OrderId))
        {
            var initialOrderRes = await api.PostAsync<OrderDetailsDto>(
                "/orders",
                new CreateOrderApiRequest(reservationId, []));
            if (initialOrderRes.Data?.Id is not null)
            {
                actor.Remember(MemoryKeys.OrderId, initialOrderRes.Data.Id);
            }
        }
        else if (!string.IsNullOrWhiteSpace(createdReservation.OrderId))
        {
            actor.Remember(MemoryKeys.OrderId, createdReservation.OrderId);
        }

        // 3. Retrieve Invite Token & GroupReservationId
        var tokenRes = await api.GetAsync<InviteTokenDto>($"/reservations/{reservationId}/invite-token");
        if (!tokenRes.IsSuccess || string.IsNullOrWhiteSpace(tokenRes.Data?.ResolvedToken))
        {
            throw new InvalidOperationException(
                $"Failed to retrieve invite token for reservation '{reservationId}': {(int)tokenRes.StatusCode} {tokenRes.RawBody}");
        }

        var inviteToken = tokenRes.Data.ResolvedToken;
        actor.Remember(MemoryKeys.InviteToken, inviteToken);

        var inviteDetailsRes = await api.GetAsync<GroupInviteDetailsDto>($"/guest/invites/{inviteToken}", includeAuth: false);
        if (!inviteDetailsRes.IsSuccess || string.IsNullOrWhiteSpace(inviteDetailsRes.Data?.ResolvedGroupReservationId))
        {
            throw new InvalidOperationException(
                $"Failed to resolve groupReservationId from invite token '{inviteToken}': {(int)inviteDetailsRes.StatusCode} {inviteDetailsRes.RawBody}");
        }

        actor.Remember(MemoryKeys.GroupReservationId, inviteDetailsRes.Data.ResolvedGroupReservationId);
    }
}

/// <summary>
/// Joins a group reservation as a Guest via REST API, optionally adds dishes, and optionally submits the cart.
/// </summary>
public sealed class JoinGroupAsGuestViaApiTask(
    string groupReservationId,
    string guestName,
    IReadOnlyList<(string DishId, int Quantity)>? dishesToOrder = null,
    bool submitCart = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();

        var createSessionRes = await api.PostAsync<CreateGuestSessionApiResponse>(
            "/guest/sessions",
            new CreateGuestSessionApiRequest(groupReservationId, guestName),
            includeAuth: false);

        if (!createSessionRes.IsSuccess || string.IsNullOrWhiteSpace(createSessionRes.Data?.ResolvedSessionId))
        {
            throw new InvalidOperationException(
                $"Guest '{guestName}' failed to join group '{groupReservationId}': {(int)createSessionRes.StatusCode} {createSessionRes.RawBody}");
        }

        var sessionId = createSessionRes.Data.ResolvedSessionId;
        var sessionToken = createSessionRes.Data.ResolvedSessionToken;

        api.WithGuestSessionToken(sessionToken);
        actor.Remember(MemoryKeys.GuestSessionId, sessionId);
        actor.Remember(MemoryKeys.GuestSessionToken, sessionToken);

        if (dishesToOrder is not null)
        {
            foreach (var (dishId, quantity) in dishesToOrder)
            {
                var addDishRes = await api.PostAsync<object>(
                    $"/guest/sessions/{sessionId}/dishes",
                    new DishQuantityApiRequest(dishId, quantity),
                    includeAuth: false,
                    useGuestToken: true);

                if (!addDishRes.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Guest '{guestName}' failed to add dish '{dishId}' (qty {quantity}): {(int)addDishRes.StatusCode} {addDishRes.RawBody}");
                }
            }
        }

        if (submitCart)
        {
            var submitRes = await api.PostAsync<object>(
                $"/guest/sessions/{sessionId}/submit",
                body: new { },
                includeAuth: false,
                useGuestToken: true);

            if (!submitRes.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Guest '{guestName}' failed to submit cart '{sessionId}': {(int)submitRes.StatusCode} {submitRes.RawBody}");
            }
        }
    }
}

/// <summary>
/// Retrieves the Owner's own guestSessionId via GET /reservations/{reservationId}/guest-session-id,
/// optionally adds dishes to the Owner's session, and optionally submits the Owner's cart.
/// </summary>
public sealed class PrepareOwnerSessionCartViaApiTask(
    string reservationId,
    IReadOnlyList<(string DishId, int Quantity)>? dishesToOrder = null,
    bool submitOwnerCart = false) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();

        var ownerSessionRes = await api.GetAsync<OwnerSessionIdDto>($"/reservations/{reservationId}/guest-session-id");
        if (!ownerSessionRes.IsSuccess || string.IsNullOrWhiteSpace(ownerSessionRes.Data?.ResolvedGuestSessionId))
        {
            throw new InvalidOperationException(
                $"Failed to get owner guestSessionId for reservation '{reservationId}': {(int)ownerSessionRes.StatusCode} {ownerSessionRes.RawBody}");
        }

        var ownerSessionId = ownerSessionRes.Data.ResolvedGuestSessionId;
        actor.Remember(MemoryKeys.OwnerGuestSessionId, ownerSessionId);

        if (dishesToOrder is not null)
        {
            foreach (var (dishId, quantity) in dishesToOrder)
            {
                var addRes = await api.PostAsync<object>(
                    $"/guest/sessions/{ownerSessionId}/dishes",
                    new DishQuantityApiRequest(dishId, quantity));

                if (!addRes.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Owner failed to add dish '{dishId}' to own session '{ownerSessionId}': {(int)addRes.StatusCode} {addRes.RawBody}");
                }
            }
        }

        if (submitOwnerCart)
        {
            var submitRes = await api.PostAsync<object>($"/guest/sessions/{ownerSessionId}/submit", new { });
            if (!submitRes.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Owner failed to submit own session '{ownerSessionId}': {(int)submitRes.StatusCode} {submitRes.RawBody}");
            }
        }
    }
}

/// <summary>
/// Cleans up a test reservation after test execution so the environment stays clean.
/// </summary>
public sealed class CleanupReservationTask(string? reservationId = null) : ITask
{
    public async Task PerformAsAsync(Actor actor)
    {
        var targetId = reservationId ?? actor.RecallOrDefault<string>(MemoryKeys.ReservationId);
        if (string.IsNullOrWhiteSpace(targetId) || !actor.HasAbility<CallRestApi>())
            return;

        var api = actor.Using<CallRestApi>();
        try
        {
            await api.DeleteAsync<object>($"/reservations?id={Uri.EscapeDataString(targetId)}");
        }
        catch
        {
            // Ignore cleanup errors so test teardown never masks test assertions
        }
    }
}

