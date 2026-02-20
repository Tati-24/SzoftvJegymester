using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Phone]
    public string? PhoneNumber { get; set; }

    public Roles Role { get; set; } = Roles.USER;

    [Required]
    public string PassWordHash { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Tickets> TicketsBought { get; set; } = new List<Tickets>();
}
