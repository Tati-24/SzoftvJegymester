using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Film
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    public int Lengt { get; set; }

    public string? AgeRating { get; set; }

    public DateTime ReleaseDate { get; set; }

    public string? Genre { get; set; }

    public string? Director { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Screenings> Screenings { get; set; } = new List<Screenings>();
}
