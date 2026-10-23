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
		// Check if any patient information is invalid
		if (!_dataStore.Doctors.Any(dr => dr.Id == appointment.DoctorId)  ||
			!_dataStore.Patients.Any(p => p.Id == appointment.PatientId) ||
			appointment.StartTime == default
			)
		{
			return BadRequest("Error in patient data entered; check info...");
		}
		else
		{
			// Generate ID and add patient
			appointment.Id = _dataStore.Appointments.Count + 1;
			appointment.IsCancelled = false;
			_dataStore.Appointments.Add(appointment);

			return StatusCode(201, appointment);
		}
	}

}
