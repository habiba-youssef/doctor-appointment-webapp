using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DoctorsAppointment.Api.Services;

// Possible outcomes of a booking attempt
public enum BookingStatus
{
    Created,
    Invalid,
    Conflict
}

// Result returned by the service to the controller
public record BookingResult(
    BookingStatus Status,
    Appointment? Appointment = null,
    string? Error = null
);

public class AppointmentService
{
    private readonly ClinicDbContext _db;
    private readonly TimeProvider _clock;

    private static readonly TimeZoneInfo KuwaitZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");

    // Constructor
    public AppointmentService(ClinicDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    // Get the current time in Kuwait
    private DateTime GetKuwaitNow()
    {
        var utcNow = _clock.GetUtcNow();
        var kuwaitNow = TimeZoneInfo.ConvertTime(utcNow, KuwaitZone);

        return kuwaitNow.DateTime;
    }

    // Business logic for creating an appointment
    public async Task<BookingResult> CreateAppointmentAsync(
        Appointment appointment)
    {
        // 1. Check doctor and patient exist
        bool doctorExists = await _db.Doctors.AnyAsync(d => d.Id == appointment.DoctorId);
        bool patientExists = await _db.Patients.AnyAsync(p => p.Id == appointment.PatientId);

        if (!doctorExists || !patientExists)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Invalid doctor or patient ID."
            );
        }

        // 2. Validate 30-minute appointment slots
        if (appointment.StartTime == default ||
            (appointment.StartTime.Minute != 0 &&
             appointment.StartTime.Minute != 30) ||
            appointment.StartTime.Second != 0 ||
            appointment.StartTime.Millisecond != 0)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments must start on the hour or half hour."
            );
        }

        // 3. Validate working hours
        if (appointment.StartTime.Hour < 8 ||
            appointment.StartTime.Hour >= 16)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments are only available from 08:00 to 16:00."
            );
        }

        // 4. Validate working days
        if (appointment.StartTime.DayOfWeek == DayOfWeek.Friday ||
            appointment.StartTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments are only available Sunday to Thursday."
            );
        }

        // 5. Prevent bookings in the past (Kuwait time)
        if (appointment.StartTime <= GetKuwaitNow())
        {
            return new(
                BookingStatus.Invalid,
                Error: "Cannot book an appointment in the past."
            );
        }

        // 6. Check for an existing active booking
        bool booked = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == appointment.DoctorId &&
            a.StartTime == appointment.StartTime &&
            !a.IsCancelled
        );

        if (booked)
        {
            return new(
                BookingStatus.Conflict,
                Error: "This doctor already has an appointment at this time."
            );
        }

        // 7. Create and save the appointment
        appointment.Id = 0;
        appointment.IsCancelled = false;

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (
            error.InnerException is Npgsql.PostgresException pgError &&
            pgError.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation &&
            pgError.ConstraintName == "IX_Appointments_DoctorId_StartTime")
        {
            return new(
                BookingStatus.Conflict,
                Error: "This doctor already has an appointment at this time."
            );
        }

        // 8. Booking successful
        return new(
            BookingStatus.Created,
            Appointment: appointment
        );
    }
}
