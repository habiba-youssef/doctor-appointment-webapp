// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("appointments")]
public class AppointmentsController : ControllerBase
{
	private readonly AppointmentsService _appointmentService;
    // Constructor
	public AppointmentsController(AppointmentsService appointmentService)
	{
		_appointmentService = appointmentService;
	}

    // GET request to retrieve appointments
    [HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] int? doctorId, [FromQuery] DateOnly? date)
    {
        // Ask the service to retrieve appointments
        var appointments = await _appointmentService.GetAppointmentsAsync(doctorId, date);
        // Return appointments to frontend
        return Ok(appointments);
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


	// GET request to retrieve Doctor's Avaliability
	[HttpGet("availability")]
	public async Task<IActionResult> GetAvailability([FromQuery] int doctorId, [FromQuery] DateOnly date)
	{
		// Ask the service to retrieve availability
		var result = await _appointmentService.GetAvailabilityAsync(
			doctorId, date
		);

		// Convert results into HTTP responses
		return result.Status switch
		{
			AvailabilityStatus.Valid => Ok(result.Slots),
			AvailabilityStatus.InvalidDoc => NotFound(result.Error),
			AvailabilityStatus.InvalidDate => BadRequest(result.Error),
			AvailabilityStatus.Closed => BadRequest(result.Error),
			_ => throw new InvalidOperationException("Unexpected availability result.")
		};
	}
}