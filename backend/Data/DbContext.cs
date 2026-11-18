
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
    // Doctor schedules and leave
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorLeave> DoctorLeaves => Set<DoctorLeave>();
    public DbSet<ClinicUser> ClinicUsers => Set<ClinicUser>();

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
        
        // Prevent two active appointments for the same doctor and start time
        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.StartTime })
            .IsUnique()
            .HasFilter("\"IsCancelled\" = false");

        // Connect doctor schedules to doctors
        modelBuilder.Entity<DoctorSchedule>()
            .HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(s => s.DoctorId);

        // Connect doctor leave to doctors
        modelBuilder.Entity<DoctorLeave>()
            .HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(l => l.DoctorId);

        // Prevent duplicate schedules for the same doctor and day
        modelBuilder.Entity<DoctorSchedule>()
            .HasIndex(s => new { s.DoctorId, s.WorkingDays })
            .IsUnique();

        // Prevent duplicate leave records for the same doctor and date
        modelBuilder.Entity<DoctorLeave>()
            .HasIndex(l => new { l.DoctorId, l.LeaveDate })
            .IsUnique();

        // Each username must be unique
        modelBuilder.Entity<ClinicUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Connect doctor accounts to their doctor record
        modelBuilder.Entity<ClinicUser>()
            .HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(u => u.DoctorId);
            }
}
