using RestaurantAqaFramework.Models;
using RestaurantAqaFramework.Screenplay.Abilities;
using RestaurantAqaFramework.Screenplay.Core;

namespace RestaurantAqaFramework.Screenplay.Questions;

/// <summary>
/// Level 1 Question: Queries GET /guest/groups/{groupReservationId}/sessions as the current Actor.
/// </summary>
public sealed class GetGroupSessionsSnapshotApiQuestion(
    string groupReservationId,
    bool includeAuth = true,
    bool useGuestToken = false) : IQuestion<ApiResult<GroupSessionsSnapshotDto>>
{
    public Task<ApiResult<GroupSessionsSnapshotDto>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.GetAsync<GroupSessionsSnapshotDto>(
            $"/guest/groups/{Uri.EscapeDataString(groupReservationId)}/sessions",
            includeAuth: includeAuth,
            useGuestToken: useGuestToken);
    }
}

/// <summary>
/// Level 2.1 Question: Queries GET /reservations/{reservationId}/guest-session-id as the current Actor.
/// </summary>
public sealed class GetOwnerSessionIdApiQuestion(
    string reservationId,
    bool includeAuth = true,
    bool useGuestToken = false) : IQuestion<ApiResult<OwnerSessionIdDto>>
{
    public Task<ApiResult<OwnerSessionIdDto>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.GetAsync<OwnerSessionIdDto>(
            $"/reservations/{Uri.EscapeDataString(reservationId)}/guest-session-id",
            includeAuth: includeAuth,
            useGuestToken: useGuestToken);
    }
}

/// <summary>
/// Level 2.2 Question: Executes POST /group-reservations/{groupReservationId}/submit as the current Actor.
/// </summary>
public sealed class SubmitConsolidatedPreOrderApiQuestion(
    string groupReservationId,
    string reservationId,
    bool includeAuth = true,
    bool useGuestToken = false) : IQuestion<ApiResult<List<ConsolidatedDishDto>>>
{
    public Task<ApiResult<List<ConsolidatedDishDto>>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.PostAsync<List<ConsolidatedDishDto>>(
            $"/group-reservations/{Uri.EscapeDataString(groupReservationId)}/submit",
            new SubmitGroupPreOrderApiRequest(reservationId),
            includeAuth: includeAuth,
            useGuestToken: useGuestToken);
    }
}

/// <summary>
/// Level 3 Question: Executes DELETE /guest/groups/{groupReservationId}/sessions/{guestSessionId} as the current Actor.
/// </summary>
public sealed class DeleteGuestSessionApiQuestion(
    string groupReservationId,
    string guestSessionId,
    bool includeAuth = true,
    bool useGuestToken = false) : IQuestion<ApiResult<GroupSessionsSnapshotDto>>
{
    public Task<ApiResult<GroupSessionsSnapshotDto>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.DeleteAsync<GroupSessionsSnapshotDto>(
            $"/guest/groups/{Uri.EscapeDataString(groupReservationId)}/sessions/{Uri.EscapeDataString(guestSessionId)}",
            includeAuth: includeAuth,
            useGuestToken: useGuestToken);
    }
}

/// <summary>
/// Queries GET /guest/groups/{groupReservationId}/status.
/// </summary>
public sealed class GetGroupStatusApiQuestion(string groupReservationId) : IQuestion<ApiResult<GroupReservationStatusDto>>
{
    public Task<ApiResult<GroupReservationStatusDto>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.GetAsync<GroupReservationStatusDto>(
            $"/guest/groups/{Uri.EscapeDataString(groupReservationId)}/status",
            includeAuth: false);
    }
}

/// <summary>
/// Queries GET /orders/{reservationId} to inspect the restaurant order state (e.g. as Waiter or Owner).
/// </summary>
public sealed class GetOrderByReservationApiQuestion(string reservationId) : IQuestion<ApiResult<OrderDetailsDto>>
{
    public Task<ApiResult<OrderDetailsDto>> AnsweredByAsync(Actor actor)
    {
        var api = actor.Using<CallRestApi>();
        return api.GetAsync<OrderDetailsDto>($"/orders/{Uri.EscapeDataString(reservationId)}");
    }
}

