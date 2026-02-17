using System.Collections.Generic;

public class Purchase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? GuestEmail { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int FullPrice { get; set; }

    public ICollection<Tickets> Tickets { get; set; } = new List<Tickets>();
}
