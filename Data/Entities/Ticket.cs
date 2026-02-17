using System.Collections.Generic;

public class Tickets
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ScreeningId { get; set; }
    public Screenings Screening { get; set; } = null!;
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = null!;
    public Guid? CashierId { get; set; }
    public User? Cashier { get; set; }
    public int SeatNumber { get; set; }
    public int TicketPrice { get; set; }
    public bool IsConfirmed { get; set; }
    public DateTime? ApprovedDate { get; set; }
}
