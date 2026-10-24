// Handles booking, listing, and cancelling appointments

using Microsoft.AspNetCore.Mvc;
using DoctorsAppointment.Api.Data;
using DoctorsAppointment.Api.Models;


namespace DoctorsAppointment.Api.Controllers;

[ApiController]
[Route("appointments")]
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
    public IActionResult GetAppointments(	// Filters Added
		[FromQuery] int? doctorId,
    	[FromQuery] DateOnly? date
		// [FromQuery] string? speciality

	)
    {
		var appointments = _dataStore.Appointments.AsEnumerable();

		// Filter by Doctor ID
		if (doctorId.HasValue)
		{
			appointments = appointments.Where(filter_id => filter_id.DoctorId == doctorId.Value);
		}

		// Filter by Date
		if (date.HasValue)
		{
			appointments = appointments.Where(filter_date =>
				DateOnly.FromDateTime(filter_date.StartTime) == date.Value);
		}

		// // Filter by specialtiy
		// if (!string.IsNullOrWhiteSpace(speciality))
		// {
		// 	appointments = appointments.Where(speicality =>_dataStore.Doctors.Any(
		// 		filter_speciality => filter_speciality.Id == speicality.DoctorId &&
		// 			string.Equals(filter_speciality.Specialty, speciality,
		// 				StringComparison.OrdinalIgnoreCase))); // not case sensistive
		// }


        // return Ok(_dataStore.Appointments);
		return Ok(appointments.ToList());
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
		// 6. Prevent double booking Doctor
		if (_dataStore.Appointments.Any(i =>
			i.DoctorId == appointment.DoctorId &&
			i.StartTime == appointment.StartTime &&
			!i.IsCancelled))
		{
			return Conflict("This doctor already has an appointment at this time."); // Conflict because appointment info is valid, but the requested slot is already occupied
		}

		// Create appointment if all validations pass
		appointment.Id = _dataStore.Appointments.Count + 1;
		appointment.IsCancelled = false;
		_dataStore.Appointments.Add(appointment);

		return StatusCode(201, appointment);
	}


    // DELETE request to cancel an appointment
    [HttpDelete("{id}")]
    public IActionResult CancelAppointment(int id)
    {
        // find the appointment by ID
        var appointment = _dataStore.Appointments
            .FirstOrDefault(a => a.Id == id); // instead of .Any() because it retrieves the actual object i.e. data

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

        return NoContent();
    }


	// Get Doctor's Avaliability
	[HttpGet("availability")]
	public IActionResult GetAvailability(
		[FromQuery] int doctorId,
		[FromQuery] DateOnly date)
	{
		// 1. Check if doctor exists
		if (!_dataStore.Doctors.Any(doctor => doctor.Id == doctorId))
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

		// 4. Generate the 16 possible time slots
		var slots = new List<object>();
		var now = DateTime.Now;

		for (int i = 0; i < 16; i++)
		{
			var slotTime = date.ToDateTime(new TimeOnly(8, 0))
							.AddMinutes(i * 30);

			// 5. Check if slot is already booked
			bool booked = _dataStore.Appointments.Any(a =>
				a.DoctorId == doctorId &&
				a.StartTime == slotTime &&
				!a.IsCancelled);

			// 6. Add slot and availability status
			slots.Add(new
			{
				time = slotTime.ToString("HH:mm"),
				available = !booked && (slotTime > now),
			});
		}

		return Ok(slots);
	}
}