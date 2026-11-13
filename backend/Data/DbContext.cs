
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorsAppointment.Api.Data;

public class ClinicDbContext : DbContext
{
    public ClinicDbContext(
        DbContextOptions<ClinicDbContext> options
    ) : base(options)
    {
    }

    // These represent database tables
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Prevent duplicate Civil IDs at database level
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.CivilId)
            .IsUnique();

        // Connect appointments to doctors and patients
        modelBuilder.Entity<Appointment>()
            .HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(a => a.DoctorId);

        modelBuilder.Entity<Appointment>()
            .HasOne<Patient>()
            .WithMany()
            .HasForeignKey(a => a.PatientId);

        // Existing model uses Kuwait local DateTime values
        modelBuilder.Entity<Appointment>()
            .Property(a => a.StartTime)
            .HasColumnType("timestamp without time zone");
    }
}
