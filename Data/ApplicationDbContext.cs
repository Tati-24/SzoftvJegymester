using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Film> Films { get; set; } = null!;
    public DbSet<MovieHalls> MovieHalls { get; set; } = null!;
    public DbSet<Screenings> Screenings { get; set; } = null!;
    public DbSet<Purchase> Purchases { get; set; } = null!;
    public DbSet<Tickets> Tickets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            .HasOne(t => t.Purchase)
            .WithMany(p => p.Tickets)
            .HasForeignKey(t => t.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tickets>()
            .HasOne(t => t.Buyer)
            .WithMany(u => u.TicketsBought)
            .HasForeignKey(t => t.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Tickets>()
            .HasOne(t => t.Cashier)
            .WithMany(u => u.TicketsValidated)
            .HasForeignKey(t => t.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasOne(p => p.User)
            .WithMany(u => u.Purchases)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
