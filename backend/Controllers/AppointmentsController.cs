// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;


namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly DataStore _dataStore;
    // Constructor
    public AppointmentsController(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    // GET request to retrieve appointments
    [HttpGet]
    public IActionResult GetAppointments()
    {
        return Ok(_dataStore.Appointments);
    }

	// POST request to create new appointments
		
	[HttpPost]
	public IActionResult CreateAppointment([FromBody] Appointment appointment)
	{
		// 1. Validate Doctor and Patient IDs
		if (!_dataStore.Doctors.Any(dr => dr.Id == appointment.DoctorId) ||
			!_dataStore.Patients.Any(p => p.Id == appointment.PatientId))
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
		if (appointment.StartTime <= DateTime.Now)
		{
			return BadRequest("Cannot book an appointment in the past.");
		}

		// 6. Create appointment if all validations pass
		appointment.Id = _dataStore.Appointments.Count + 1;
		appointment.IsCancelled = false;
		_dataStore.Appointments.Add(appointment);

		return StatusCode(201, appointment);
	}
}