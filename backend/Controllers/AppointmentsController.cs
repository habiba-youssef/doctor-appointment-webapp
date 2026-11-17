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

	// Convert an invalid field into a ValidationProblem response
    private IActionResult ValidationError(string field, string? error)
    {
        ModelState.AddModelError(
            field,
            error ?? "Invalid value."
        );

        return ValidationProblem(ModelState);
    }

	// Identify the field associated with a service validation error
    private static string GetAppointmentErrorField(string? error)
    {
        return error switch
        {
            "Invalid appointment type." => "type",

            "Appointments must start at :00, :15, :30 or :45" or
            "Appointments are only available between 08:00 and 16:00." or
            "Appointments are only available Sunday to Thursday." or
            "Cannot book an appointment in the past." or
            "Appointment is outside the doctor's working hours." or
            "Doctor is not scheduled to work on this day." or
            "Doctor is on leave on this date."
                => "startTime",

            // The service doesn't distinguish which ID is invalid
            _ => "appointment"
        };
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
			BookingStatus.Invalid => ValidationError(GetAppointmentErrorField(result.Error), result.Error),
			BookingStatus.Conflict => Problem(statusCode: 409, title: "Appointment conflict", detail: result.Error),
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
            RescheduleStatus.NotFound => Problem(statusCode: 404, title: "Appointment not found", detail: result.Error),
            RescheduleStatus.AlreadyCancelled => Problem(statusCode: 409, title: "Appointment already cancelled", detail: result.Error),
            RescheduleStatus.AlreadyStarted => Problem(statusCode: 409, title: "Appointment already started", detail: result.Error),
            RescheduleStatus.Invalid => ValidationError( GetAppointmentErrorField(result.Error), result.Error),
            RescheduleStatus.Conflict => Problem(statusCode: 409, title: "Rescheduling conflict", detail: result.Error),
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
			CancellationStatus.NotFound => Problem(statusCode: 404, title: "Appointment not found", detail: result.Error),
			CancellationStatus.AlreadyCancelled => Problem(statusCode: 409, title: "Appointment already cancelled", detail: result.Error),
			CancellationStatus.InvalidReason => ValidationError("reason", result.Error),
			CancellationStatus.AlreadyStarted => Problem(statusCode: 409, title: "Appointment already started", detail: result.Error),
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
			AvailabilityStatus.InvalidDoc => Problem(statusCode: 404, title: "Doctor not found", detail: result.Error),
			AvailabilityStatus.InvalidDate => ValidationError("date", result.Error),
			AvailabilityStatus.InvalidType => ValidationError("type", result.Error),
			AvailabilityStatus.Closed => ValidationError("date", result.Error),
			_ => throw new InvalidOperationException("Unexpected availability result.")
		};
	}
}