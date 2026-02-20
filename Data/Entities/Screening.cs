using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Screenings
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid FilmId { get; set; }
    public Film Film { get; set; } = null!;

    public Guid MovieHallId { get; set; }
    public MovieHalls MovieHall { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsCancelled { get; set; }

    public ICollection<Tickets> Tickets { get; set; } = new List<Tickets>();
}
