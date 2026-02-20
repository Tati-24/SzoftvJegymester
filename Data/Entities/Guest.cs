using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

/// <summary>Stores contact data for guest checkouts (no authentication account).</summary>
public class Guest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, Phone]
    public string PhoneNumber { get; set; } = null!;

    public ICollection<Tickets> Tickets { get; set; } = new List<Tickets>();
}
