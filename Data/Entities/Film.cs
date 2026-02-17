using System.Collections.Generic;

public class Film
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Length { get; set; }
    public int Rating { get; set; }
    public DateTime ReleaseDate { get; set; }

    public ICollection<Screenings> Screenings { get; set; } = new List<Screenings>();
}
