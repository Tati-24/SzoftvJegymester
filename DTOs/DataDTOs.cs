using System;
using System.ComponentModel.DataAnnotations;

public record FilmCreateRequest(
    [Required, MaxLength(200)] string Title,
    [Required, MaxLength(4000)] string Description,
    [Range(1, 1000)] int Length,
    string? AgeRating,
    [Required] DateTime ReleaseDate,
    string? Genre,
    string? Director,
    bool IsActive = true);

public record FilmUpdateRequest(
    [MaxLength(200)] string? Title,
    [MaxLength(4000)] string? Description,
    [Range(1, 1000)] int? Length,
    string? AgeRating,
    DateTime? ReleaseDate,
    string? Genre,
    string? Director,
    bool? IsActive);

public record MovieHallCreateRequest(
    [Required, MaxLength(100)] string HallName,
    [Range(1, int.MaxValue)] int SeatCount,
    bool IsOccupied = false);

public record MovieHallUpdateRequest(
    [MaxLength(100)] string? HallName,
    [Range(1, int.MaxValue)] int? SeatCount,
    bool? IsOccupied);

public record ScreeningCreateRequest(
    [Required] Guid FilmId,
    [Required] Guid MovieHallId,
    [Required] DateTime StartTime,
    [Range(0, double.MaxValue)] decimal BasePrice);

public record ScreeningUpdateRequest(
    Guid? FilmId,
    Guid? MovieHallId,
    DateTime? StartTime,
    decimal? BasePrice,
    bool? IsCancelled);

public record ScreeningResponse(
    Guid Id,
    Guid FilmId,
    string FilmTitle,
    Guid MovieHallId,
    string MovieHallName,
    DateTime StartTime,
    decimal BasePrice,
    bool IsCancelled);

public record ScreeningDeleteResponse(
    string Message,
    Guid FilmId,
    string FilmTitle,
    Guid MovieHallId,
    string MovieHallName);

public enum TicketBuyerType
{
    RegisteredUser,
    Guest
}

public record TicketPurchaseRequest(
    [Required] Guid ScreeningId,
    [Range(1, int.MaxValue)] int SeatNumber,
    [Required] TicketBuyerType BuyerType,
    Guid? UserId,
    string? GuestName,
    [EmailAddress] string? GuestEmail,
    [Phone] string? GuestPhone);
public record TicketPurchaseResponse(
    Guid? ScreeningId,
    [Range(1, int.MaxValue)] int SeatNumber,
    decimal Price,
    DateTime purchasedAt,
    Guid? UserId,
    string? GuestName,
    [EmailAddress] string? GuestEmail,
    [Phone] string? GuestPhone);

public record TicketResponse(
    Guid TicketId,
    Guid ScreeningId,
    Guid FilmId,
    string FilmTitle,
    Guid MovieHallId,
    string MovieHallName,
    DateTime ScreeningStartTime,
    int SeatNumber,
    decimal Price,
    DateTime PurchasedAt,
    TicketBuyerType BuyerType,
    Guid? UserId,
    string? UserName,
    string? UserEmail,
    Guid? GuestId,
    string? GuestName,
    string? GuestEmail,
    string? GuestPhone,
    bool IsValidated,
    DateTime? ValidatedAt,
    bool IsCancelled,
    DateTime? CancelledAt);

public record ContactUpdateRequest(
    [Phone] string? PhoneNumber,
    [EmailAddress] string? Email);
