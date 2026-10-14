using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    // Each DbSet becomes a table
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventOrganiser> EventOrganisers => Set<EventOrganiser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Email).HasMaxLength(150);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(255);
            e.Property(u => u.Role).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Participant");
            e.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            e.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.EventName).HasMaxLength(150);
            e.Property(x => x.EventDate).HasColumnType("date");
            e.Property(x => x.Location).HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.EventType).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Run");
            e.Property(x => x.BannerImageUrl).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.ToTable(t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));

            e.HasOne(x => x.Organiser)
             .WithMany(u => u.OrganisedEvents)
             .HasForeignKey(x => x.OrganiserID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventOrganiser>(e =>
        {
            e.Property(x => x.AssignedRole).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Assistant");
            e.HasIndex(x => new { x.EventID, x.UserID }).IsUnique();

            e.HasOne(x => x.Event)
             .WithMany(ev => ev.EventOrganisers)
             .HasForeignKey(x => x.EventID)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User)
             .WithMany(u => u.EventOrganisers)
             .HasForeignKey(x => x.UserID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.CategoryName).HasMaxLength(100);
            e.Property(x => x.DistanceKM).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);

            e.HasOne(x => x.Event)
             .WithMany(ev => ev.Categories)
             .HasForeignKey(x => x.EventID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.Property(x => x.BibNumber).HasMaxLength(20).IsUnicode(false);
            e.HasIndex(x => x.BibNumber).IsUnique();
            e.Property(x => x.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Registered");
            e.HasIndex(x => new { x.ParticipantID, x.CategoryID }).IsUnique();

            e.HasOne(x => x.Participant)
             .WithMany(u => u.Enrolments)
             .HasForeignKey(x => x.ParticipantID)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Category)
             .WithMany(c => c.Enrolments)
             .HasForeignKey(x => x.CategoryID)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Result>(e =>
        {
            e.Property(x => x.FinishTime).HasColumnType("time");
            e.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Finished");

            // One enrolment has zero or one result; WithOne makes EnrolmentID unique
            e.HasOne(x => x.Enrolment)
             .WithOne(en => en.Result)
             .HasForeignKey<Result>(x => x.EnrolmentID)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}