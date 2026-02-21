using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Film> Films { get; set; } = null!;
    public DbSet<MovieHalls> MovieHalls { get; set; } = null!;
    public DbSet<Screenings> Screenings { get; set; } = null!;
    public DbSet<Guest> Guests { get; set; } = null!;
    public DbSet<Tickets> Tickets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Screenings>()
            .HasOne(s => s.Film)
            .WithMany(f => f.Screenings)
            .HasForeignKey(s => s.FilmId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Screenings>()
            .HasOne(s => s.MovieHall)
            .WithMany(h => h.Screenings)
            .HasForeignKey(s => s.MovieHallId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tickets>()
            .HasOne(t => t.Screening)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.ScreeningId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tickets>()
            .HasOne(t => t.User)
            .WithMany(u => u.TicketsBought)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tickets>()
            .HasOne(t => t.Guest)
            .WithMany(g => g.Tickets)
            .HasForeignKey(t => t.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
