// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Services;


namespace DoctorsAppointment.Api.Controllers;


[ApiController]
[Route("appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _db;
	private readonly TimeProvider _clock;
	private static readonly TimeZoneInfo KuwaitZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuwait");
	private readonly AppointmentService _appointmentService;

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
	public AppointmentsController(
		ClinicDbContext db,
		TimeProvider clock,
		AppointmentService appointmentService)
	{
		_db = db;
		_clock = clock;
		_appointmentService = appointmentService;
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
		// Go to service to create the appointment
		var result = await _appointmentService.CreateAppointmentAsync(appointment);

		// Convert the service result into HTTP response
		return result.Status switch
		{
			BookingStatus.Created => StatusCode(201, result.Appointment),
			BookingStatus.Invalid => BadRequest(result.Error),
			BookingStatus.Conflict => Conflict(result.Error),
			_ => throw new InvalidOperationException("Unexpected Booking Error")
		};
	}

    // DELETE request to cancel an appointment
	[HttpDelete("{id}")]
	public async Task<IActionResult> CancelAppointment(int id)
	{
		// Ask the service to cancel the appointment
		var result = await _appointmentService.CancelAppointmentAsync(id);

		// Convert the service result into an HTTP response
		return result.Status switch
		{
			CancellationStatus.Cancelled => NoContent(),
			CancellationStatus.NotFound => NotFound(result.Error),
			CancellationStatus.AlreadyCancelled => Conflict(result.Error),
			_ => throw new InvalidOperationException("Unexpected Cancellation Error.")
		};
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