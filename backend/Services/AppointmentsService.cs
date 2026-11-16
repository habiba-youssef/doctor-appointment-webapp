using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Helpers;

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
    AlreadyCancelled,
    InvalidReason,
    AlreadyStarted
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
    InvalidType,
	Closed,
}
public record AvailabilityResult(
    AvailabilityStatus Status,
	List<AvailabilitySlot>? Slots = null,
    string? Error = null
);

public class AppointmentsService
{
    private readonly ClinicDbContext _db;
    private readonly TimeProvider _clock;
    private static int GetAppointmentLength(AppointmentType type)
    {
        return type switch
        {
            AppointmentType.Consultation => 30,
            AppointmentType.FollowUp => 15,
            AppointmentType.Procedure => 60,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
    
    // Constructor
    public AppointmentsService(ClinicDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }
    
    // Create an appointment
    public async Task<BookingResult> CreateAppointmentAsync(Appointment appointment)
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

         // Check that appointment type is valid
        if (!Enum.IsDefined(appointment.Type))
        {
            return new(
                BookingStatus.Invalid,
                Error: "Invalid appointment type."
            );
        }

        // 2. Validate appointment start time
        if (appointment.StartTime == default ||
            appointment.StartTime.Minute % 15 != 0 ||
            appointment.StartTime.Second != 0 ||
            appointment.StartTime.Millisecond != 0)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments must start at :00, :15, :30 or :45"
            );
        }

        // Calculate duration and END time
        int durationMinutes = GetAppointmentLength(appointment.Type);
        DateTime endTime = appointment.StartTime.AddMinutes(durationMinutes);

        // 3. Validate working hours
        var clinicStart = appointment.StartTime.Date.AddHours(8);
        var clinicEnd = appointment.StartTime.Date.AddHours(16);

        if (appointment.StartTime < clinicStart || endTime > clinicEnd)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments are only available between 08:00 and 16:00."
            );
        }

        // 4. Validate working days
        if (appointment.StartTime.DayOfWeek == DayOfWeek.Friday || appointment.StartTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointments are only available Sunday to Thursday."
            );
        }

        // 5. Prevent bookings in the past (Kuwait time)
        if (appointment.StartTime <= KuwaitTimeHelper.GetNow(_clock))
        {
            return new(
                BookingStatus.Invalid,
                Error: "Cannot book an appointment in the past."
            );
        }

        // Check doctor's working schedule
        var schedule = await _db.DoctorSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.DoctorId == appointment.DoctorId &&
                s.WorkingDays == appointment.StartTime.DayOfWeek
            );

        // Doctor has no schedule for this day
        if (schedule == null)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Doctor is not scheduled to work on this day."
            );
        }

        // Check the entire appointment fits within working hours
        if (TimeOnly.FromDateTime(appointment.StartTime) < schedule.StartTime ||
            TimeOnly.FromDateTime(endTime) > schedule.EndTime)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Appointment is outside the doctor's working hours."
            );
        }

        // Check if doctor is on leave
        bool isOnLeave = await _db.DoctorLeaves
            .AnyAsync(l =>
                l.DoctorId == appointment.DoctorId &&
                l.LeaveDate == DateOnly.FromDateTime(appointment.StartTime)
            );

        if (isOnLeave)
        {
            return new(
                BookingStatus.Invalid,
                Error: "Doctor is on leave on this date."
            );
        }

        // 6. Check for overlapping appointments
        var dayStart = appointment.StartTime.Date;
        var dayEnd = dayStart.AddDays(1);

        // Retrieve active appointments for this doctor OR patient
        var existingAppointments = await _db.Appointments.AsNoTracking()
            .Where(a =>
                !a.IsCancelled &&
                (a.DoctorId == appointment.DoctorId ||
                a.PatientId == appointment.PatientId) &&
                a.StartTime >= dayStart &&
                a.StartTime < dayEnd)
            .ToListAsync();

        // Check each existing appointment for overlap
        foreach (var app in existingAppointments)
        {
            var appEnd = app.StartTime.AddMinutes(
                GetAppointmentLength(app.Type)
            );

            bool overlaps =
                appointment.StartTime < appEnd &&   // Does the new appointment start before the existing appointment ends?
                endTime > app.StartTime;        // Does the new appointment end after the existing appointment starts?

            if (overlaps)
            {
                // Doctor cannot have overlapping appointments
                if (app.DoctorId == appointment.DoctorId)
                {
                    return new(
                        BookingStatus.Conflict,
                        Error: "Doctor already has an overlapping appointment."
                    );
                }

                // Patient cannot have overlapping appointments
                return new(
                    BookingStatus.Conflict,
                    Error: "Patient already has an overlapping appointment."
                );
            }
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
        (
            // Same doctor and exact same start time
            (pgError.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation &&
            pgError.ConstraintName == "IX_Appointments_DoctorId_StartTime")

            ||

            // Same doctor and overlapping appointment time range
            (pgError.SqlState == Npgsql.PostgresErrorCodes.ExclusionViolation &&
            pgError.ConstraintName == "EX_Appointments_Doctor_NoOverlap")
        )
        )
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
    public async Task<CancellationResult> CancelAppointmentAsync(int id, string? reason)
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

        // validate cancellation reason
        if (string.IsNullOrWhiteSpace(reason))
        {
            return new(
                CancellationStatus.InvalidReason,
                Error: "Cancellation reason is required."
            );
        }

        // Prevent cancellation after appointment has started
        if (appointment.StartTime <= KuwaitTimeHelper.GetNow(_clock))
        {
            return new(
                CancellationStatus.AlreadyStarted,
                Error: "Cannot cancel an appointment that has already started."
            );
        }

        // Cancel the appointment
        appointment.IsCancelled = true;
        appointment.CancellationReason = reason.Trim();

		await _db.SaveChangesAsync();

		// Canellation successful
        return new(CancellationStatus.Cancelled);
	}

	// Get doctor's Availability
    public async Task<AvailabilityResult> GetAvailabilityAsync(int doctorId, DateOnly date, AppointmentType type)
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

        // Check if appointment type is valid
        if (!Enum.IsDefined(type))
        {
            return new(
                AvailabilityStatus.InvalidType,
                Error: "Invalid appointment type."
            );
        }

        // Get doctor's working schedule for the selected weekday
        var schedule = await _db.DoctorSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.DoctorId == doctorId &&
                s.WorkingDays == date.DayOfWeek
            );

        // Check if doctor is on leave
        bool isOnLeave = await _db.DoctorLeaves
            .AnyAsync(l =>
                l.DoctorId == doctorId &&
                l.LeaveDate == date
            );

		var dayStart = date.ToDateTime(TimeOnly.MinValue);
		var dayEnd = dayStart.AddDays(1);

		// Retrieve existing active bookings for this doctor and date
        var existingAppointments = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId &&
                        !a.IsCancelled &&
                        a.StartTime >= dayStart &&
                        a.StartTime < dayEnd)
            .ToListAsync();

		var slots = new List<AvailabilitySlot>();
		var now = KuwaitTimeHelper.GetNow(_clock);

        // Get duration of the requested appointment type
        int durationMinutes = GetAppointmentLength(type);
        // Clinic closing time
        var clinicEnd = date.ToDateTime(new TimeOnly(16, 0));

        // Generate 32 slots at 15-minute intervals
        for (int i = 0; i < 32; i++)
        {
            var slotTime = date.ToDateTime(new TimeOnly(8, 0)).AddMinutes(i * 15);
            var slotEnd = slotTime.AddMinutes(durationMinutes);

            // Check if this slot overlaps any existing appointment
            bool overlaps = existingAppointments.Any(existing =>
            {
                var existingEnd = existing.StartTime.AddMinutes(
                    GetAppointmentLength(existing.Type)
                );

                return slotTime < existingEnd &&
                    slotEnd > existing.StartTime;
            });

            // Check if the appointment fits inside the doctor's schedule
            bool withinSchedule =
                schedule != null &&
                TimeOnly.FromDateTime(slotTime) >= schedule.StartTime &&
                TimeOnly.FromDateTime(slotEnd) <= schedule.EndTime;

            // Determine whether the slot is available
            bool available =
                !overlaps &&
                slotTime > now &&
                slotEnd <= clinicEnd &&
                withinSchedule &&
                !isOnLeave;

            // Add the slot to the list
            slots.Add(new AvailabilitySlot(slotTime.ToString("HH:mm"),available));
        }

		return new(
			AvailabilityStatus.Valid,
			Slots: slots
		);
	}

    // Retrieve Appointments
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
			return await appointments
                .OrderBy(t => t.StartTime)
                .ToListAsync();
		}
}
