using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class MovieHalls
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string HallName { get; set; } = null!;

    public int SeatCount { get; set; }

    public bool IsOccupied { get; set; } = true;

    public ICollection<Screenings> Screenings { get; set; } = new List<Screenings>();
}
