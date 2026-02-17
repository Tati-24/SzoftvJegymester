using System.Collections.Generic;

public class Screenings
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid FilmId { get; set; }
    public Film Film { get; set; } = null!;

    public Guid MovieHallId { get; set; }
    public MovieHalls MovieHall { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public int BasePrice { get; set; }
    public bool MovieActive { get; set; } = true;

    public ICollection<Tickets> Tickets { get; set; } = new List<Tickets>();
}
