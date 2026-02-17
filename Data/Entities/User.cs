using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    public Roles Role { get; set; } = Roles.USER;
    [Required]
    public string PassWordHash { get; set; } = null!;
    [Required]
    public string UserName { get; set; } = null!;

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<Tickets> TicketsBought { get; set; } = new List<Tickets>();
    public ICollection<Tickets> TicketsValidated { get; set; } = new List<Tickets>();
}
