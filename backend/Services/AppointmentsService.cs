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

public enum CancellationStatus
{
    Cancelled,
    NotFound,
    AlreadyCancelled
}
public record CancellationResult(
    CancellationStatus Status,
    string? Error = null
);

public enum AvailabilityStatus
{
    Valid,
	InvalidDoc,
    InvalidDate,
	Closed,
}
public record AvailabilityResult(
    AvailabilityStatus Status,
	List<object>? Slots = null,
    string? Error = null
);

public class AppointmentService
{
    private readonly ClinicDbContext _db;
    private readonly TimeProvider _clock;
    private static readonly TimeZoneInfo KuwaitZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");
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

    // Create an appointment
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

	// Cancel an appointment
    public async Task<CancellationResult> CancelAppointmentAsync(int id)
	{
		// find the appointment by ID
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id); // instead of .Any() because it retrieves the actual object i.e. data

        // check if appointment exists
        if (appointment == null)
        {
            return new(
                CancellationStatus.NotFound,
                Error: "Appointment not found."
            );
        }

        // Check if already cancelled
        if (appointment.IsCancelled)
        {
            return new(
                CancellationStatus.AlreadyCancelled,
                Error: "Appointment is already cancelled."
            );
        }

        // Cancel the appointment
        appointment.IsCancelled = true;
		await _db.SaveChangesAsync();

		// Canellation successful
        return new(CancellationStatus.Cancelled);
	}

	// Get doctor's Availability
    public async Task<AvailabilityResult> GetAvailabilityAsync(int doctorId, DateOnly date)
	{
		// 1. Check if doctor exists
		if (! await _db.Doctors.AnyAsync(doctor => doctor.Id == doctorId))
		{
			return new(
                AvailabilityStatus.InvalidDoc,
                Error: "Doctor not found."
            );
		}

		// 2. Check if date is valid
		if (date == default)
		{
			return new(
                AvailabilityStatus.InvalidDate,
                Error: "Invalid Date"
            );
		}

		// 3. Check working days
		if (date.DayOfWeek == DayOfWeek.Friday ||
			date.DayOfWeek == DayOfWeek.Saturday)
		{
			return new(
                AvailabilityStatus.Closed,
                Error: "The clinic is closed on Fridays and Saturdays."
            );
		}

		var dayStart = date.ToDateTime(TimeOnly.MinValue);
		var dayEnd = dayStart.AddDays(1);

		// Retrieve existing active bookings for this doctor and date
		var bookedTimes = await _db.Appointments
			.Where(a => a.DoctorId == doctorId &&
						!a.IsCancelled &&
						a.StartTime >= dayStart &&
						a.StartTime < dayEnd)
			.Select(a => a.StartTime)
			.ToListAsync();

		var slots = new List<object>();
		var now = GetKuwaitNow();

		// Generate the existing 16 half-hour slots
		for (int i = 0; i < 16; i++)
		{
			var slotTime = date.ToDateTime(new TimeOnly(8, 0))
							.AddMinutes(i * 30);

			bool booked = bookedTimes.Contains(slotTime);

			slots.Add(new
			{
				time = slotTime.ToString("HH:mm"),
				available = !booked && slotTime > now
			});
		}

		return new(
			AvailabilityStatus.Valid,
			Slots: slots
		);
	}

	public async Task<List<Appointment>> GetAppointmentsAsync(int? doctorId, DateOnly? date)
		{
			// Start building a database query
			var appointments = _db.Appointments.AsQueryable();

			// Filter by Doctor's ID
			if (doctorId.HasValue)
			{
				appointments = appointments.Where(filter_id => filter_id.DoctorId == doctorId.Value);

			}

			// Filter by Date
			if (date.HasValue)
			{
				var start = date.Value.ToDateTime(TimeOnly.MinValue);
				var end = start.AddDays(1);

				appointments = appointments.Where(filter_date => filter_date.StartTime >= start && filter_date.StartTime < end);
			}

			// Execute the query and retrieve the results
			return await appointments.ToListAsync();
		}
}
