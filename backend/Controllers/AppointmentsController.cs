// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;


namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _db;
	private readonly TimeProvider _clock;
	private static readonly TimeZoneInfo KuwaitZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");

	// method that returns kuwait timing
	private DateTime GetKuwaitNow()
	{
		// 1. Get the current UTC time
		var utcNow = _clock.GetUtcNow();

		// 2. Convert UTC to Kuwait time
		var kuwaitNow = TimeZoneInfo.ConvertTime(
			utcNow, KuwaitZone
		);

		// 3. Return the Kuwait local date and time
		return kuwaitNow.DateTime;
	}

    // Constructor
    public AppointmentsController(ClinicDbContext db, TimeProvider? clock = null)
    {
        _db = db;
		_clock = clock ?? TimeProvider.System;	// to unify timezone

    }

    // GET request to retrieve appointments
    [HttpGet]
    public async Task<IActionResult> GetAppointments(	// Filters Added
		[FromQuery] int? doctorId,
    	[FromQuery] DateOnly? date
	)
    {
		// Start building a database query
		var appointments = _db.Appointments.AsQueryable();

		// Filter by Doctor ID
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
		var results = await appointments.ToListAsync();
		return Ok(results);
    }

	// POST request to create new appointments
		
	[HttpPost]
	public async Task<IActionResult> CreateAppointment([FromBody] Appointment appointment)
	{
		// 1. Validate Doctor and Patient IDs
		if (!await _db.Doctors.AnyAsync(d => d.Id == appointment.DoctorId)||
			!await _db.Patients.AnyAsync(p => p.Id == appointment.PatientId))
		{
			return BadRequest("Invalid doctor or patient ID.");
		}

		// 2. Validate 30-minute appointment slots
		if (appointment.StartTime == default ||
			(appointment.StartTime.Minute != 0 &&
			appointment.StartTime.Minute != 30) ||
			appointment.StartTime.Second != 0 ||
			appointment.StartTime.Millisecond != 0)
		{
			return BadRequest("Appointments must start on the hour or half hour.");
		}

		// 3. Validate working hours: 08:00 to 16:00
		if (appointment.StartTime.Hour < 8 ||
			appointment.StartTime.Hour >= 16)
		{
			return BadRequest("Appointments are only available from 08:00 to 16:00.");
		}

		// 4. Validate working days: Sunday to Thursday
		if (appointment.StartTime.DayOfWeek == DayOfWeek.Friday ||
			appointment.StartTime.DayOfWeek == DayOfWeek.Saturday)
		{
			return BadRequest("Appointments are only available Sunday to Thursday.");
		}

		// 5. Reject appointments in the past
		if (appointment.StartTime <= GetKuwaitNow())
		{
			return BadRequest("Cannot book an appointment in the past.");
		}
		// 6. Prevent double booking Doctor
		// Check whether the doctor already has an active booking
		bool booked = await _db.Appointments.AnyAsync(a => 
			a.DoctorId == appointment.DoctorId && 
			a.StartTime == appointment.StartTime && 
			!a.IsCancelled);

		if (booked){
			return Conflict("This doctor already has an appointment at this time.");
		}

		// Create the appointment
		appointment.Id = 0;
		appointment.IsCancelled = false;

		// Prepare the record for insertion
		_db.Appointments.Add(appointment);

		try
		{
			// Save the appointment to PostgreSQL
			await _db.SaveChangesAsync();
		}
		catch (DbUpdateException error) when (
			error.InnerException is Npgsql.PostgresException pgError &&
			pgError.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation &&
			pgError.ConstraintName == "IX_Appointments_DoctorId_StartTime")
		{
			return Conflict("This doctor already has an appointment at this time.");
		}

		return StatusCode(201, appointment);

	}


    // DELETE request to cancel an appointment
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        // find the appointment by ID
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id); // instead of .Any() because it retrieves the actual object i.e. data

        // check if appointment exists
        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        // Check if already cancelled
        if (appointment.IsCancelled)
        {
            return Conflict("Appointment is already cancelled.");
        }

        // Cancel the appointment
        appointment.IsCancelled = true;
		await _db.SaveChangesAsync();
        return NoContent();
    }

	// Get Doctor's Avaliability
	[HttpGet("availability")]
	public async Task<IActionResult> GetAvailability(
		[FromQuery] int doctorId,
		[FromQuery] DateOnly date)
	{
		// 1. Check if doctor exists
		if (! await _db.Doctors.AnyAsync(doctor => doctor.Id == doctorId))
		{
			return NotFound("Doctor not found.");
		}

		// 2. Check if date is valid
		if (date == default)
		{
			return BadRequest("Invalid date.");
		}

		// 3. Check working days
		if (date.DayOfWeek == DayOfWeek.Friday ||
			date.DayOfWeek == DayOfWeek.Saturday)
		{
			return BadRequest("The clinic is closed on Fridays and Saturdays.");
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

		return Ok(slots);
	}
}