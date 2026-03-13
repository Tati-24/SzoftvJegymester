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

public record TicketPurchaseRequest(
    [Required] Guid ScreeningId,
    [Range(1, int.MaxValue)] int SeatNumber,
    decimal? TicketPrice,
    Guid? UserId,
    string? GuestName,
    [EmailAddress] string? GuestEmail,
    [Phone] string? GuestPhone);

public record ContactUpdateRequest(
    [Phone] string? PhoneNumber,
    [EmailAddress] string? Email);
