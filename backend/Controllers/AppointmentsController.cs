// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace DoctorsAppointment.Api.Controllers;

public record CancelAppointmentRequest(string? Reason);
public record RescheduleAppointmentRequest(int DoctorId, DateTime StartTime);

[ApiController]
[Authorize(Roles = "Receptionist")]
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

    // PUT request to reschedule an existing appointment
    [HttpPut("{id}")]
    public async Task<IActionResult> RescheduleAppointment(int id, [FromBody] RescheduleAppointmentRequest request)
    {
        // Ask the service to reschedule the appointment
        var result = await _appointmentService.RescheduleAppointmentAsync(
            id,
            request.DoctorId,
            request.StartTime
        );

        // Convert the service result into an HTTP response
        return result.Status switch
        {
            RescheduleStatus.Rescheduled => Ok(result.Appointment),
            RescheduleStatus.NotFound => NotFound(result.Error),
            RescheduleStatus.AlreadyCancelled => Conflict(result.Error),
            RescheduleStatus.AlreadyStarted => Conflict(result.Error),
            RescheduleStatus.Invalid => BadRequest(result.Error),
            RescheduleStatus.Conflict => Conflict(result.Error),
            _ => throw new InvalidOperationException("Unexpected Reschedule Error.")
        };
    }

    // DELETE request to cancel an appointment
	[HttpDelete("{id}")]
	public async Task<IActionResult> CancelAppointment(int id, [FromBody] CancelAppointmentRequest request)
	{
		// Ask the service to cancel the appointment
		var result = await _appointmentService.CancelAppointmentAsync(id, request.Reason);
		// Convert the service result into an HTTP response
		return result.Status switch
		{
			CancellationStatus.Cancelled => NoContent(),
			CancellationStatus.NotFound => NotFound(result.Error),
			CancellationStatus.AlreadyCancelled => Conflict(result.Error),
			CancellationStatus.InvalidReason => BadRequest(result.Error),
			CancellationStatus.AlreadyStarted => Conflict(result.Error),
			_ => throw new InvalidOperationException("Unexpected Cancellation Error.")
		};
	}


	// GET request to retrieve Doctor's Avaliability
	[HttpGet("availability")]
	public async Task<IActionResult> GetAvailability([FromQuery] int doctorId, [FromQuery] DateOnly date, [FromQuery] AppointmentType type)
	{
		// Ask the service to retrieve availability
		var result = await _appointmentService.GetAvailabilityAsync(doctorId, date, type);

		// Convert results into HTTP responses
		return result.Status switch
		{
			AvailabilityStatus.Valid => Ok(result.Slots),
			AvailabilityStatus.InvalidDoc => NotFound(result.Error),
			AvailabilityStatus.InvalidDate => BadRequest(result.Error),
			AvailabilityStatus.InvalidType => BadRequest(result.Error),
			AvailabilityStatus.Closed => BadRequest(result.Error),
			_ => throw new InvalidOperationException("Unexpected availability result.")
		};
	}
}