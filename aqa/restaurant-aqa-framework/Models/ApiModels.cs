using System.Net;
using System.Text.Json.Serialization;

namespace RestaurantAqaFramework.Models;

public sealed record ApiResult<T>(
    HttpStatusCode StatusCode,
    string RawBody,
    T? Data,
    string? ErrorMessage)
{
    public int StatusCodeInt => (int)StatusCode;
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public sealed record SignUpRequest(
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public sealed record SignInRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("password")] string Password);

public sealed record SignInResponse(
    [property: JsonPropertyName("idToken")] string IdToken,
    [property: JsonPropertyName("accessToken")] string? AccessToken,
    [property: JsonPropertyName("role")] string? Role,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("locationId")] string? LocationId);

public sealed record LocationDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("totalCapacity")] int TotalCapacity,
    [property: JsonPropertyName("averageOccupancy")] double AverageOccupancy,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl,
    [property: JsonPropertyName("rating")] double Rating);

public sealed record AvailableTableDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("tableId")] string? TableId,
    [property: JsonPropertyName("tableNumber")] int TableNumber,
    [property: JsonPropertyName("capacity")] int Capacity,
    [property: JsonPropertyName("locationId")] string? LocationId,
    [property: JsonPropertyName("locationAddress")] string? LocationAddress,
    [property: JsonPropertyName("availableSlots")] List<string>? AvailableSlots)
{
    public string ResolvedTableId => !string.IsNullOrWhiteSpace(Id) ? Id : (TableId ?? string.Empty);
}

public sealed record CreateReservationApiRequest(
    [property: JsonPropertyName("locationId")] string LocationId,
    [property: JsonPropertyName("tableNumber")] int TableNumber,
    [property: JsonPropertyName("tableId")] string TableId,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("timeFrom")] string TimeFrom,
    [property: JsonPropertyName("timeTo")] string TimeTo,
    [property: JsonPropertyName("guestsNumber")] int GuestsNumber);

public sealed record ReservationApiResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("locationAddress")] string? LocationAddress,
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("timeSlot")] string? TimeSlot,
    [property: JsonPropertyName("preOrder")] string? PreOrder,
    [property: JsonPropertyName("guestsNumber")] string? GuestsNumber,
    [property: JsonPropertyName("feedbackId")] string? FeedbackId,
    [property: JsonPropertyName("tableNumber")] int? TableNumber,
    [property: JsonPropertyName("orderId")] string? OrderId,
    [property: JsonPropertyName("userInfo")] string? UserInfo);

public sealed record InviteTokenDto(
    [property: JsonPropertyName("token")] string? Token,
    [property: JsonPropertyName("Token")] string? TokenPascal)
{
    public string ResolvedToken => !string.IsNullOrWhiteSpace(Token) ? Token : (TokenPascal ?? string.Empty);
}

public sealed record GroupInviteDetailsDto(
    [property: JsonPropertyName("groupReservationId")] string? GroupReservationIdCamel,
    [property: JsonPropertyName("GroupReservationId")] string? GroupReservationIdPascal,
    [property: JsonPropertyName("locationId")] string? LocationIdCamel,
    [property: JsonPropertyName("LocationId")] string? LocationIdPascal,
    [property: JsonPropertyName("restaurantName")] string? RestaurantName,
    [property: JsonPropertyName("locationAddress")] string? LocationAddress,
    [property: JsonPropertyName("maxGuestCount")] int? MaxGuestCountCamel,
    [property: JsonPropertyName("MaxGuestCount")] int? MaxGuestCountPascal)
{
    public string ResolvedGroupReservationId =>
        !string.IsNullOrWhiteSpace(GroupReservationIdCamel) ? GroupReservationIdCamel : (GroupReservationIdPascal ?? string.Empty);

    public string ResolvedLocationId =>
        !string.IsNullOrWhiteSpace(LocationIdCamel) ? LocationIdCamel : (LocationIdPascal ?? string.Empty);

    public int ResolvedMaxGuestCount => MaxGuestCountCamel ?? MaxGuestCountPascal ?? 0;
}

public sealed record CreateGuestSessionApiRequest(
    [property: JsonPropertyName("groupReservationId")] string GroupReservationId,
    [property: JsonPropertyName("guestName")] string GuestName);

public sealed record CreateGuestSessionApiResponse(
    [property: JsonPropertyName("sessionId")] string? SessionIdCamel,
    [property: JsonPropertyName("SessionId")] string? SessionIdPascal,
    [property: JsonPropertyName("sessionToken")] string? SessionTokenCamel,
    [property: JsonPropertyName("SessionToken")] string? SessionTokenPascal)
{
    public string ResolvedSessionId =>
        !string.IsNullOrWhiteSpace(SessionIdCamel) ? SessionIdCamel : (SessionIdPascal ?? string.Empty);

    public string ResolvedSessionToken =>
        !string.IsNullOrWhiteSpace(SessionTokenCamel) ? SessionTokenCamel : (SessionTokenPascal ?? string.Empty);
}

public sealed record DishQuantityApiRequest(
    [property: JsonPropertyName("dishId")] string DishId,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record DishSummaryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("weight")] string? Weight,
    [property: JsonPropertyName("imageUrl")] string? ImageUrl,
    [property: JsonPropertyName("state")] string? State);

public sealed record ParticipantOrderItemDto(
    [property: JsonPropertyName("dish")] DishSummaryDto Dish,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record ParticipantDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("items")] List<ParticipantOrderItemDto> Items,
    [property: JsonPropertyName("total")] decimal Total,
    [property: JsonPropertyName("isSubmitted")] bool IsSubmitted);

public sealed record GroupSessionsSnapshotDto(
    [property: JsonPropertyName("participants")] List<ParticipantDto> Participants,
    [property: JsonPropertyName("total")] decimal Total);

public sealed record OwnerSessionIdDto(
    [property: JsonPropertyName("guestSessionId")] string? GuestSessionIdCamel,
    [property: JsonPropertyName("GuestSessionId")] string? GuestSessionIdPascal)
{
    public string ResolvedGuestSessionId =>
        !string.IsNullOrWhiteSpace(GuestSessionIdCamel) ? GuestSessionIdCamel : (GuestSessionIdPascal ?? string.Empty);
}

public sealed record SubmitGroupPreOrderApiRequest(
    [property: JsonPropertyName("reservationId")] string ReservationId);

public sealed record ConsolidatedDishDto(
    [property: JsonPropertyName("dishId")] string DishId,
    [property: JsonPropertyName("quantity")] int Quantity);

public sealed record GroupReservationStatusDto(
    [property: JsonPropertyName("currentParticipants")] int CurrentParticipants,
    [property: JsonPropertyName("maxCapacity")] int MaxCapacity,
    [property: JsonPropertyName("status")] string Status);

public sealed record CreateOrderApiRequest(
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("dishes")] List<DishQuantityApiRequest> Dishes);

public sealed record OrderDishDetailsDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("state")] int State);

public sealed record OrderDetailsDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("dishes")] List<OrderDishDetailsDto>? Dishes,
    [property: JsonPropertyName("dishItems")] List<OrderDishDetailsDto>? DishItems)
{
    public List<OrderDishDetailsDto> ResolvedDishes => Dishes ?? DishItems ?? [];
}

public sealed record UiParticipantCardSnapshot(
    string Title,
    string StatusChip,
    bool HasRemoveButton,
    IReadOnlyList<(string Name, int Quantity, string PriceText)> Items,
    bool ShowsEmptyMessage,
    string SubtotalText);

public sealed record UiGroupOrderPageSnapshot(
    string StatusBadgeText,
    string? WarningBannerText,
    bool ShowsEmptyParticipantsState,
    bool ShowsNotFoundError,
    IReadOnlyList<UiParticipantCardSnapshot> ParticipantCards,
    IReadOnlyList<(string Name, int Quantity, string PriceText)> ConsolidatedItems,
    string GrandTotalText,
    bool IsSubmitButtonDisabled);
