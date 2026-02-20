using System;
using System.ComponentModel.DataAnnotations;

public class Tickets
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ScreeningId { get; set; }
    public Screenings Screening { get; set; } = null!;

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? GuestId { get; set; }
    public Guest? Guest { get; set; }

    public int SeatNumber { get; set; }
    public decimal TicketPrice { get; set; }

    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public bool IsValidated { get; set; }
    public DateTime? ValidatedAt { get; set; }

    public bool IsCancelled { get; set; }
    public DateTime? CancelledAt { get; set; }
}
