
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DoctorsAppointment.Api.Services;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Authorize(Roles = "Doctor")]
[Route("doctors/{doctorId:int}/appointments")]
public class DoctorAppointmentsController : ControllerBase
{
    private readonly AppointmentsService _appointmentService;

    public DoctorAppointmentsController(
        AppointmentsService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // GET /doctors/3/appointments
    [HttpGet]
    public async Task<IActionResult> GetDoctorAppointments(
        int doctorId,
        [FromQuery] DateOnly? date)
    {
        // Read the doctor ID from the authenticated JWT
        var claimDoctorId = User.FindFirst("doctorId")?.Value;

        if (!int.TryParse(claimDoctorId, out int loggedInDoctorId))
        {
            return Forbid();
        }

        // Prevent doctors from accessing another doctor's appointments
        if (loggedInDoctorId != doctorId)
        {
            return Forbid();
        }

        // Retrieve appointments belonging only to this doctor
        var appointments = await _appointmentService
            .GetAppointmentsAsync(loggedInDoctorId, date);

        return Ok(appointments);
    }
}
