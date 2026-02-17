using System.Collections.Generic;

public class MovieHalls
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string HallName { get; set; } = null!;
    public int AvailableSeats { get; set; }

    public ICollection<Screenings> Screenings { get; set; } = new List<Screenings>();
}
