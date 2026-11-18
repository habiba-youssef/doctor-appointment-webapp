
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;
using DoctorsAppointment.Api.Helpers;

namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Authorize(Roles = "Doctor")]
[Route("doctors/{doctorId:int}/appointments")]
public class DoctorAppointmentsController : ControllerBase
{
    private readonly ClinicDbContext _db;
    private readonly TimeProvider _clock;

    public DoctorAppointmentsController(
        ClinicDbContext db,
        TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    // Check that the doctor owns this account
    private bool IsOwnAccount(int doctorId)
    {
        var claim = User.FindFirst("doctorId")?.Value;

        return int.TryParse(claim, out int loggedInDoctorId)
            && loggedInDoctorId == doctorId;
    }

    // GET /doctors/1/appointments?date=2026-11-19
    [HttpGet]
    public async Task<IActionResult> GetAppointments(
        int doctorId,
        [FromQuery] DateOnly? date)
    {
        if (!IsOwnAccount(doctorId))
            return Forbid();

        var selectedDate = date ?? KuwaitTimeHelper.GetToday(_clock);
        var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        // Include only the doctor's own active appointments
        var appointments = await (
            from a in _db.Appointments.AsNoTracking()
            join p in _db.Patients.AsNoTracking()
                on a.PatientId equals p.Id
            where a.DoctorId == doctorId
                && !a.IsCancelled
                && a.StartTime >= dayStart
                && a.StartTime < dayEnd
            orderby a.StartTime
            select new
            {
                a.Id,
                a.PatientId,
                PatientName = p.FirstName + " " + p.LastName,
                a.StartTime,
                a.Type,
                a.Status
            }
        ).ToListAsync();

        return Ok(appointments);
    }

    // PUT /doctors/1/appointments/5/status
    [HttpPut("{appointmentId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int doctorId,
        int appointmentId,
        [FromBody] UpdateStatusRequest request)
    {
        if (!IsOwnAccount(doctorId))
            return Forbid();

        // Doctor can only select Completed or NoShow
        if (request.Status != AppointmentStatus.Completed &&
            request.Status != AppointmentStatus.NoShow)
        {
            return Problem(
                statusCode: 400,
                title: "Invalid appointment status."
            );
        }

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a =>
                a.Id == appointmentId &&
                a.DoctorId == doctorId);

        if (appointment == null)
            return NotFound();

        if (appointment.IsCancelled)
        {
            return Problem(
                statusCode: 409,
                title: "Cannot update a cancelled appointment."
            );
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return Problem(
                statusCode: 409,
                title: "Appointment status has already been finalized."
            );
        }

        // Save the new status
        appointment.Status = request.Status;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record UpdateStatusRequest(AppointmentStatus Status);
